using GithubAnalyzer.WebApi.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace GithubAnalyzer.WebApi.Services;

public class ProjectCacheService(IDistributedCache cache) : IProjectCacheService
{
  private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheAbsoluteExpiration = TimeSpan.FromHours(24);

    private static readonly DistributedCacheEntryOptions CacheEntryOptions =
      new DistributedCacheEntryOptions()
        .SetSlidingExpiration(CacheSlidingExpiration)
        .SetAbsoluteExpiration(CacheAbsoluteExpiration);

    private static string GetProjectKey(Guid projectGuid)
        => $"project:{projectGuid}";

    private static string GetAnalysisKey(Guid projectGuid, string analysisType)
        => $"analysis:{projectGuid}_{analysisType.ToLowerInvariant()}";


    public async Task<string?> GetProjectJsonAsync(Guid projectGuid, CancellationToken ct = default)
      => await cache.GetStringAsync(GetProjectKey(projectGuid), ct);

    public async Task SetProjectJsonAsync(Guid projectGuid, string json, CancellationToken ct = default)
      => await cache.SetStringAsync(GetProjectKey(projectGuid), json, CacheEntryOptions, ct);

    public async Task RemoveProjectAsync(Guid projectGuid, CancellationToken ct = default)
      => await cache.RemoveAsync(GetProjectKey(projectGuid), ct);


    public async Task<string?> GetAnalysisJsonAsync(Guid projectGuid, string analysisType, CancellationToken ct = default)
      => await cache.GetStringAsync(GetAnalysisKey(projectGuid, analysisType), ct);

    public async Task SetAnalysisJsonAsync(Guid projectGuid, string analysisType, string json, CancellationToken ct = default)
      => await cache.SetStringAsync(GetAnalysisKey(projectGuid, analysisType), json, CacheEntryOptions, ct);

    public async Task RemoveAnalysisAsync(Guid projectGuid, string analysisType, CancellationToken ct = default)
      => await cache.RemoveAsync(GetAnalysisKey(projectGuid, analysisType), ct);


    public async Task RemoveAllProjectCachesAsync(Guid projectGuid, CancellationToken ct = default)
    {
        await RemoveProjectAsync(projectGuid, ct);
        await RemoveAnalysisAsync(projectGuid, "statistic", ct);
        await RemoveAnalysisAsync(projectGuid, "codegraph", ct);
    }
}
