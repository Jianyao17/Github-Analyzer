using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Interfaces;

public interface IRepositoryFetcher
{
    Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl, 
        string branch = "main", 
        string? commitHash = null, 
        CancellationToken ct = default);
    
    Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(
        string repoUrl, CancellationToken ct = default);
    
    Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(
        string repoUrl, string? branch = null, 
        CancellationToken ct = default);

    /// <inheritdoc cref="IRepositoryProvider.GetTotalBranchCountAsync"/>
    Task<int?> GetTotalBranchCountAsync(string repoUrl, CancellationToken ct = default);

    /// <inheritdoc cref="IRepositoryProvider.GetTotalCommitCountAsync"/>
    Task<int?> GetTotalCommitCountAsync(
        string repoUrl, string? branch = null, CancellationToken ct = default);

    /// <inheritdoc cref="IRepositoryProvider.GetTotalContributorCountAsync"/>
    Task<int?> GetTotalContributorCountAsync(string repoUrl, CancellationToken ct = default);
}
