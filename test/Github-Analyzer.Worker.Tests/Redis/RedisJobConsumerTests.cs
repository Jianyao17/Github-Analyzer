using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Worker.Config;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Models;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Redis;

namespace GithubAnalyzer.Worker.Tests.Redis;

public class RedisJobConsumerTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IAnalysisPipeline> _pipelineMock = new();
    private readonly Mock<IProjectQueueRepository> _queueRepoMock = new();
    private readonly Mock<ILogger<RedisJobConsumer>> _loggerMock = new();
    private readonly RedisConfig _config = new()
    {
        StreamName = "analysis:jobs",
        ConsumerGroup = "analysis-workers",
        StaleIdleTimeout = TimeSpan.FromMinutes(5),
        MaxDeliveryAttempts = 3,
        PollDelay = TimeSpan.FromMilliseconds(10),
        StaleCheckInterval = TimeSpan.FromSeconds(30)
    };

    public RedisJobConsumerTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

        _scopeFactoryMock.Setup(s => s.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAnalysisPipeline))).Returns(_pipelineMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IProjectQueueRepository))).Returns(_queueRepoMock.Object);
    }

    private RedisJobConsumer CreateConsumer(RedisConfig? customOptions = null)
    {
        var opt = Options.Create(customOptions ?? _config);
        return new RedisJobConsumer(_redisMock.Object, opt, _scopeFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task EnsureConsumerGroup_WhenStreamAndGroupAlreadyExist_DoesNotCreateGroup()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        _dbMock.Setup(d => d.KeyExistsAsync(_config.StreamName, CommandFlags.None))
            .ReturnsAsync(true);

        var existingGroup = RedisTestFactory.CreateStreamGroupInfo(_config.ConsumerGroup);
        _dbMock.Setup(d => d.StreamGroupInfoAsync(_config.StreamName, CommandFlags.None))
            .ReturnsAsync([existingGroup]);

        // Act
        var consumer = CreateConsumer();
        await consumer.StartAsync(cts.Token);
        await Task.Delay(20);
        await consumer.StopAsync(CancellationToken.None);

        // Assert
        _dbMock.Verify(d => d.StreamCreateConsumerGroupAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<RedisValue>(),
            It.IsAny<bool>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task EnsureConsumerGroup_WhenStreamExistsButGroupDoesNotExist_CreatesGroup()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        _dbMock.Setup(d => d.KeyExistsAsync(_config.StreamName, CommandFlags.None))
            .ReturnsAsync(true);

        _dbMock.Setup(d => d.StreamGroupInfoAsync(_config.StreamName, CommandFlags.None))
            .ReturnsAsync([]);

        _dbMock.Setup(d => d.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", false, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        var consumer = CreateConsumer();
        await consumer.StartAsync(cts.Token);
        await Task.Delay(20);
        await consumer.StopAsync(CancellationToken.None);

        // Assert
        _dbMock.Verify(d => d.StreamCreateConsumerGroupAsync(
            _config.StreamName,
            _config.ConsumerGroup,
            "0-0",
            false,
            CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task EnsureConsumerGroup_WhenStreamDoesNotExist_CreatesStreamAndGroupWithFlag()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        _dbMock.Setup(d => d.KeyExistsAsync(_config.StreamName, CommandFlags.None))
            .ReturnsAsync(false);

        _dbMock.Setup(d => d.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", true, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        var consumer = CreateConsumer();
        await consumer.StartAsync(cts.Token);
        await Task.Delay(20);
        await consumer.StopAsync(CancellationToken.None);

        // Assert
        _dbMock.Verify(d => d.StreamCreateConsumerGroupAsync(
            _config.StreamName,
            _config.ConsumerGroup,
            "0-0",
            true,
            CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task ProcessNewJob_WhenValidEntryAvailable_ExecutesPipelineAndAcknowledges()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var jobMessage = new AnalysisJobMessage
        {
            JobId = jobId,
            ProjectId = projectId,
            RepositoryUrl = "https://github.com/dotnet/runtime",
            Options = new AnalysisOptions
            {
                Statistics = true,
                CodeGraph = false
            }
        };

        var json = JsonSerializer.Serialize(jobMessage, WorkerJsonContext.Default.AnalysisJobMessage);
        var entryId = (RedisValue)"1700000000000-0";
        var streamEntry = new StreamEntry(entryId, [new NameValueEntry("payload", json)]);

        using var cts = new CancellationTokenSource();

        _dbMock.Setup(d => d.KeyExistsAsync(_config.StreamName, CommandFlags.None)).ReturnsAsync(false);
        _dbMock.Setup(d => d.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", true, CommandFlags.None)).ReturnsAsync(true);

        _dbMock.SetupSequence(d => d.StreamReadGroupAsync(
                _config.StreamName,
                _config.ConsumerGroup,
                It.IsAny<RedisValue>(),
                ">",
                1,
                false,
                null,
                CommandFlags.None))
            .ReturnsAsync([streamEntry])
            .ReturnsAsync([]);

        _pipelineMock.Setup(p => p.ExecuteAsync(It.Is<AnalysisJobMessage>(j => j.JobId == jobId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _dbMock.Setup(d => d.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entryId, CommandFlags.None))
            .Callback(() => cts.Cancel())
            .ReturnsAsync(1);

        // Act
        var consumer = CreateConsumer();
        await consumer.StartAsync(cts.Token);
        try { await Task.Delay(500, cts.Token); } catch (OperationCanceledException) { }
        await consumer.StopAsync(CancellationToken.None);

        // Assert
        _pipelineMock.Verify(p => p.ExecuteAsync(It.Is<AnalysisJobMessage>(j => j.JobId == jobId), It.IsAny<CancellationToken>()), Times.Once);
        _dbMock.Verify(d => d.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entryId, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task ProcessStaleJob_WhenDeliveryExceedsMaxAttempts_AcknowledgesAsDeadLetterWithoutExecutingPipeline()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobMessage = new AnalysisJobMessage
        {
            JobId = jobId,
            ProjectId = Guid.NewGuid(),
            RepositoryUrl = "https://github.com/dotnet/runtime"
        };
        var json = JsonSerializer.Serialize(jobMessage, WorkerJsonContext.Default.AnalysisJobMessage);
        var entryId = (RedisValue)"1700000000001-0";
        var streamEntry = new StreamEntry(entryId, [new NameValueEntry("payload", json)]);

        var claimResult = RedisTestFactory.CreateStreamAutoClaimResult("0-0", [streamEntry]);

        var options = new RedisConfig
        {
            StreamName = "analysis:jobs",
            ConsumerGroup = "analysis-workers",
            StaleCheckInterval = TimeSpan.Zero, // Selalu trigger stale check
            MaxDeliveryAttempts = 3,
            PollDelay = TimeSpan.FromMilliseconds(10)
        };

        using var cts = new CancellationTokenSource();

        _dbMock.Setup(d => d.KeyExistsAsync(options.StreamName, CommandFlags.None)).ReturnsAsync(false);
        _dbMock.Setup(d => d.StreamCreateConsumerGroupAsync(options.StreamName, options.ConsumerGroup, "0-0", true, CommandFlags.None)).ReturnsAsync(true);

        _dbMock.SetupSequence(d => d.StreamAutoClaimAsync(
                options.StreamName,
                options.ConsumerGroup,
                It.IsAny<RedisValue>(),
                It.IsAny<long>(),
                "0-0",
                1,
                CommandFlags.None))
            .ReturnsAsync(claimResult)
            .ReturnsAsync(RedisTestFactory.CreateStreamAutoClaimResult("0-0", []));

        // Delivery count = 4, melebihi max 3
        var pendingInfo = RedisTestFactory.CreateStreamPendingMessageInfo(entryId, "other-worker", 400000, deliveryCount: 4);
        _dbMock.Setup(d => d.StreamPendingMessagesAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync([pendingInfo]);

        _dbMock.Setup(d => d.StreamPendingMessagesAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<long?>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync([pendingInfo]);

        _dbMock.Setup(d => d.StreamAcknowledgeAsync(options.StreamName, options.ConsumerGroup, entryId, CommandFlags.None))
            .Callback(() => cts.Cancel())
            .ReturnsAsync(1);

        // Act
        var consumer = CreateConsumer(options);
        await consumer.StartAsync(cts.Token);
        try { await Task.Delay(500, cts.Token); } catch (OperationCanceledException) { }
        await consumer.StopAsync(CancellationToken.None);

        // Assert: Pipeline TIDAK dijalankan, tapi status DB ditandai Failed dan stream di-ACK (dead-letter)
        _pipelineMock.Verify(p => p.ExecuteAsync(It.IsAny<AnalysisJobMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _queueRepoMock.Verify(q => q.MarkJobFailedAsync(jobId, It.Is<string>(s => s.Contains("Dead-letter")), It.IsAny<CancellationToken>()), Times.Once);
        _dbMock.Verify(d => d.StreamAcknowledgeAsync(options.StreamName, options.ConsumerGroup, entryId, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task ProcessJob_WhenCorruptPayload_AcknowledgesToPreventBlocking()
    {
        // Arrange
        var entryId = (RedisValue)"1700000000002-0";
        var streamEntry = new StreamEntry(entryId, [new NameValueEntry("payload", "{ invalid-json }")]);

        using var cts = new CancellationTokenSource();

        _dbMock.Setup(d => d.KeyExistsAsync(_config.StreamName, CommandFlags.None)).ReturnsAsync(false);
        _dbMock.Setup(d => d.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", true, CommandFlags.None)).ReturnsAsync(true);

        _dbMock.SetupSequence(d => d.StreamReadGroupAsync(
                _config.StreamName,
                _config.ConsumerGroup,
                It.IsAny<RedisValue>(),
                ">",
                1,
                false,
                null,
                CommandFlags.None))
            .ReturnsAsync([streamEntry])
            .ReturnsAsync([]);

        _dbMock.Setup(d => d.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entryId, CommandFlags.None))
            .Callback(() => cts.Cancel())
            .ReturnsAsync(1);

        // Act
        var consumer = CreateConsumer();
        await consumer.StartAsync(cts.Token);
        try { await Task.Delay(500, cts.Token); } catch (OperationCanceledException) { }
        await consumer.StopAsync(CancellationToken.None);

        // Assert: Pesan di-ACK dan pipeline tidak dieksekusi
        _pipelineMock.Verify(p => p.ExecuteAsync(It.IsAny<AnalysisJobMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _dbMock.Verify(d => d.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entryId, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task ProcessJob_WhenPipelineThrows_DoesNotAcknowledgeMessage()
    {
        // Arrange
        var jobMessage = new AnalysisJobMessage
        {
            JobId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RepositoryUrl = "https://github.com/dotnet/runtime"
        };
        var json = JsonSerializer.Serialize(jobMessage, WorkerJsonContext.Default.AnalysisJobMessage);
        var entryId = (RedisValue)"1700000000003-0";
        var streamEntry = new StreamEntry(entryId, [new NameValueEntry("payload", json)]);

        using var cts = new CancellationTokenSource();

        _dbMock.Setup(d => d.KeyExistsAsync(_config.StreamName, CommandFlags.None)).ReturnsAsync(false);
        _dbMock.Setup(d => d.StreamCreateConsumerGroupAsync(_config.StreamName, _config.ConsumerGroup, "0-0", true, CommandFlags.None)).ReturnsAsync(true);

        _dbMock.SetupSequence(d => d.StreamReadGroupAsync(
                _config.StreamName,
                _config.ConsumerGroup,
                It.IsAny<RedisValue>(),
                ">",
                1,
                false,
                null,
                CommandFlags.None))
            .ReturnsAsync([streamEntry])
            .ReturnsAsync([]);

        _pipelineMock.Setup(p => p.ExecuteAsync(It.IsAny<AnalysisJobMessage>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new InvalidOperationException("Pipeline crash on step 2"));

        // Act
        var consumer = CreateConsumer();
        await consumer.StartAsync(cts.Token);
        try { await Task.Delay(500, cts.Token); } catch (OperationCanceledException) { }
        await consumer.StopAsync(CancellationToken.None);

        // Assert: StreamAcknowledgeAsync TIDAK dipanggil agar tetap berada di PEL untuk di-claim ulang
        _dbMock.Verify(d => d.StreamAcknowledgeAsync(_config.StreamName, _config.ConsumerGroup, entryId, CommandFlags.None), Times.Never);
    }
}
