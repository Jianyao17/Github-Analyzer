using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Services.Repo;

public sealed class RepositoryFetcher(IEnumerable<IRepositoryProvider> providers) 
    : IRepositoryFetcher
{
    private IRepositoryProvider GetProvider(string repoUrl)
    {
        var provider = providers.FirstOrDefault(p => p.CanHandle(repoUrl));
        if (provider == null)
        {
            throw new NotSupportedException($"No repository provider found that can handle the URL: {repoUrl}");
        }
        return provider;
    }

    public Task<RepositoryResult> DownloadAndExtractAsync(string repoUrl, 
        string branch = "main", string? commitHash = null, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        return provider.DownloadAndExtractAsync(repoUrl, branch, commitHash, ct);
    }

    public Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(string repoUrl, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        return provider.GetBranchesAsync(repoUrl, ct);
    }

    public Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(string repoUrl, 
        string? branch = null, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        return provider.GetCommitsAsync(repoUrl, branch, ct);
    }

    public Task<int?> GetTotalBranchCountAsync(string repoUrl, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        return provider.GetTotalBranchCountAsync(repoUrl, ct);
    }

    public Task<int?> GetTotalCommitCountAsync(
        string repoUrl, string? branch = null, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        return provider.GetTotalCommitCountAsync(repoUrl, branch, ct);
    }

    public Task<int?> GetTotalContributorCountAsync(string repoUrl, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        return provider.GetTotalContributorCountAsync(repoUrl, ct);
    }
}
