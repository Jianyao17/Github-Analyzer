using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Services;

namespace GithubAnalyzer.WebApi.Tests.Services;

public sealed class SourceCodeManagerTests
{
    private readonly Mock<IGitService> _mockGitService = new();
    private readonly Mock<IDistributedCache> _mockCache = new();
    private readonly SourceCodeManager _manager;

    public SourceCodeManagerTests()
    {
        _manager = new SourceCodeManager(
            _mockGitService.Object,
            _mockCache.Object,
            NullLogger<SourceCodeManager>.Instance);
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("https://github.com/owner/repo", null)]
    [InlineData("https://github.com/owner/repo", "")]
    public async Task GetFileContentAsync_WhenProjectMetadataInvalid_ReturnsNull(string? repoUrl, string? commitHash)
    {
        var project = new Project
        {
            RepositoryUrl = repoUrl!,
            LastCommitHash = commitHash!
        };

        var result = await _manager.GetFileContentAsync(project, "src/Program.cs");

        Assert.Null(result);
        _mockGitService.Verify(g => g.GetFileContentAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFileContentAsync_WhenCacheHit_ReturnsCachedContent()
    {
        var project = new Project
        {
            RepositoryUrl = "https://github.com/owner/repo",
            LastCommitHash = "abcdef123456"
        };
        const string relativePath = "src/Program.cs";
        const string cachedContent = "Console.WriteLine(\"Cached\");";
        var cachedBytes = Encoding.UTF8.GetBytes(cachedContent);

        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        var result = await _manager.GetFileContentAsync(project, relativePath);

        Assert.Equal(cachedContent, result);
        _mockGitService.Verify(g => g.GetFileContentAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFileContentAsync_WhenCacheMiss_CallsGitServiceAndCachesResult()
    {
        var project = new Project
        {
            RepositoryUrl = "https://github.com/owner/repo",
            LastCommitHash = "abcdef123456"
        };
        const string relativePath = "src/Program.cs";
        const string fetchedContent = "Console.WriteLine(\"Fetched\");";

        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _mockGitService.Setup(g => g.GetFileContentAsync(
                project.RepositoryUrl, project.LastCommitHash, relativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchedContent);

        var result = await _manager.GetFileContentAsync(project, relativePath);

        Assert.Equal(fetchedContent, result);
        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == fetchedContent),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFileContentAsync_WhenCacheThrows_GracefullyFallsBackToGitService()
    {
        var project = new Project
        {
            RepositoryUrl = "https://github.com/owner/repo",
            LastCommitHash = "abcdef123456"
        };
        const string relativePath = "src/Program.cs";
        const string fetchedContent = "Console.WriteLine(\"Fallback\");";

        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis unavailable"));

        _mockGitService.Setup(g => g.GetFileContentAsync(
                project.RepositoryUrl, project.LastCommitHash, relativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchedContent);

        var result = await _manager.GetFileContentAsync(project, relativePath);

        Assert.Equal(fetchedContent, result);
    }
}
