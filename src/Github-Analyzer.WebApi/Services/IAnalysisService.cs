namespace GithubAnalyzer.WebApi.Services;

public interface IAnalysisService
{
    Task<object> AnalyzeAsync(string repoPath, CancellationToken cancellationToken);
}
