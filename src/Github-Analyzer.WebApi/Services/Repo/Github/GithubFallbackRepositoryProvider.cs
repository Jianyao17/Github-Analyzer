using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Analysis;
using GithubAnalyzer.WebApi.Models;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Services.Repo;

public class GithubFallbackRepositoryProvider : BaseRepositoryProvider, IRepositoryProvider
{
    private const string GithubApiBase = "https://api.github.com/repos";

    private readonly HttpClient _httpClient;

    public GithubFallbackRepositoryProvider(HttpClient httpClient,
      ILogger<GithubFallbackRepositoryProvider> logger) : base(logger)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(string repoUrl)
    {
        return !string.IsNullOrWhiteSpace(repoUrl) &&
               repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public Task<RepositoryResult> DownloadAndExtractAsync(
      string repoUrl, string branch = "main", string? commitHash = null,
      CancellationToken ct = default)
    {
        // Fallback is primarily designed to handle API read endpoints (metadata).
        // The main provider's zipball streaming logic already uses pure HttpClient,
        // avoiding memory bloat, thus no fallback needed here unless specifically asked.
        throw new NotSupportedException("Fallback should only be used for direct API read endpoints.");
    }

    public async Task<IReadOnlyList<RepoBranch>> GetBranchesAsync(
      string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = $"{GithubApiBase}/{owner}/{repo}/branches";

        using var response = await SendGetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var branches = new List<RepoBranch>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var sha = string.Empty;
            var name = element.TryGetProperty("name", out var nameProp)
              ? nameProp.GetString() ?? string.Empty
              : string.Empty;

            if (element.TryGetProperty("commit", out var commitProp) &&
                commitProp.TryGetProperty("sha", out var shaProp))
            {
                sha = shaProp.GetString() ?? string.Empty;
            }

            branches.Add(new RepoBranch(name, sha));
        }

        return branches;
    }

    public async Task<IReadOnlyList<RepoCommit>> GetCommitsAsync(
      string repoUrl, string? branch = null, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var query = !string.IsNullOrWhiteSpace(branch) ? $"?sha={Uri.EscapeDataString(branch)}" : string.Empty;
        var url = $"{GithubApiBase}/{owner}/{repo}/commits{query}";

        using var response = await SendGetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var commits = new List<RepoCommit>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var sha = element.TryGetProperty("sha", out var shaProp)
              ? shaProp.GetString() ?? string.Empty
              : string.Empty;

            if (!element.TryGetProperty("commit", out var commitNode))
                continue;

            var message = commitNode.TryGetProperty("message", out var msgNode)
              ? msgNode.GetString() ?? string.Empty
              : string.Empty;

            var authorName = string.Empty;
            var date = DateTimeOffset.MinValue;

            if (commitNode.TryGetProperty("author", out var authorNode))
            {
                var dateStr = authorNode.TryGetProperty("date", out var dateNode)
                  ? dateNode.GetString() : null;

                authorName = authorNode.TryGetProperty("name", out var nameNode)
                  ? nameNode.GetString() ?? string.Empty : string.Empty;

                date = DateTimeOffset.TryParse(dateStr, out var parsed)
                  ? parsed : DateTimeOffset.MinValue;
            }
            commits.Add(new RepoCommit(sha, message, authorName, date));
        }
        return commits;
    }

    public Task<int?> GetTotalBranchCountAsync(string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = $"{GithubApiBase}/{owner}/{repo}/branches?per_page=1";

        return FetchCountViaLinkHeaderAsync(url, ct);
    }

    public Task<int?> GetTotalCommitCountAsync(string repoUrl, string? branch = null, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var query = string.IsNullOrWhiteSpace(branch)
          ? "?per_page=1" : $"?per_page=1&sha={Uri.EscapeDataString(branch)}";

        var url = $"{GithubApiBase}/{owner}/{repo}/commits{query}";
        return FetchCountViaLinkHeaderAsync(url, ct);
    }

    public Task<int?> GetTotalContributorCountAsync(string repoUrl, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);
        var url = $"{GithubApiBase}/{owner}/{repo}/contributors?per_page=1&anon=false";

        return FetchCountViaLinkHeaderAsync(url, ct);
    }

    private Task<HttpResponseMessage> SendGetAsync(string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private async Task<int?> FetchCountViaLinkHeaderAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await SendGetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            if (response.Headers.TryGetValues("Link", out var linkValues))
            {
                var linkHeader = string.Join(", ", linkValues);
                var count = ParseLinkHeaderLastPage(linkHeader);
                if (count.HasValue)
                    return count;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch count from {Url}", url);
            return null;
        }
    }

    private static int? ParseLinkHeaderLastPage(string linkHeader)
    {
        foreach (var segment in linkHeader.Split(','))
        {
            var parts = segment.Trim().Split(';');
            if (parts.Length < 2) continue;

            var rel = parts[1].Trim();
            if (!rel.Equals("rel=\"last\"", StringComparison.OrdinalIgnoreCase))
                continue;

            var urlPart = parts[0].Trim().TrimStart('<').TrimEnd('>');

            if (!Uri.TryCreate(urlPart, UriKind.Absolute, out var uri))
                continue;

            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (int.TryParse(query["page"], out var page))
                return page;
        }

        return null;
    }

    private static (string Owner, string Repo) ParseOwnerAndRepo(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid GitHub URL format.", nameof(url));

        var segments = uri.Segments
          .Select(s => s.TrimEnd('/'))
          .Where(s => !string.IsNullOrEmpty(s))
          .ToArray();

        if (segments.Length < 2)
            throw new ArgumentException("URL does not contain owner and repository.", nameof(url));

        return (segments[^2], segments[^1]);
    }
}
