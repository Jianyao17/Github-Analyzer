using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using GithubAnalyzer.WebApi.Services.Redis;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Shared.Jobs;

namespace GithubAnalyzer.WebApi.Tests.Services.Redis;

public class RedisAnalysisJobDispatcherTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly RedisConfig _config = new()
    {
        StreamName = "test:analysis:jobs"
    };

    public RedisAnalysisJobDispatcherTests()
    {
        // GetDatabase() with no args resolves to GetDatabase(-1, null)
        _redisMock.Setup(r => r.GetDatabase(-1, null)).Returns(_dbMock.Object);
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);
    }

    private RedisAnalysisJobDispatcher CreateDispatcher() =>
        new(_redisMock.Object, Options.Create(_config), NullLogger<RedisAnalysisJobDispatcher>.Instance);

    [Fact]
    public async Task DispatchAsync_ShouldAddEntryToStream_WithJobIdProjectIdAndPayload()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var job = new AnalysisJobMessage
        {
            JobId = jobId,
            ProjectId = projectId,
            UserId = Guid.NewGuid(),
            RepositoryUrl = "https://github.com/test/repo",
            RepositoryName = "repo",
            Branch = "main",
            Options = new AnalysisOptions { Statistics = true, CodeGraph = true }
        };

        NameValueEntry[] capturedEntries = [];

        _dbMock.Setup(d => d.StreamAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<NameValueEntry[]>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<long?>(),
                It.IsAny<bool>(),
                It.IsAny<long?>(),
                It.IsAny<StreamTrimMode>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, NameValueEntry[], RedisValue?, long?, bool, long?, StreamTrimMode, CommandFlags>(
                (_, entries, _, _, _, _, _, _) => capturedEntries = entries)
            .ReturnsAsync((RedisValue)"1728543210000-0");

        // Act
        var result = await CreateDispatcher().DispatchAsync(job);

        // Assert — stream ID returned from Redis
        Assert.Equal("1728543210000-0", result);
        Assert.Equal(3, capturedEntries.Length);

        var jobIdEntry = capturedEntries.First(e => e.Name == "jobId");
        Assert.Equal(jobId.ToString(), jobIdEntry.Value.ToString());

        var projectIdEntry = capturedEntries.First(e => e.Name == "projectId");
        Assert.Equal(projectId.ToString(), projectIdEntry.Value.ToString());

        var payloadEntry = capturedEntries.First(e => e.Name == "payload");
        var deserialized = JsonSerializer.Deserialize<AnalysisJobMessage>(payloadEntry.Value.ToString());
        Assert.NotNull(deserialized);
        Assert.Equal(jobId, deserialized.JobId);
        Assert.Equal(projectId, deserialized.ProjectId);
        Assert.True(deserialized.Options.Statistics);
        Assert.True(deserialized.Options.CodeGraph);
    }

    [Fact]
    public async Task DispatchAsync_ShouldPropagateException_WhenRedisFails()
    {
        // Arrange
        _dbMock.Setup(d => d.StreamAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<NameValueEntry[]>(),
                It.IsAny<RedisValue?>(),
                It.IsAny<long?>(),
                It.IsAny<bool>(),
                It.IsAny<long?>(),
                It.IsAny<StreamTrimMode>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable"));

        var job = new AnalysisJobMessage
        {
            JobId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        // Act & Assert — dispatcher should let Redis exceptions propagate
        await Assert.ThrowsAsync<RedisConnectionException>(() => CreateDispatcher().DispatchAsync(job));
    }
}
