namespace GithubAnalyzer.WebApi.Models;

public sealed class AnalysisJob
{
    public required string JobId { get; init; }
    public required string RepoUrl { get; init; }
    public int ProgressPercentage { get; set; }
    public string CurrentStatus { get; set; } = "Queued";
    public object? Result { get; set; }
    public DateTime CreatedAt { get; init; }
}
