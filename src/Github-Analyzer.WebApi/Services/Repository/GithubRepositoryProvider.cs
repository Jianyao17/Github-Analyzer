using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Config;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text.Json;
using System.Text;
using GithubAnalyzer.WebApi.Models.Analysis;

namespace GithubAnalyzer.WebApi.Services.Repo;

public sealed class GithubRepositoryProvider : IRepositoryProvider
{
    // -------------------------------------------------------------------------
    // GitHub API URL Templates
    // -------------------------------------------------------------------------
    private const string GithubApiBase         = "https://api.github.com/repos/{0}/{1}";
    private const string ZipballEndpoint       = "/zipball/{2}";           // /{reference}
    private const string BranchesEndpoint      = "/branches";
    private const string CommitsEndpoint       = "/commits";
    private const string CommitEndpoint        = "/commits/{2}";           // /{reference}
    private const string ContributorsEndpoint  = "/contributors";

    // -------------------------------------------------------------------------
    // Fields & Constructor
    // -------------------------------------------------------------------------
    private readonly HttpClient _httpClient;
    private readonly AnalysisConfig _analysisConfig;
    private readonly ILogger<GithubRepositoryProvider> _logger;

    public GithubRepositoryProvider(
        HttpClient httpClient, AnalysisConfig analysisConfig,
        ILogger<GithubRepositoryProvider> logger)
    {
        _httpClient     = httpClient;
        _analysisConfig = analysisConfig;
        _logger         = logger;
    }

    // -------------------------------------------------------------------------
    // IRepositoryProvider Implementation
    // -------------------------------------------------------------------------

    public bool CanHandle(string repoUrl)
    {
        return !string.IsNullOrWhiteSpace(repoUrl)
               && repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<RepositoryResult> DownloadAndExtractAsync(
        string repoUrl,                 // Url of the repository
        string branch = "main",         // Branch to download
        string? commitHash = null,      // Commit hash to download
        CancellationToken ct = default) // Cancellation token
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);

        // Prefer commit hash over branch name when both are provided
        var reference = !string.IsNullOrWhiteSpace(commitHash) ? commitHash : branch;

        var randomSuffix  = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        var repositoryId  = GetDeterministicRepoId(repoUrl, reference, randomSuffix);
        var tempDirectory = Path.Combine(_analysisConfig.GetBaseTempPath(), repositoryId);
        var extractPath   = Path.Combine(tempDirectory, "extracted");

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(tempDirectory);

            // Step 1: Download and extract the repository zip (must complete first)
            await DownloadAndExtractZipAsync(owner, repo, reference, tempDirectory, ct);
        }
        else
        {
            _logger.LogInformation("Repository {RepoUrl} at reference {Reference} already exists at {ExtractPath}",
                repoUrl, reference, extractPath);
        }

        // Step 2: Fetch repo description and commit metadata in parallel
        // (both are independent GitHub API calls, so no need to wait for one before the other)
        var descriptionTask    = FetchRepoDescriptionAsync(owner, repo, ct);
        var commitMetadataTask = FetchCommitMetadataAsync(owner, repo, reference, ct);
        await Task.WhenAll(descriptionTask, commitMetadataTask);

        var description                                   = await descriptionTask;
        var (authorName, lastCommitHash, lastCommitAtUtc) = await commitMetadataTask;

        // BranchName is null when a specific commit hash was requested
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

    private static string GetDeterministicRepoId(
        string repoUrl, string reference, string randomSuffix)
    {
        // Create a unique ID per request to allow multiple analyses of the same repo.
        var input = $"{repoUrl.ToLowerInvariant()}|{reference.ToLowerInvariant()}|{randomSuffix}";

        // Hash the input to ensure a fixed length 
        // and avoid filesystem issues with long URLs 
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash  = SHA1.HashData(bytes);

        // Use the first 12 characters of the hex string as the ID 
        // (enough for uniqueness while keeping it short)
        return Convert.ToHexString(hash).ToLowerInvariant().Substring(0, 12); 
    }

    public async Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(
        string repoUrl, // Url of the repository
        CancellationToken cancellationToken = default) 
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = BuildRepoUrl(owner, repo, BranchesEndpoint);

        var response = await SendGetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream   = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var branches = new List<RepoBranch>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var name = element.GetProperty("name").GetString() ?? string.Empty;
            var sha  = element.GetProperty("commit").GetProperty("sha").GetString() ?? string.Empty;
            branches.Add(new RepoBranch(name, sha));
        }

        return branches;
    }

    public async Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(
        string repoUrl,        // Url of the repository
        string? branch = null, // Branch to get commits from
        CancellationToken cancellationToken = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);

        // Append ?sha=<branch> query param when filtering by branch
        var path = !string.IsNullOrWhiteSpace(branch)
            ? $"{CommitsEndpoint}?sha={branch}"
            : CommitsEndpoint;

        var url = BuildRepoUrl(owner, repo, path);

        var response = await SendGetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream   = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var commits = new List<RepoCommit>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var sha        = element.GetProperty("sha").GetString() ?? string.Empty;
            var commitNode = element.GetProperty("commit");
            var authorNode = commitNode.GetProperty("author");

            var message    = commitNode.GetProperty("message").GetString() ?? string.Empty;
            var authorName = authorNode.GetProperty("name").GetString() ?? string.Empty;
            var dateStr    = authorNode.GetProperty("date").GetString();

            var date = DateTimeOffset.TryParse(dateStr, out var parsed) ? parsed : DateTimeOffset.MinValue;

            commits.Add(new RepoCommit(sha, message, authorName, date));
        }

        return commits;
    }

    public async Task<int?> GetTotalBranchCountAsync(
        string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = BuildRepoUrl(owner, repo, BranchesEndpoint) + "?per_page=1";
        return await FetchCountViaLinkHeaderAsync(url, ct);
    }

    public async Task<int?> GetTotalCommitCountAsync(
        string repoUrl, string? branch = null, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var query = string.IsNullOrWhiteSpace(branch)
            ? "?per_page=1"
            : $"?per_page=1&sha={Uri.EscapeDataString(branch)}";
        var url = BuildRepoUrl(owner, repo, CommitsEndpoint) + query;
        return await FetchCountViaLinkHeaderAsync(url, ct);
    }

    public async Task<int?> GetTotalContributorCountAsync(
        string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = BuildRepoUrl(owner, repo, ContributorsEndpoint) + "?per_page=1&anon=false";
        return await FetchCountViaLinkHeaderAsync(url, ct);
    }

    // -------------------------------------------------------------------------
    // Private Helpers — Download & Extract
    // -------------------------------------------------------------------------

    /// <summary>
    /// Downloads the repository zip from GitHub and extracts it to <paramref name="tempDirectory"/>.
    /// Returns the path to the extracted folder.
    /// </summary>
    private async Task<string> DownloadAndExtractZipAsync(
        string owner, string repo, string reference,
        string tempDirectory, CancellationToken ct)
    {
        var zipUrl = BuildRepoUrl(owner, repo, ZipballEndpoint, reference);
        _logger.LogInformation("Downloading repository zip from {ZipUrl}", zipUrl);

        var response = await SendGetAsync(zipUrl, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Repository not found or reference missing. URL tried: {ZipUrl}", zipUrl);
            throw new Exception(
                "Repository not found. Please ensure the URL is correct, " +
                "the repository is public, and the branch/commit exists.");
        }

        response.EnsureSuccessStatusCode();

        // Save zip to disk, then extract and clean up
        var zipFilePath = Path.Combine(tempDirectory, "repo.zip");
        using (var fs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await response.Content.CopyToAsync(fs, ct);
        }

        var extractPath = Path.Combine(tempDirectory, "extracted");
        ZipFile.ExtractToDirectory(zipFilePath, extractPath, overwriteFiles: true);

        File.Delete(zipFilePath);

        return extractPath;
    }

    /// <summary>
    /// Fetches the repository description from the GitHub Repo API.
    /// Returns <see langword="null"/> if the request fails or the field is absent.
    /// </summary>
    private async Task<string?> FetchRepoDescriptionAsync(
        string owner, string repo, CancellationToken ct)
    {
        var url      = BuildRepoUrl(owner, repo);
        var response = await SendGetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        using var stream   = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var descElement = document.RootElement.GetProperty("description");
        return descElement.ValueKind == JsonValueKind.String
            ? descElement.GetString()
            : null;
    }

    /// <summary>
    /// Fetches commit author name, commit hash, and commit date for the given reference.
    /// Returns nulls if the request fails.
    /// </summary>
    private async Task<(string? AuthorName, string? LastCommitHash, DateTime? LastCommitAtUtc)>
        FetchCommitMetadataAsync(string owner, string repo, string reference, CancellationToken ct)
    {
        var url      = BuildRepoUrl(owner, repo, CommitEndpoint, reference);
        var response = await SendGetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return (null, null, null);

        using var stream   = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root       = document.RootElement;
        var commitNode = root.GetProperty("commit");
        var authorNode = commitNode.GetProperty("author");

        var lastCommitHash  = root.GetProperty("sha").GetString();
        var authorName      = authorNode.GetProperty("name").GetString();
        var dateStr         = authorNode.GetProperty("date").GetString();

        DateTime? lastCommitAtUtc = DateTimeOffset.TryParse(dateStr, out var parsedDate)
            ? parsedDate.UtcDateTime
            : null;

        return (authorName, lastCommitHash, lastCommitAtUtc);
    }

    // -------------------------------------------------------------------------
    // Private Helpers — URL Building & HTTP
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a GitHub API URL for the given owner/repo.
    /// Optionally appends <paramref name="endpointTemplate"/> (which may contain {2} for a reference).
    /// </summary>
    private static string BuildRepoUrl(string owner, string repo,
        string? endpointTemplate = null, string? reference = null)
    {
        var baseUrl = string.Format(GithubApiBase, owner, repo);

        if (endpointTemplate is null)
            return baseUrl;

        return reference is not null
            ? baseUrl + string.Format(endpointTemplate, owner, repo, reference)
            : baseUrl + endpointTemplate;
    }

    private Task<HttpResponseMessage> SendGetAsync(string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>
    /// Sends a HEAD-like GET request (per_page=1) and reads the <c>Link</c> header to determine
    /// the total item count without downloading the full payload.
    /// Returns <see langword="null"/> if the request fails.
    /// Returns <c>1</c> when there is at least one item but no pagination <c>Link</c> header present.
    /// </summary>
    private async Task<int?> FetchCountViaLinkHeaderAsync(string url, CancellationToken ct)
    {
        try
        {
            var response = await SendGetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            // If the Link header is present, parse the last page number → total count
            if (response.Headers.TryGetValues("Link", out var linkValues))
            {
                var linkHeader = string.Join(", ", linkValues);
                var count = ParseLinkHeaderLastPage(linkHeader);
                if (count.HasValue)
                    return count;
            }

            // No Link header means ≤ per_page items — read the actual array length
            using var stream   = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch count from {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Parses a GitHub <c>Link</c> response header and extracts the page number of <c>rel="last"</c>,
    /// which equals the total number of pages (and therefore items when per_page=1).
    /// </summary>
    /// <example>Link: &lt;...?page=3&gt;; rel="next", &lt;...?page=42&gt;; rel="last"</example>
    private static int? ParseLinkHeaderLastPage(string linkHeader)
    {
        // Each segment looks like: <https://...?page=N>; rel="last"
        foreach (var segment in linkHeader.Split(','))
        {
            var parts = segment.Trim().Split(';');
            if (parts.Length < 2)
                continue;

            var rel = parts[1].Trim();
            if (!rel.Equals("rel=\"last\"", StringComparison.OrdinalIgnoreCase))
                continue;

            // Extract URL between < >
            var urlPart = parts[0].Trim().TrimStart('<').TrimEnd('>');
            var uri     = new Uri(urlPart);
            var query   = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var pageStr = query["page"];

            if (int.TryParse(pageStr, out var page))
                return page;
        }

        return null;
    }

    /// <summary>
    /// Parses a GitHub repository URL and extracts the owner and repository name.
    /// Supports both <c>https://github.com/owner/repo</c> and <c>github.com/owner/repo</c> formats.
    /// </summary>
    private static (string Owner, string Repo) ParseOwnerAndRepo(string url)
    {
        var parts = url.TrimEnd('/').Split('/');
        if (parts.Length < 2)
            throw new ArgumentException("Invalid GitHub URL format.", nameof(url));

        // e.g. https://github.com/owner/repo  →  parts[^2] = owner, parts[^1] = repo
        return (parts[^2], parts[^1]);
    }
}
