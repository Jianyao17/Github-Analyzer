using Microsoft.Extensions.Caching.Distributed;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Interfaces;

namespace GithubAnalyzer.WebApi.Services.Repo;

public class SourceCodeManager : ISourceCodeManager
{
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(2);
    private static readonly TimeSpan CacheAbsoluteExpiration = TimeSpan.FromHours(24);

    private readonly IDistributedCache _cache;
    private readonly IEnumerable<ISourceCodeProvider> _providers;
    private readonly ILogger<SourceCodeManager> _logger;

    public SourceCodeManager(
        IEnumerable<ISourceCodeProvider> providers,
        IDistributedCache cache, ILogger<SourceCodeManager> logger)
    {
        _providers = providers;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetFileContentAsync(
      Project project, string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(project.RepositoryUrl) ||
            string.IsNullOrWhiteSpace(project.LastCommitHash))
        {
            // Repository URL or commit hash is missing, cannot fetch content
            return null;
        }

        // Normalize repository URL for consistent cache keys (remove trailing slash, lowercase)
        var cleanRepoUrl = project.RepositoryUrl.TrimEnd('/').ToLowerInvariant();
        var repoKey = Uri.EscapeDataString(cleanRepoUrl);
        var safePath = Uri.EscapeDataString(relativePath);

        // Cache key format: source:{repoKey}:{commitHash}:{safePath}
        var cacheKey = $"source:{repoKey}:{project.LastCommitHash}:{safePath}";

        try
        {
            var cachedBytes = await _cache.GetAsync(cacheKey, ct);
            if (cachedBytes != null)
            {
                // Cache hit, return cached content
                return System.Text.Encoding.UTF8.GetString(cachedBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}", cacheKey);
            // Continue fetching from provider even if cache fails
        }

        var provider = _providers.FirstOrDefault(p => p.CanHandle(project.RepositoryUrl));
        if (provider == null)
        {
            _logger.LogWarning("No source code provider found that can handle repository URL: {RepoUrl}", project.RepositoryUrl);
            return null;
        }

        var content = await provider.GetFileContentAsync(project, relativePath, ct);

        if (content != null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(CacheSlidingExpiration)
                    .SetAbsoluteExpiration(CacheAbsoluteExpiration);

                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                await _cache.SetAsync(cacheKey, contentBytes, options, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key {CacheKey}", cacheKey);
            }
        }

        return content;
    }
}
