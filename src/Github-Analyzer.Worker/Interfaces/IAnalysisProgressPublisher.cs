using GithubAnalyzer.Shared.Progress;

namespace GithubAnalyzer.Worker.Interfaces;

/// <summary>
/// Kontrak publisher untuk streaming progress analisis secara real-time.
/// </summary>
public interface IAnalysisProgressPublisher
{
    /// <summary>
    /// Mempublikasikan progress analisis ke consumer/subscriber.
    /// </summary>
    ValueTask PublishAsync(AnalysisProgressEvent progressEvent, CancellationToken ct = default);
}
