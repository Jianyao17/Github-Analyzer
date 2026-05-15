using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Interfaces;

public interface IRepositoryProvider
{
    bool CanHandle(string repoUrl);
    
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

    /// <summary>
    /// Returns the total number of branches for the repository.
    /// Uses the GitHub Link header trick (per_page=1) so no large payload is transferred.
    /// Returns <see langword="null"/> if the request fails.
    /// </summary>
    Task<int?> GetTotalBranchCountAsync(string repoUrl, CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of commits, optionally filtered to a specific branch.
    /// Uses the GitHub Link header trick (per_page=1) so no large payload is transferred.
    /// Returns <see langword="null"/> if the request fails.
    /// </summary>
    Task<int?> GetTotalCommitCountAsync(
        string repoUrl, string? branch = null, CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of unique contributors to the repository.
    /// Uses the GitHub Link header trick (per_page=1) so no large payload is transferred.
    /// Returns <see langword="null"/> if the request fails.
    /// </summary>
    Task<int?> GetTotalContributorCountAsync(string repoUrl, CancellationToken ct = default);
}
