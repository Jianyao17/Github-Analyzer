using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Interfaces;

public interface IAnalysisCacheService
{
    /// <summary>
    /// Attempts to find an existing cache for the given parameters and copy it to the user's project analysis table.
    /// Returns true if a cache hit occurred and the data was copied.
    /// </summary>
    Task<bool> TryCopyCacheToProjectAsync(
        AnalysisType type, 
        Guid projectId, 
        Guid userId, 
        string repoUrl, 
        string? branch, 
        string? commitHash, 
        string analysisVersion, 
        CancellationToken ct);

    /// <summary>
    /// Saves the new cache data.
    /// </summary>
    Task SetCacheAsync<T>(T cacheData, CancellationToken ct) where T : class;

    /// <summary>
    /// Deletes cache records older than the specified max age.
    /// </summary>
    Task InvalidateOldCachesAsync(TimeSpan maxAge, CancellationToken ct);
}
