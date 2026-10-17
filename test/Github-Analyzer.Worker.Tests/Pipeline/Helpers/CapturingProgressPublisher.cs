using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Progress;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Services;

namespace GithubAnalyzer.Worker.Tests.Pipeline.Helpers;

/// <summary>
/// In-memory implementation of IAnalysisProgressPublisher yang merekam semua event yang dipublikasikan.
/// Digunakan untuk verifikasi progress reporting tanpa Redis.
/// </summary>
internal sealed class CapturingProgressPublisher : IAnalysisProgressPublisher
{
    private readonly List<AnalysisProgressEvent> _events = [];

    public IReadOnlyList<AnalysisProgressEvent> Events => _events;

    public ValueTask PublishAsync(AnalysisProgressEvent progressEvent, CancellationToken ct = default)
    {
        _events.Add(progressEvent);
        return ValueTask.CompletedTask;
    }

    public bool HasEventWithProgress(AnalysisType type, int progress) =>
        _events.Any(e => e.AnalysisType == type && e.Progress == progress);

    public IEnumerable<AnalysisProgressEvent> ForType(AnalysisType type) =>
        _events.Where(e => e.AnalysisType == type);
}
