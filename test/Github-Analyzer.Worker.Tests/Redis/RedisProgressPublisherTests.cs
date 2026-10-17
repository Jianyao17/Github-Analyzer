using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.Worker.Redis;

namespace GithubAnalyzer.Worker.Tests.Redis;

public class RedisProgressPublisherTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<ISubscriber> _subscriberMock = new();
    private readonly Mock<ILogger<RedisProgressPublisher>> _loggerMock = new();

    public RedisProgressPublisherTests()
    {
        _redisMock
            .Setup(r => r.GetSubscriber(It.IsAny<object>()))
            .Returns(_subscriberMock.Object);
    }

    [Fact]
    public async Task PublishAsync_PublishesToCorrectChannelWithSerializedPayload()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var progressEvent = new AnalysisProgressEvent
        {
            JobId = jobId,
            ProjectId = projectId,
            AnalysisType = AnalysisType.Statistic,
            Status = JobQueueStatus.Running,
            Progress = 42,
            Message = "Computing lines of code"
        };

        var expectedChannel = $"analysis:progress:{projectId}:statistic";

        _subscriberMock
            .Setup(s => s.PublishAsync(
                It.Is<RedisChannel>(c => c.ToString() == expectedChannel),
                It.Is<RedisValue>(v => v.ToString().Contains("Computing lines of code")),
                CommandFlags.None))
            .ReturnsAsync(1);

        var publisher = new RedisProgressPublisher(_redisMock.Object, _loggerMock.Object);

        // Act
        await publisher.PublishAsync(progressEvent);

        // Assert
        _subscriberMock.Verify(s => s.PublishAsync(
            It.Is<RedisChannel>(c => c.ToString() == expectedChannel),
            It.Is<RedisValue>(v => v.ToString().Contains("Computing lines of code") && v.ToString().Contains(jobId.ToString())),
            CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenRedisThrows_LogsWarningAndDoesNotThrow()
    {
        // Arrange
        var progressEvent = new AnalysisProgressEvent
        {
            JobId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            AnalysisType = AnalysisType.CodeGraph,
            Progress = 80
        };

        _subscriberMock
            .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), CommandFlags.None))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis node unavailable"));

        var publisher = new RedisProgressPublisher(_redisMock.Object, _loggerMock.Object);

        // Act & Assert (tidak boleh throw)
        var exception = await Record.ExceptionAsync(async () => await publisher.PublishAsync(progressEvent));
        Assert.Null(exception);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
