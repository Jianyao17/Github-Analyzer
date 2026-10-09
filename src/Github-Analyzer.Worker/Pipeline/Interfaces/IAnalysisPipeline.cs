using GithubAnalyzer.Shared.Jobs;

namespace GithubAnalyzer.Worker.Pipeline;

/// <summary>
/// Kontrak orchestrator pipeline yang menerima dan mengeksekusi analisis job secara berurutan.
/// </summary>
public interface IAnalysisPipeline
{
    /// <summary>
    /// Menjalankan pipeline analisis untuk job yang diberikan.
    /// </summary>
    Task ExecuteAsync(AnalysisJobMessage job, CancellationToken ct = default);
}
