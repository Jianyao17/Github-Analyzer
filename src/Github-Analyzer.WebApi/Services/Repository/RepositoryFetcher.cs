using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Services;

public sealed class RepositoryFetcher : IRepositoryFetcher
{
    private readonly IEnumerable<IRepositoryProvider> _providers;

    public RepositoryFetcher(IEnumerable<IRepositoryProvider> providers)
    {
        _providers = providers;
    }

    private IRepositoryProvider GetProvider(string repoUrl)
    {
        var provider = _providers.FirstOrDefault(p => p.CanHandle(repoUrl));
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
}
