using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Services;
using Microsoft.Extensions.Logging;

namespace GithubAnalyzer.Worker.Tests.Services;

public class DbAnalysisCacheServiceTests
{
    private readonly Mock<IAnalysisRepository> _mockRepo = new();
    private readonly Mock<ILogger<DbAnalysisCacheService>> _mockLogger = new();
    private readonly DbAnalysisCacheService _service;

    public DbAnalysisCacheServiceTests()
    {
        _service = new DbAnalysisCacheService(_mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task TryCopyStatisticCacheAsync_WhenHit_ReturnsTrue()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repoUrl = "https://github.com/dotnet/runtime";
        var branch = "main";
        var commitHash = "abc1234";
        var version = "v1";

        var expectedKey = CacheLookupKey.Generate(repoUrl, branch, commitHash, version);

        _mockRepo
            .Setup(r => r.TryCopyStatisticCacheAsync(projectId, userId, expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.TryCopyStatisticCacheAsync(projectId, userId, repoUrl, branch, commitHash, version);

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.TryCopyStatisticCacheAsync(projectId, userId, expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryCopyStatisticCacheAsync_WhenMiss_ReturnsFalse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockRepo
            .Setup(r => r.TryCopyStatisticCacheAsync(projectId, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.TryCopyStatisticCacheAsync(projectId, userId, "https://github.com/dotnet/runtime", null, null, "v1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryCopyCodeGraphCacheAsync_WhenHit_ReturnsTrue()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repoUrl = "https://github.com/dotnet/roslyn";
        var branch = "main";
        var commitHash = "def5678";
        var version = "v2";

        var expectedKey = CacheLookupKey.Generate(repoUrl, branch, commitHash, version);

        _mockRepo
            .Setup(r => r.TryCopyCodeGraphCacheAsync(projectId, userId, expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.TryCopyCodeGraphCacheAsync(projectId, userId, repoUrl, branch, commitHash, version);

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.TryCopyCodeGraphCacheAsync(projectId, userId, expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryCopyCodeGraphCacheAsync_WhenMiss_ReturnsFalse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockRepo
            .Setup(r => r.TryCopyCodeGraphCacheAsync(projectId, userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.TryCopyCodeGraphCacheAsync(projectId, userId, "https://github.com/dotnet/roslyn", "dev", "111", "v1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetStatisticCacheAsync_WhenLookupKeyEmpty_GeneratesLookupKeyAndSaves()
    {
        // Arrange
        var cache = new StatisticCache
        {
            RepoUrl = "https://github.com/dotnet/aspnetcore",
            Branch = "main",
            CommitHash = "999aaa",
            AnalysisVersion = "v1",
            LookupKey = ""
        };

        var expectedKey = CacheLookupKey.Generate(cache.RepoUrl, cache.Branch, cache.CommitHash, cache.AnalysisVersion);

        _mockRepo
            .Setup(r => r.SaveStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SetStatisticCacheAsync(cache);

        // Assert
        Assert.Equal(expectedKey, cache.LookupKey);
        _mockRepo.Verify(r => r.SaveStatisticCacheAsync(It.Is<StatisticCache>(c => c.LookupKey == expectedKey), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetStatisticCacheAsync_WhenLookupKeyProvided_PreservesLookupKeyAndSaves()
    {
        // Arrange
        var cache = new StatisticCache
        {
            RepoUrl = "https://github.com/dotnet/aspnetcore",
            Branch = "main",
            CommitHash = "999aaa",
            AnalysisVersion = "v1",
            LookupKey = "existing-key"
        };

        _mockRepo
            .Setup(r => r.SaveStatisticCacheAsync(It.IsAny<StatisticCache>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SetStatisticCacheAsync(cache);

        // Assert
        Assert.Equal("existing-key", cache.LookupKey);
        _mockRepo.Verify(r => r.SaveStatisticCacheAsync(It.Is<StatisticCache>(c => c.LookupKey == "existing-key"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCodeGraphCacheAsync_WhenLookupKeyEmpty_GeneratesLookupKeyAndSaves()
    {
        // Arrange
        var cache = new CodeGraphCache
        {
            RepoUrl = "https://github.com/dotnet/aspnetcore",
            Branch = "main",
            CommitHash = "888bbb",
            AnalysisVersion = "v2",
            LookupKey = ""
        };
        var graphJson = "{\"nodes\":[],\"edges\":[]}";
        var expectedKey = CacheLookupKey.Generate(cache.RepoUrl, cache.Branch, cache.CommitHash, cache.AnalysisVersion);

        _mockRepo
            .Setup(r => r.SaveCodeGraphCacheAsync(It.IsAny<CodeGraphCache>(), graphJson, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SetCodeGraphCacheAsync(cache, graphJson);

        // Assert
        Assert.Equal(expectedKey, cache.LookupKey);
        _mockRepo.Verify(r => r.SaveCodeGraphCacheAsync(
            It.Is<CodeGraphCache>(c => c.LookupKey == expectedKey),
            graphJson,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateOldCachesAsync_DelegatesToRepository()
    {
        // Arrange
        var maxAge = TimeSpan.FromDays(7);

        _mockRepo
            .Setup(r => r.InvalidateOldCachesAsync(maxAge, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.InvalidateOldCachesAsync(maxAge);

        // Assert
        _mockRepo.Verify(r => r.InvalidateOldCachesAsync(maxAge, It.IsAny<CancellationToken>()), Times.Once);
    }
}
