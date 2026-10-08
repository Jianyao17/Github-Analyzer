using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GithubAnalyzer.Shared.Git;

public sealed class GitService : IGitService
{
    private readonly GitOptions _options;
    private readonly IEnumerable<IGitProvider> _providers;
    private readonly ILogger<GitService> _logger;

    public GitService(
        IEnumerable<IGitProvider> providers,
        IOptions<GitOptions> options,
        ILogger<GitService> logger)
    {
        _providers = providers;
        _options = options.Value;
        _logger = logger;
    }

    private IGitProvider GetProvider(string repoUrl) =>
        _providers.FirstOrDefault(p => p.CanHandle(repoUrl))
        ?? throw new NotSupportedException($"Tidak ada Git provider yang dapat menangani URL: {repoUrl}");

    public async Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl, string branch = "main", string? commitHash = null, CancellationToken ct = default)
    {
        var provider = GetProvider(repoUrl);
        var reference = string.IsNullOrWhiteSpace(commitHash) ? branch : commitHash;

        // Path deterministik: SHA1(repoUrl|ref)
        var input = $"{repoUrl.Trim().ToLowerInvariant()}|{reference.Trim().ToLowerInvariant()}";
        var repoHash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(input)))[..12].ToLowerInvariant();
        var targetDir = Path.Combine(_options.StoragePath, repoHash);

        return await provider.DownloadAndExtractAsync(repoUrl, branch, commitHash, targetDir, ct);
    }

    public Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(string repoUrl, CancellationToken ct = default) =>
        GetProvider(repoUrl).GetBranchesAsync(repoUrl, ct);

    public Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(string repoUrl, string? branch = null, CancellationToken ct = default) =>
        GetProvider(repoUrl).GetCommitsAsync(repoUrl, branch, ct);

    public Task<GitCounts> GetCountsAsync(string repoUrl, string? branch = null, CancellationToken ct = default) =>
        GetProvider(repoUrl).GetCountsAsync(repoUrl, branch, ct);

    public Task<string?> GetFileContentAsync(
        string repoUrl, string commitHash, string relativePath, CancellationToken ct = default) =>
        GetProvider(repoUrl).GetFileContentAsync(repoUrl, commitHash, relativePath, ct);
}
