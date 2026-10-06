using GithubAnalyzer.Shared.Enums;

namespace GithubAnalyzer.Shared.Progress;

// Dikirim via Redis Pub/Sub
// Channel: "analysis:progress:{projectId}:{analysisType.ToLowerInvariant()}"
public class AnalysisProgressEvent
{
    public Guid JobId { get; set; }            // Correlation ID ke ProjectQueue.Id
    public Guid ProjectId { get; set; }
    public AnalysisType AnalysisType { get; set; } 


    public JobQueueStatus Status { get; set; }
    public int Progress { get; set; }               // 0–100 untuk step ini
    public string? Message { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
