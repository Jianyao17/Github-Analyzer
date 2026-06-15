using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Analysis;
using GithubAnalyzer.WebApi.Models;
using System.Security.Cryptography;
using Octokit;

namespace GithubAnalyzer.WebApi.Services.Repo;

public sealed class GithubRepositoryProvider : BaseRepositoryProvider, IRepositoryProvider
{
    private readonly HttpClient _httpClient;
    private readonly IGitHubClient _githubClient;
    private readonly AnalysisConfig _analysisConfig;
    private readonly GithubFallbackRepositoryProvider _fallbackProvider;

    public GithubRepositoryProvider(
        HttpClient httpClient, IGitHubClient githubClient, AnalysisConfig analysisConfig,
        GithubFallbackRepositoryProvider fallbackProvider, ILogger<GithubRepositoryProvider> logger)
      : base(logger)
    {
        _httpClient = httpClient;
        _githubClient = githubClient;
        _analysisConfig = analysisConfig;
        _fallbackProvider = fallbackProvider;
    }

    public bool CanHandle(string repoUrl)
    {
        return !string.IsNullOrWhiteSpace(repoUrl)
               && repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl, string branch = "main", string? commitHash = null,
        CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var reference = !string.IsNullOrWhiteSpace(commitHash) ? commitHash : branch;

        var randomSuffix  = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        var repositoryId  = GetDeterministicRepoId(repoUrl, reference, randomSuffix);
        var tempDirectory = Path.Combine(_analysisConfig.GetBaseTempPath(), repositoryId);
        var extractPath   = Path.Combine(tempDirectory, "extracted");

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(tempDirectory);
            await DownloadAndExtractZipAsync(owner, repo, reference, tempDirectory, ct);
        }
        else
        {
            Logger.LogInformation("Repository {RepoUrl} at reference {Reference} already exists at {ExtractPath}",
                repoUrl, reference, extractPath);
        }

        string? description = null;
        string? authorName = null;
        string? lastCommitHash = null;
        DateTime? lastCommitAtUtc = null;

        try
        {
            var repository = await _githubClient.Repository.Get(owner, repo);
            var commit = await _githubClient.Repository.Commit.Get(owner, repo, reference);

            description = repository.Description;
            authorName = commit.Commit.Author.Name;
            lastCommitAtUtc = commit.Commit.Author.Date.UtcDateTime;
            lastCommitHash = commit.Sha;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Octokit failed fetching metadata for {Owner}/{Repo}, using fallback...", owner, repo);

            var commits = await _fallbackProvider.GetCommitsAsync(repoUrl, reference, ct);
            var latestCommit = commits.FirstOrDefault();
            if (latestCommit != null)
            {
                authorName = latestCommit.Author;
                lastCommitHash = latestCommit.Hash;
                lastCommitAtUtc = latestCommit.Date.UtcDateTime;
            }
        }

        var branchName = string.IsNullOrWhiteSpace(commitHash) ? branch : null;

        return new RepositoryResult(
            ExtractPath:     extractPath,
            RepositoryUrl:   repoUrl,
            RepositoryName:  repo,
            Description:     description,
            AuthorName:      authorName,
            BranchName:      branchName,
            LastCommitHash:  lastCommitHash,
            LastCommitAtUtc: lastCommitAtUtc
        );
    }

    private async Task DownloadAndExtractZipAsync(
      string owner, string repo, string reference,
      string tempDirectory, CancellationToken ct)
    {
        // Using authenticated http client for zip download (which are manually downloaded to disk)
        var zipUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball/{reference}";
        Logger.LogInformation("Downloading repository zip from {ZipUrl}", zipUrl);

        var response = await _httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var zipFilePath = Path.Combine(tempDirectory, "repo.zip");
        using (var fs = new FileStream(zipFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await response.Content.CopyToAsync(fs, ct);
        }

        ExtractZipAndGetRootPath(zipFilePath, Path.Combine(tempDirectory, "extracted"));
    }

    public async Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(
      string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        try
        {
            var branches = await _githubClient.Repository.Branch.GetAll(owner, repo);
            return branches.Select(b => new RepoBranch(b.Name, b.Commit.Sha)).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Octokit failed GetBranchesAsync, using fallback...");
            return await _fallbackProvider.GetBranchesAsync(repoUrl, ct);
        }
    }

    public async Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(
      string repoUrl, string? branch = null, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        try
        {
            var request = new CommitRequest();
            if (!string.IsNullOrWhiteSpace(branch))
            {
                request.Sha = branch;
            }

            var commits = await _githubClient.Repository.Commit.GetAll(owner, repo, request);
            return commits.Select(c => new RepoCommit(
                c.Sha,
                c.Commit.Message,
                c.Commit.Author.Name,
                c.Commit.Author.Date)).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Octokit failed GetCommitsAsync, using fallback...");
            return await _fallbackProvider.GetCommitsAsync(repoUrl, branch, ct);
        }
    }

    public async Task<int?> GetTotalBranchCountAsync(string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        try
        {
            var url = new Uri($"/repos/{owner}/{repo}/branches?per_page=1", UriKind.Relative);
            return await FetchCountViaLinkHeaderAsync(url, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Octokit failed GetTotalBranchCountAsync, using fallback...");
            return await _fallbackProvider.GetTotalBranchCountAsync(repoUrl, ct);
        }
    }

    public async Task<int?> GetTotalCommitCountAsync(
      string repoUrl, string? branch = null, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        try
        {
            var query = string.IsNullOrWhiteSpace(branch) ? "?per_page=1" : $"?per_page=1&sha={Uri.EscapeDataString(branch)}";
            var url = new Uri($"/repos/{owner}/{repo}/commits{query}", UriKind.Relative);
            return await FetchCountViaLinkHeaderAsync(url, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Octokit failed GetTotalCommitCountAsync, using fallback...");
            return await _fallbackProvider.GetTotalCommitCountAsync(repoUrl, branch, ct);
        }
    }

    public async Task<int?> GetTotalContributorCountAsync(
      string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        try
        {
            var url = new Uri($"/repos/{owner}/{repo}/contributors?per_page=1&anon=false", UriKind.Relative);
            return await FetchCountViaLinkHeaderAsync(url, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Octokit failed GetTotalContributorCountAsync, using fallback...");
            return await _fallbackProvider.GetTotalContributorCountAsync(repoUrl, ct);
        }
    }

    private async Task<int?> FetchCountViaLinkHeaderAsync(Uri uri, CancellationToken ct)
    {
        var response = await _githubClient.Connection.Get<IReadOnlyList<object>>(uri, new Dictionary<string, string>(), null);
        if (response.HttpResponse.ApiInfo.Links.TryGetValue("last", out var lastLink))
        {
            var query = System.Web.HttpUtility.ParseQueryString(lastLink.Query);
            if (int.TryParse(query["page"], out var page))
                return page;
        }

        if (response.Body != null)
          return response.Body.Count;

        return null;
    }

    private static (string Owner, string Repo) ParseOwnerAndRepo(string url)
    {
        var parts = url.TrimEnd('/').Split('/');
        if (parts.Length < 2)
            throw new ArgumentException("Invalid GitHub URL format.", nameof(url));

        return (parts[^2], parts[^1]);
    }
}
