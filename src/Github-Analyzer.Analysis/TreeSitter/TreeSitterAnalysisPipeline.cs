using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

namespace GithubAnalyzer.Analysis.TreeSitter;

public class TreeSitterAnalysisPipeline : ICodeAnalysisPipeline
{
    public IEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(CodebaseSnapshot snapshot, SupportedLanguage language)
    {
        ILanguageAnalyzerStrategy strategy = language switch
        {
            SupportedLanguage.CSharp => new CSharpLanguageStrategy(),
            SupportedLanguage.JavaScript => new JavaScriptLanguageStrategy(),
            SupportedLanguage.Php => new PhpLanguageStrategy(),
            SupportedLanguage.Cpp => new CppLanguageStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        var analyzer = new TreeSitterAnalyzer(strategy);
        return analyzer.Analyze(snapshot);
    }
}
