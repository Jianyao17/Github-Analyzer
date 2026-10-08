using Microsoft.Extensions.Caching.Distributed;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.Shared.Git;

namespace GithubAnalyzer.WebApi.Services;

/// <summary>
/// Mengelola pengambilan konten source code dengan membungkus IGitService dan caching Redis (IDistributedCache).
/// </summary>
public class SourceCodeManager : ISourceCodeManager
{
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(2);
    private static readonly TimeSpan CacheAbsoluteExpiration = TimeSpan.FromHours(24);

    private readonly IGitService _gitService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SourceCodeManager> _logger;

    public SourceCodeManager(
        IGitService gitService,
        IDistributedCache cache,
        ILogger<SourceCodeManager> logger)
    {
        _gitService = gitService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetFileContentAsync(
        Project project, string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(project.RepositoryUrl) ||
            string.IsNullOrWhiteSpace(project.LastCommitHash))
        {
            return null;
        }

        // Normalize repository URL for consistent cache keys
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
                return System.Text.Encoding.UTF8.GetString(cachedBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}", cacheKey);
        }

        var content = await _gitService.GetFileContentAsync(
            project.RepositoryUrl, project.LastCommitHash,
            relativePath, ct);

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
