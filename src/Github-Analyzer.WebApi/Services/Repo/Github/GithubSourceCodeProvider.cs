using static System.Text.Encoding;
using Project = GithubAnalyzer.WebApi.Entities.Repo.Project;
using GithubAnalyzer.WebApi.Interfaces;
using Octokit;

namespace GithubAnalyzer.WebApi.Services.Repo;

public class GithubSourceCodeProvider : ISourceCodeProvider
{
    private readonly IGitHubClient _githubClient;
    private readonly ILogger<GithubSourceCodeProvider> _logger;
    private readonly GithubFallbackSourceCodeProvider _fallbackProvider;

    public GithubSourceCodeProvider(
        IGitHubClient githubClient,
        GithubFallbackSourceCodeProvider fallbackProvider,
        ILogger<GithubSourceCodeProvider> logger)
    {
        _logger = logger;
        _githubClient = githubClient;
        _fallbackProvider = fallbackProvider;
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

        try
        {
            var rawContentBytes = await _githubClient.Repository.Content
              .GetRawContentByRef(owner, repo, normalizedPath, project.LastCommitHash);

            return UTF8.GetString(rawContentBytes);
        }
        catch (NotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
              ex, "Octokit failed fetching source code for {Path} from {Owner}/{Repo}, using fallback...",
              normalizedPath, owner, repo);

            return await _fallbackProvider.GetFileContentAsync(project, relativePath, ct);
        }
    }
}
