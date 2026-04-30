namespace GithubAnalyzer.WebApi.Models;

public sealed record AnalysisResult(
    string RepositoryPath,
    int FilesScanned,
    DateTime CompletedAtUtc);
