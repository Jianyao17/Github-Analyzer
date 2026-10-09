using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database;
using Microsoft.Extensions.Logging;

namespace GithubAnalyzer.Worker.Services;

/// <summary>
/// Implementasi IDbAnalysisCacheService untuk pengecekan, penyalinan level database, dan penyimpanan cache analisis.
/// </summary>
public sealed class DbAnalysisCacheService(
  IAnalysisRepository repository, ILogger<DbAnalysisCacheService> logger) : IDbAnalysisCacheService
{
    /// <inheritdoc />
    public async Task<bool> TryCopyStatisticCacheAsync(
        Guid projectId, Guid userId,
        string repoUrl, string? branch,
        string? commitHash, string version,
        CancellationToken ct = default)
    {
        var lookupKey = CacheLookupKey.Generate(repoUrl, branch, commitHash, version);
        var hit = await repository.TryCopyStatisticCacheAsync(projectId, userId, lookupKey, ct);

        if (hit)
        {
            logger.LogInformation(
                "Cache HIT: Statistic analysis copied to project {ProjectId} (LookupKey={LookupKey}, Version={Version})",
                projectId, lookupKey, version);
        }
        else
        {
            logger.LogDebug(
                "Cache MISS: Statistic analysis for project {ProjectId} (LookupKey={LookupKey}, Version={Version})",
                projectId, lookupKey, version);
        }

        return hit;
    }

    /// <inheritdoc />
    public async Task<bool> TryCopyCodeGraphCacheAsync(
        Guid projectId, Guid userId,
        string repoUrl, string? branch,
        string? commitHash, string version,
        CancellationToken ct = default)
    {
        var lookupKey = CacheLookupKey.Generate(repoUrl, branch, commitHash, version);
        var hit = await repository.TryCopyCodeGraphCacheAsync(projectId, userId, lookupKey, ct);

        if (hit)
        {
            logger.LogInformation(
                "Cache HIT: CodeGraph analysis copied to project {ProjectId} (LookupKey={LookupKey}, Version={Version})",
                projectId, lookupKey, version);
        }
        else
        {
            logger.LogDebug(
                "Cache MISS: CodeGraph analysis for project {ProjectId} (LookupKey={LookupKey}, Version={Version})",
                projectId, lookupKey, version);
        }

        return hit;
    }

    /// <inheritdoc />
    public async Task SetStatisticCacheAsync(StatisticCache cache, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(cache.LookupKey))
        {
            cache.LookupKey = CacheLookupKey.Generate(
                cache.RepoUrl,
                cache.Branch,
                cache.CommitHash,
                cache.AnalysisVersion);
        }

        await repository.SaveStatisticCacheAsync(cache, ct);
        logger.LogInformation("Statistic cache saved for {RepoUrl} (LookupKey={LookupKey})", cache.RepoUrl, cache.LookupKey);
    }

    /// <inheritdoc />
    public async Task SetCodeGraphCacheAsync(CodeGraphCache cache, string graphJson, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(cache.LookupKey))
        {
            cache.LookupKey = CacheLookupKey.Generate(
                cache.RepoUrl,
                cache.Branch,
                cache.CommitHash,
                cache.AnalysisVersion);
        }

        await repository.SaveCodeGraphCacheAsync(cache, graphJson, ct);
        logger.LogInformation("CodeGraph cache saved for {RepoUrl} (LookupKey={LookupKey})", cache.RepoUrl, cache.LookupKey);
    }

    /// <inheritdoc />
    public async Task InvalidateOldCachesAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        await repository.InvalidateOldCachesAsync(maxAge, ct);
        logger.LogInformation("Old analysis caches invalidated with max age {MaxAge}", maxAge);
    }
}
