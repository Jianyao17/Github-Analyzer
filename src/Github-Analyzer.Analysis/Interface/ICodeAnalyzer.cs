using System.Collections.Generic;
using System.Threading;
using GithubAnalyzer.Analysis.Domain.Analyzer;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;

namespace GithubAnalyzer.Analysis.Interface;

public interface ICodeAnalyzer
{
    IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(CodebaseSnapshot snapshot, CancellationToken cancellationToken = default);
}