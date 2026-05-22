using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services.Repo;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RepositoryFetcher"/>.
/// </summary>
public sealed class RepositoryFetcherTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helper: build a mock provider
    // ─────────────────────────────────────────────────────────────────────────

    private static Mock<IRepositoryProvider> MakeProvider(
        bool canHandle, string? url = null)
    {
        var mock = new Mock<IRepositoryProvider>();
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
                    It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        return mock;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Provider selection
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadAndExtractAsync_UsesMatchingProvider()
    {
        const string url = "https://github.com/owner/repo";

        var provider = MakeProvider(canHandle: true, url: url);
        var fetcher  = new RepositoryFetcher([provider.Object]);

        await fetcher.DownloadAndExtractAsync(url);

        provider.Verify(p => p.DownloadAndExtractAsync(url, "main", null, default), Times.Once);
    }

    [Fact]
    public async Task DownloadAndExtractAsync_ThrowsWhenNoProviderMatches()
    {
        var provider = MakeProvider(canHandle: false);
        var fetcher  = new RepositoryFetcher([provider.Object]);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => fetcher.DownloadAndExtractAsync("https://unknown.vcs/repo"));

        Assert.Contains("No repository provider", ex.Message);
    }

    [Fact]
    public async Task DownloadAndExtractAsync_PicksFirstMatchingProviderWhenMultipleExist()
    {
        const string url = "https://github.com/owner/repo";

        var provider1 = MakeProvider(canHandle: false);
        var provider2 = MakeProvider(canHandle: true, url: url);
        var fetcher   = new RepositoryFetcher([provider1.Object, provider2.Object]);

        await fetcher.DownloadAndExtractAsync(url);

        provider1.Verify(p => p.DownloadAndExtractAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);

        provider2.Verify(p => p.DownloadAndExtractAsync(
            url, "main", null, default), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Delegation tests for metadata calls
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTotalBranchCountAsync_DelegatesToProvider()
    {
        const string url = "https://github.com/owner/repo";
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetTotalBranchCountAsync(url, default))
                .ReturnsAsync(5);

        var fetcher = new RepositoryFetcher([provider.Object]);
        var result  = await fetcher.GetTotalBranchCountAsync(url);

        Assert.Equal(5, result);
        provider.Verify(p => p.GetTotalBranchCountAsync(url, default), Times.Once);
    }

    [Fact]
    public async Task GetTotalCommitCountAsync_DelegatesToProvider()
    {
        const string url    = "https://github.com/owner/repo";
        const string branch = "main";
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetTotalCommitCountAsync(url, branch, default))
                .ReturnsAsync(100);

        var fetcher = new RepositoryFetcher([provider.Object]);
        var result  = await fetcher.GetTotalCommitCountAsync(url, branch);

        Assert.Equal(100, result);
    }

    [Fact]
    public async Task GetTotalContributorCountAsync_DelegatesToProvider()
    {
        const string url = "https://github.com/owner/repo";
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetTotalContributorCountAsync(url, default))
                .ReturnsAsync(42);

        var fetcher = new RepositoryFetcher([provider.Object]);
        var result  = await fetcher.GetTotalContributorCountAsync(url);

        Assert.Equal(42, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetBranchesAsync / GetCommitsAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBranchesAsync_DelegatesToProvider()
    {
        const string url = "https://github.com/owner/repo";
        var branches = new List<RepoBranch> { new("main", "abc123") };
        var provider = MakeProvider(canHandle: true);
        provider.Setup(p => p.GetBranchesAsync(url, default))
                .ReturnsAsync(branches);

        var fetcher = new RepositoryFetcher([provider.Object]);
        var result  = await fetcher.GetBranchesAsync(url);

        Assert.Equal(branches.Count, result.Count());
        Assert.Equal("main",   result.First().Name);
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

        var fetcher = new RepositoryFetcher([provider.Object]);
        var result  = await fetcher.GetCommitsAsync(url);

        Assert.Equal(commits.Count, result.Count());
        Assert.Equal("abc123",        result.First().Hash);
        Assert.Equal("Initial commit",result.First().Message);
    }
}
