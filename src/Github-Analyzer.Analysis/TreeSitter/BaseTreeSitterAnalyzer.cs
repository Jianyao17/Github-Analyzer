using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

namespace GithubAnalyzer.Analysis.TreeSitter;

public abstract class BaseTreeSitterAnalyzer
{
    protected readonly ILanguageAnalyzerStrategy Strategy;
    protected readonly Parser Parser;

    protected BaseTreeSitterAnalyzer(ILanguageAnalyzerStrategy strategy)
    {
        Strategy = strategy;
        Parser = new Parser();
        if (strategy.Language != null)
        {
            Parser.Language = strategy.Language;
        }
    }

    public IEnumerable<TreeSitterProgress<CodeGraph>> Analyze(CodebaseSnapshot snapshot)
    {
        var graph = new CodeGraph();
        var index = new SymbolIndex();
        
        // Pass 1: Declaration Mapping
        var files = snapshot.Files;
        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            ProcessDeclarations(file, graph, index);
            
            double progress = (i + 1) * 45.0 / files.Count;
            yield return TreeSitterProgress<CodeGraph>.Report(progress, $"Mapping: {file.RelativePath}");
        }

        // Finalize Hierarchy (SourceRelEdges)
        yield return TreeSitterProgress<CodeGraph>.Report(45, "Building hierarchy...");
        BuildHierarchy(graph, index);

        // Pass 2: Usage Scanning
        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            ProcessUsages(file, graph, index);
            
            double progress = 50.0 + (i + 1) * 50.0 / files.Count;
            yield return TreeSitterProgress<CodeGraph>.Report(progress, $"Scanning usages: {file.RelativePath}");
        }

        yield return TreeSitterProgress<CodeGraph>.Complete(graph);
    }

    protected abstract void ProcessDeclarations(CodebaseFileContent file, CodeGraph graph, SymbolIndex index);
    protected abstract void ProcessUsages(CodebaseFileContent file, CodeGraph graph, SymbolIndex index);
    protected abstract void BuildHierarchy(CodeGraph graph, SymbolIndex index);
}
