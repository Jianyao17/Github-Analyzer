using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using GithubAnalyzer.WebApi.Services.Redis;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Shared.Enums;

namespace GithubAnalyzer.WebApi.Tests.Services.Redis;

public class RedisJobProgressSubscriberTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<ISubscriber> _subscriberMock = new();
    private readonly RedisConfig _config = new()
    {
        ProgressChannelPrefix = "test:progress"
    };

    public RedisJobProgressSubscriberTests()
    {
        _redisMock.Setup(r => r.GetSubscriber(It.IsAny<object>()))
            .Returns(_subscriberMock.Object);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldSubscribeToCorrectChannel_AndStreamProgressEvents()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var jobType = "Statistic";
        var expectedChannel = $"test:progress:{projectId}:{jobType.ToLowerInvariant()}";

        Action<RedisChannel, RedisValue>? messageHandler = null;

        _subscriberMock.Setup(s => s.SubscribeAsync(
                RedisChannel.Literal(expectedChannel),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>(
                (_, handler, _) => messageHandler = handler)
            .Returns(Task.CompletedTask);

        _subscriberMock.Setup(s => s.UnsubscribeAsync(
                RedisChannel.Literal(expectedChannel),
                It.IsAny<Action<RedisChannel, RedisValue>?>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);

        var subscriberService = new RedisJobProgressSubscriber(
            _redisMock.Object,
            Options.Create(_config),
            NullLogger<RedisJobProgressSubscriber>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act & Assert
        var stream = subscriberService.SubscribeAsync(projectId, jobType, cts.Token);
        var enumerator = stream.GetAsyncEnumerator(cts.Token);

        // Start enumerator move next in background
        var moveNextTask1 = enumerator.MoveNextAsync().AsTask();

        Assert.NotNull(messageHandler);

        // 1. Emit Running Event
        var runningEvent = new AnalysisProgressEvent
        {
            JobId = jobId,
            ProjectId = projectId,
            AnalysisType = AnalysisType.Statistic,
            Status = JobQueueStatus.Running,
            Progress = 50,
            Message = "Extracting files...",
            Timestamp = DateTimeOffset.UtcNow
        };
        messageHandler(RedisChannel.Literal(expectedChannel), JsonSerializer.Serialize(runningEvent));

        var hasItem1 = await moveNextTask1;
        Assert.True(hasItem1);
        Assert.Equal(50, enumerator.Current.Progress);
        Assert.Equal("Running", enumerator.Current.Status);
        Assert.Equal("Extracting files...", enumerator.Current.Message);

        // 2. Emit Completed Event (Should complete the channel)
        var moveNextTask2 = enumerator.MoveNextAsync().AsTask();
        var completedEvent = new AnalysisProgressEvent
        {
            JobId = jobId,
            ProjectId = projectId,
            AnalysisType = AnalysisType.Statistic,
            Status = JobQueueStatus.Completed,
            Progress = 100,
            Message = "Done",
            Timestamp = DateTimeOffset.UtcNow
        };
        messageHandler(RedisChannel.Literal(expectedChannel), JsonSerializer.Serialize(completedEvent));

        var hasItem2 = await moveNextTask2;
        Assert.True(hasItem2);
        Assert.Equal(100, enumerator.Current.Progress);
        Assert.Equal("Completed", enumerator.Current.Status);

        // 3. Since completed event completed the channel, next MoveNext should return false
        var hasItem3 = await enumerator.MoveNextAsync();
        Assert.False(hasItem3);

        await enumerator.DisposeAsync();

        // Verify UnsubscribeAsync was called
        _subscriberMock.Verify(s => s.UnsubscribeAsync(
            RedisChannel.Literal(expectedChannel),
            It.IsAny<Action<RedisChannel, RedisValue>?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
