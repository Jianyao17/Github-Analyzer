using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.TreeSitter;

namespace GithubAnalyzer.Analysis.Interface;

public interface ICodeAnalysisPipeline
{
    IEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(CodebaseSnapshot snapshot, SupportedLanguage language);
}
