using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using GithubAnalyzer.Shared.Git;

namespace GithubAnalyzer.WebApi.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GitService"/>.
/// </summary>
public sealed class GitServiceTests
{
    private static readonly IOptions<GitOptions> DefaultOptions = 
        Options.Create(new GitOptions { StoragePath = Path.Combine(Path.GetTempPath(), "test-ga") });

    private static Mock<IGitProvider> MakeProvider(bool canHandle, string? url = null)
    {
        var mock = new Mock<IGitProvider>();
        mock.Setup(p => p.CanHandle(It.IsAny<string>())).Returns(canHandle);

        if (url is not null)
        {
            var result = new RepositoryResult(
                ExtractPath:     "/tmp/repo",
                RepositoryUrl:   url,
                RepositoryName:  "test-repo",
                Description:     null,
                AuthorName:      null,
                BranchName:      "main",
                LastCommitHash:  null,
                LastCommitAtUtc: null);

            mock.Setup(p => p.DownloadAndExtractAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        return mock;
    }

    [Fact]
    public async Task DownloadAndExtractAsync_UsesMatchingProvider()
    {
        const string url = "https://github.com/owner/repo";

        var provider = MakeProvider(canHandle: true, url: url);
        var service  = new GitService([provider.Object], DefaultOptions, NullLogger<GitService>.Instance);

        await service.DownloadAndExtractAsync(url);

        provider.Verify(p => p.DownloadAndExtractAsync(
            url, "main", null, It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task DownloadAndExtractAsync_ThrowsWhenNoProviderMatches()
    {
        var provider = MakeProvider(canHandle: false);
        var service  = new GitService([provider.Object], DefaultOptions, NullLogger<GitService>.Instance);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => service.DownloadAndExtractAsync("https://unknown.vcs/repo"));

        Assert.Contains("Tidak ada Git provider", ex.Message);
    }

    [Fact]
    public async Task DownloadAndExtractAsync_PicksFirstMatchingProviderWhenMultipleExist()
    {
        const string url = "https://github.com/owner/repo";

        var provider1 = MakeProvider(canHandle: false);
        var provider2 = MakeProvider(canHandle: true, url: url);
        var service   = new GitService([provider1.Object, provider2.Object], DefaultOptions, NullLogger<GitService>.Instance);

        await service.DownloadAndExtractAsync(url);

        provider1.Verify(p => p.DownloadAndExtractAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        provider2.Verify(p => p.DownloadAndExtractAsync(
            url, "main", null, It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task GetCountsAsync_DelegatesToProvider()
    {
        const string url = "https://github.com/owner/repo";
        var counts = new GitCounts(5, 100, 42);
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetCountsAsync(url, null, default))
                .ReturnsAsync(counts);

        var service = new GitService([provider.Object], DefaultOptions, NullLogger<GitService>.Instance);
        var result  = await service.GetCountsAsync(url);

        Assert.Equal(5, result.Branches);
        Assert.Equal(100, result.Commits);
        Assert.Equal(42, result.Contributors);
        provider.Verify(p => p.GetCountsAsync(url, null, default), Times.Once);
    }

    [Fact]
    public async Task GetBranchesAsync_DelegatesToProvider()
    {
        const string url = "https://github.com/owner/repo";
        var branches = new List<RepoBranch> { new("main", "abc123") };
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetBranchesAsync(url, default))
                .ReturnsAsync(branches);

        var service = new GitService([provider.Object], DefaultOptions, NullLogger<GitService>.Instance);
        var result  = await service.GetBranchesAsync(url);

        Assert.Single(result);
        Assert.Equal("main", result.First().Name);
        Assert.Equal("abc123", result.First().CommitHash);
    }

    [Fact]
    public async Task GetCommitsAsync_DelegatesToProvider()
    {
        const string url = "https://github.com/owner/repo";
        var commits = new List<RepoCommit>
        {
            new("abc123", "Initial commit", "author", DateTimeOffset.UtcNow)
        };
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetCommitsAsync(url, null, default))
                .ReturnsAsync(commits);

        var service = new GitService([provider.Object], DefaultOptions, NullLogger<GitService>.Instance);
        var result  = await service.GetCommitsAsync(url);

        Assert.Single(result);
        Assert.Equal("abc123", result.First().Hash);
        Assert.Equal("Initial commit", result.First().Message);
    }
}
