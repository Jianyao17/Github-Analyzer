using System.Net.Http.Json;
using System.IO.Compression;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace GithubAnalyzer.Shared.Git.Providers;

public sealed class GitHubProvider : IGitProvider
{
    private const string ApiBaseUrl = "https://api.github.com";
    private const string RawBaseUrl = "https://raw.githubusercontent.com";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubProvider> _logger;
    public GitProviderType ProviderType => GitProviderType.GitHub;

    public GitHubProvider(HttpClient httpClient, ILogger<GitHubProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }


    public bool CanHandle(string repoUrl) =>
        !string.IsNullOrWhiteSpace(repoUrl) &&
        repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);

    public async Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl, string branch, string? commitHash, string targetDir, CancellationToken ct)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var reference = !string.IsNullOrWhiteSpace(commitHash) ? commitHash : branch;
        var extractedPath = Path.Combine(targetDir, "extracted");

        if (Directory.Exists(extractedPath) &&
            Directory.EnumerateFileSystemEntries(extractedPath).Any())
        {
            _logger.LogRepositoryAlreadyExists(owner, repo, reference, extractedPath);
        }
        else
        {
            Directory.CreateDirectory(targetDir);
            var zipPath = Path.Combine(targetDir, "repo.zip");
            var zipUrl = $"{ApiBaseUrl}/repos/{owner}/{repo}/zipball/{reference}";

            _logger.LogDownloadingZipball(owner, repo, reference, zipUrl);

            using var response = await _httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            ZipFile.ExtractToDirectory(zipPath, extractedPath, overwriteFiles: true);
            File.Delete(zipPath);

            // Flatten subfolder jika zipball dibungkus dalam single root folder
            FlattenSingleFolder(extractedPath);
            _logger.LogRepositoryExtracted(owner, repo, extractedPath);
        }

        // Ambil metadata ringkas (deskripsi & last commit) via API
        string? author = null;
        string? description = null;
        string? resolvedHash = commitHash;
        DateTime? commitDate = null;

        try
        {
            var repoUrlEndpoint = $"{ApiBaseUrl}/repos/{owner}/{repo}";
            var repoDto = await _httpClient.GetFromJsonAsync(repoUrlEndpoint,
                GitHubJsonContext.Default.GitHubRepoDto, ct);
            description = repoDto?.Description;

            var commitUrlEndpoint = $"{ApiBaseUrl}/repos/{owner}/{repo}/commits/{reference}";
            var commitDto = await _httpClient.GetFromJsonAsync( commitUrlEndpoint,
                GitHubJsonContext.Default.GitHubCommitItemDto, ct);

            if (commitDto != null)
            {
                resolvedHash = commitDto.Sha;
                author = commitDto.Commit?.Author?.Name;
                commitDate = commitDto.Commit?.Author?.Date.UtcDateTime;
            }
        }
        catch (Exception ex)
        {
            _logger.LogMetadataFetchFailed(ex, owner, repo);
        }

        return new RepositoryResult(
            ExtractPath: extractedPath,
            RepositoryUrl: repoUrl,
            RepositoryName: repo,
            Description: description,
            AuthorName: author,
            BranchName: string.IsNullOrWhiteSpace(commitHash) ? branch : null,
            LastCommitHash: resolvedHash,
            LastCommitAtUtc: commitDate
        );
    }

    public async Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(string repoUrl, CancellationToken ct)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var branchesUrl = $"{ApiBaseUrl}/repos/{owner}/{repo}/branches";

        var list = await _httpClient.GetFromJsonAsync(branchesUrl,
            GitHubJsonContext.Default.ListGitHubBranchDto, ct);

        return list?.Select(b => new RepoBranch(b.Name, b.Commit.Sha)).ToList()
               ?? (IReadOnlyList<RepoBranch>)Array.Empty<RepoBranch>();
    }

    public async Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(string repoUrl, string? branch, CancellationToken ct)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var query = !string.IsNullOrWhiteSpace(branch) ? $"?sha={Uri.EscapeDataString(branch)}" : "";
        var commitsUrl = $"{ApiBaseUrl}/repos/{owner}/{repo}/commits{query}";

        var list = await _httpClient.GetFromJsonAsync(commitsUrl,
            GitHubJsonContext.Default.ListGitHubCommitItemDto, ct);

        return list?.Select(c
          => new RepoCommit(
            c.Sha,
            c.Commit?.Message ?? "",
            c.Commit?.Author?.Name ?? "",
            c.Commit?.Author?.Date ?? DateTimeOffset.MinValue
        )).ToList() ?? (IReadOnlyList<RepoCommit>)Array.Empty<RepoCommit>();
    }

    public async Task<GitCounts> GetCountsAsync(string repoUrl, string? branch, CancellationToken ct)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var branchQuery = !string.IsNullOrWhiteSpace(branch)
          ? $"?per_page=1&sha={Uri.EscapeDataString(branch)}"
          : "?per_page=1";

        var branchesUrl = $"{ApiBaseUrl}/repos/{owner}/{repo}/branches?per_page=1";
        var commitsUrl = $"{ApiBaseUrl}/repos/{owner}/{repo}/commits{branchQuery}";
        var contributorsUrl = $"{ApiBaseUrl}/repos/{owner}/{repo}/contributors?per_page=1&anon=false";

        var branchesTask = FetchLinkCountAsync(branchesUrl, ct);
        var commitsTask = FetchLinkCountAsync(commitsUrl, ct);
        var contributorsTask = FetchLinkCountAsync(contributorsUrl, ct);

        await Task.WhenAll(branchesTask, commitsTask, contributorsTask);
        return new GitCounts(await branchesTask, await commitsTask, await contributorsTask);
    }

    public async Task<string?> GetFileContentAsync(string repoUrl, string commitHash, string relativePath, CancellationToken ct)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var cleanPath = relativePath.Replace('\\', '/').TrimStart('/');
        var rawFileUrl = $"{RawBaseUrl}/{owner}/{repo}/{commitHash}/{cleanPath}";

        using var response = await _httpClient.GetAsync(rawFileUrl, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helper Methods
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mengambil total count dari header 'Link' (rel="last") dengan query per_page=1 tanpa mengunduh seluruh payload data.
    /// </summary>
    private async Task<int?> FetchLinkCountAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            if (response.Headers.TryGetValues("Link", out var links))
            {
                var linkHeader = string.Join(",", links);
                foreach (var part in linkHeader.Split(','))
                {
                    if (part.Contains("rel=\"last\""))
                    {
                        var start = part.IndexOf('<') + 1;
                        var end = part.IndexOf('>');
                        if (start > 0 && end > start && Uri.TryCreate(part[start..end], UriKind.Absolute, out var uri))
                        {
                            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                            if (int.TryParse(query["page"], out var page)) return page;
                        }
                    }
                }
            }
            return 1;
        }
        catch { return null; }
    }

    /// <summary>
    /// Menghilangkan wrapper subfolder tunggal jika zipball GitHub mengekstrak ke dalam satu direktori pembungkus.
    /// </summary>
    private static void FlattenSingleFolder(string extractPath)
    {
        var subDirs = Directory.GetDirectories(extractPath);
        var subFiles = Directory.GetFiles(extractPath);
        if (subDirs.Length == 1 && subFiles.Length == 0)
        {
            var nested = subDirs[0];
            var tempMove = extractPath + "_temp";
            Directory.Move(nested, tempMove);
            Directory.Delete(extractPath, true);
            Directory.Move(tempMove, extractPath);
        }
    }

    /// <summary>
    /// Mengurai owner dan nama repositori dari URL Git yang diberikan.
    /// </summary>
    private static (string Owner, string Repo) ParseOwnerAndRepo(string url)
    {
        var segments = url.TrimEnd('/').Split('/');
        if (segments.Length < 2) throw new ArgumentException("URL repositori tidak valid", nameof(url));
        return (segments[^2], segments[^1].Replace(".git", ""));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DTOs & Source Generator Context (Diletakkan di bagian paling bawah file)
// ─────────────────────────────────────────────────────────────────────────────

internal sealed record GitHubRepoDto(string? Description);
internal sealed record GitHubBranchDto(string Name, GitHubCommitRefDto Commit);

internal sealed record GitHubCommitRefDto(string Sha);
internal sealed record GitHubCommitItemDto(string Sha, GitHubCommitDetailDto? Commit);
internal sealed record GitHubCommitDetailDto(string? Message, GitHubAuthorDto? Author);

internal sealed record GitHubAuthorDto(string? Name, DateTimeOffset Date);

[JsonSerializable(typeof(GitHubRepoDto))]
[JsonSerializable(typeof(GitHubCommitItemDto))]
[JsonSerializable(typeof(List<GitHubBranchDto>))]
[JsonSerializable(typeof(List<GitHubCommitItemDto>))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class GitHubJsonContext : JsonSerializerContext { }
