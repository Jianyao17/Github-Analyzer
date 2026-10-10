using GithubAnalyzer.Shared.Jobs;

namespace GithubAnalyzer.WebApi.Interfaces;

/// <summary>
/// Kontrak untuk mendistribusikan analysis job ke Redis Streams antrean worker.
/// </summary>
public interface IAnalysisJobDispatcher
{
    /// <summary>
    /// Mengirimkan pesan job ke stream Redis.
    /// </summary>
    Task<string> DispatchAsync(AnalysisJobMessage job, CancellationToken ct = default);
}
