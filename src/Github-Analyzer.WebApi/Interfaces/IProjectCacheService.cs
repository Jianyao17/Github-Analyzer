namespace GithubAnalyzer.WebApi.Interfaces;

public interface IProjectCacheService
{
    Task<string?> GetProjectJsonAsync(Guid projectGuid, CancellationToken ct = default);

    Task SetProjectJsonAsync(Guid projectGuid, string json, CancellationToken ct = default);

    Task RemoveProjectAsync(Guid projectGuid, CancellationToken ct = default);


    Task<string?> GetAnalysisJsonAsync(Guid projectGuid, string analysisType, CancellationToken ct = default);

    Task SetAnalysisJsonAsync(Guid projectGuid, string analysisType, string json, CancellationToken ct = default);
    
    Task RemoveAnalysisAsync(Guid projectGuid, string analysisType, CancellationToken ct = default);

    /// <summary>
    /// Removes both project details and all related analyses caches.
    /// </summary>
    Task RemoveAllProjectCachesAsync(Guid projectGuid, CancellationToken ct = default);
}
