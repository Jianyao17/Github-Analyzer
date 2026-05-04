using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Analyzer;

namespace GithubAnalyzer.WebApi.Services;

public interface IAnalysisService
{
    IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(string repoPath, CancellationToken cancellationToken);
}
