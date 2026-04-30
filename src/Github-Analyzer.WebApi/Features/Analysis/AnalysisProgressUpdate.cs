namespace GithubAnalyzer.WebApi.Features.Analysis;

public sealed record AnalysisProgressUpdate(
    int ProgressPercentage,
    string CurrentStatus);
