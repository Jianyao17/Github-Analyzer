using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;
using System.IO.Compression;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Services;

public sealed class GithubRepositoryProvider : IRepositoryProvider
{
    private readonly HttpClient _httpClient;
    private readonly RepoConfig _repoConfig;
    private readonly ILogger<GithubRepositoryProvider> _logger;

    public GithubRepositoryProvider(
        HttpClient httpClient, RepoConfig repoConfig,
        ILogger<GithubRepositoryProvider> logger)
    {
        _httpClient = httpClient;
        _repoConfig = repoConfig;
        _logger = logger;
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
        var repoId = Guid.NewGuid().ToString("N"); 
        var tempDirectory = Path.Combine(_repoConfig.GetBaseTempPath(), repoId);
        
        Directory.CreateDirectory(tempDirectory);
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        
        // Use commit hash if provided, otherwise fallback to branch
        var reference = !string.IsNullOrWhiteSpace(commitHash) ? commitHash : branch;
        var zipUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball/{reference}";

        try
        {
            _logger.LogInformation("Downloading from {ZipUrl}", zipUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, zipUrl);
            
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Repository not found or branch missing. URL tried: {ZipUrl}", zipUrl);
                throw new Exception("Repository not found. Please ensure the URL is correct, the repository is public, and the branch/commit exists.");
            }
            
            response.EnsureSuccessStatusCode();

            var zipFilePath = Path.Combine(tempDirectory, "repo.zip");
            using (var fs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            var extractPath = Path.Combine(tempDirectory, "extracted");
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
            File.Delete(zipFilePath);

            // Fetch repo details for metadata
            var repoApiUrl = $"https://api.github.com/repos/{owner}/{repo}";
            var repoRequest = new HttpRequestMessage(HttpMethod.Get, repoApiUrl);
            var repoResponse = await _httpClient.SendAsync(repoRequest, ct);
            string? description = null;

            if (repoResponse.IsSuccessStatusCode)
            {
                using var repoStream = await repoResponse.Content.ReadAsStreamAsync(ct);
                using var repoDoc = await JsonDocument.ParseAsync(repoStream, cancellationToken: ct);
                var descElement = repoDoc.RootElement.GetProperty("description");
                
                description = descElement.ValueKind == JsonValueKind.String 
                    ? descElement.GetString() : null;
            }

            // Fetch commit details for metadata
            var commitApiUrl = $"https://api.github.com/repos/{owner}/{repo}/commits/{reference}";
            var commitRequest = new HttpRequestMessage(HttpMethod.Get, commitApiUrl);
            var commitResponse = await _httpClient.SendAsync(commitRequest, ct);

            string? authorName = null;
            string? lastCommitHash = null;
            DateTime? lastCommitAtUtc = null;

            if (commitResponse.IsSuccessStatusCode)
            {
                using var commitStream = await commitResponse.Content.ReadAsStreamAsync(ct);
                using var commitDoc = await JsonDocument.ParseAsync(commitStream, cancellationToken: ct);
                lastCommitHash = commitDoc.RootElement.GetProperty("sha").GetString();
                
                var commitNode = commitDoc.RootElement.GetProperty("commit");
                var authorNode = commitNode.GetProperty("author");
                authorName = authorNode.GetProperty("name").GetString();
                var dateStr = authorNode.GetProperty("date").GetString();
                
                if (DateTimeOffset.TryParse(dateStr, out var parsedDate)) 
                    lastCommitAtUtc = parsedDate.UtcDateTime;
            }

            var branchName = string
                .IsNullOrWhiteSpace(commitHash) 
                ? branch : null;
            
            return new RepositoryResult(
                ExtractPath: extractPath,
                RepositoryUrl: repoUrl,
                RepositoryName: repo,

                Description: description,
                AuthorName: authorName,
                BranchName: branchName,
                
                LastCommitHash: lastCommitHash,
                LastCommitAtUtc: lastCommitAtUtc
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download or extract repository {RepoUrl} at reference {Ref}", repoUrl, reference);
            throw;
        }
    }

    public async Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(
        string repoUrl, CancellationToken cancellationToken = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = $"https://api.github.com/repos/{owner}/{repo}/branches";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        
        var branches = new List<RepoBranch>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var name = element.GetProperty("name").GetString() ?? string.Empty;
            var sha = element.GetProperty("commit").GetProperty("sha").GetString() ?? string.Empty;
            branches.Add(new RepoBranch(name, sha));
        }

        return branches;
    }

    public async Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(string repoUrl, 
        string? branch = null, CancellationToken cancellationToken = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = $"https://api.github.com/repos/{owner}/{repo}/commits";
        if (!string.IsNullOrWhiteSpace(branch))
        {
            url += $"?sha={branch}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var commits = new List<RepoCommit>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var sha = element.GetProperty("sha").GetString() ?? string.Empty;
            
            var commitNode = element.GetProperty("commit");
            var message = commitNode.GetProperty("message").GetString() ?? string.Empty;
            
            var authorNode = commitNode.GetProperty("author");
            var authorName = authorNode.GetProperty("name").GetString() ?? string.Empty;
            var dateStr = authorNode.GetProperty("date").GetString();
            
            var date = DateTimeOffset.TryParse(dateStr, out var parsedDate) ? parsedDate : DateTimeOffset.MinValue;
            
            commits.Add(new RepoCommit(sha, message, authorName, date));
        }

        return commits;
    }

    private static (string Owner, string Repo) 
        ParseOwnerAndRepo(string url)
    {
        var parts = url.TrimEnd('/').Split('/');
        if (parts.Length < 2) throw new ArgumentException("Invalid GitHub URL format.");
        
        // e.g. https://github.com/owner/repo
        return (parts[^2], parts[^1]);
    }
}
