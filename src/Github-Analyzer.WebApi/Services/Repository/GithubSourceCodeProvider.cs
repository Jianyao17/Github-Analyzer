using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GithubAnalyzer.WebApi.Services.Repo;

public class GithubSourceCodeProvider : ISourceCodeProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GithubSourceCodeProvider> _logger;

    public GithubSourceCodeProvider(HttpClient httpClient, IMemoryCache cache, ILogger<GithubSourceCodeProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetFileContentAsync(Project project, string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(project.RepositoryUrl) || string.IsNullOrWhiteSpace(project.LastCommitHash))
        {
            return null;
        }

        // Parse owner and repo from URL (e.g. https://github.com/owner/repo)
        var parts = project.RepositoryUrl.TrimEnd('/').Split('/');
        if (parts.Length < 2) return null;
        
        var owner = parts[^2];
        var repo = parts[^1];

        // Replace backslashes with forward slashes for raw github content URL
        var normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        var rawUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{project.LastCommitHash}/{normalizedPath}";

        // Cache key based on ProjectId and path
        var cacheKey = $"source_{project.Id}_{normalizedPath}";

        if (_cache.TryGetValue(cacheKey, out string? cachedContent))
        {
            return cachedContent;
        }

        try
        {
            var response = await _httpClient.GetAsync(rawUrl, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(ct);
            
            // Cache configuration: keep for 1 hour sliding, 24 hours absolute
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            _cache.Set(cacheKey, content, cacheEntryOptions);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch source code from {RawUrl}", rawUrl);
            return null;
        }
    }
}
