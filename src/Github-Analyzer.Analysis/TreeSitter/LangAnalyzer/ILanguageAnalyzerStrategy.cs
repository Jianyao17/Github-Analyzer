using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public interface ILanguageAnalyzerStrategy
{
    Language Language { get; }
    
    string DeclarationQuery { get; }
    string UsageQuery { get; }

    NodeType GetNodeType(string captureName);
    string? GetNamespace(Node node, string content);
}
