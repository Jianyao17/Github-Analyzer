using Project = GithubAnalyzer.WebApi.Entities.Repo.Project;
using GithubAnalyzer.WebApi.Interfaces;

namespace GithubAnalyzer.WebApi.Services.Repo;

public class GithubFallbackSourceCodeProvider : ISourceCodeProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GithubFallbackSourceCodeProvider> _logger;

    public GithubFallbackSourceCodeProvider(
      HttpClient httpClient,
      ILogger<GithubFallbackSourceCodeProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool CanHandle(string repoUrl)
    {
        return !string.IsNullOrWhiteSpace(repoUrl) &&
               repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string?> GetFileContentAsync(
      Project project, string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(project.RepositoryUrl) ||
            string.IsNullOrWhiteSpace(project.LastCommitHash))
          return null;

        var parts = project.RepositoryUrl.TrimEnd('/').Split('/');
        if (parts.Length < 2) return null;

        var owner = parts[^2];
        var repo = parts[^1];

        var normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        var rawUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{project.LastCommitHash}/{normalizedPath}";

        try
        {
            var response = await _httpClient.GetAsync(rawUrl, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
              return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback failed to fetch {Path} from {Owner}/{Repo}", normalizedPath, owner, repo);
            return null;
        }
    }
}
