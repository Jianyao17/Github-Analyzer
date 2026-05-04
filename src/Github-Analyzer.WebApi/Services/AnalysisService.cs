using System.Runtime.CompilerServices;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.Analysis.TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Analyzer;
using GithubAnalyzer.Analysis.Domain.Reader;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Services;

public sealed class AnalysisService(ILogger<AnalysisService> logger) : IAnalysisService
{
    private readonly ILogger<AnalysisService> _logger = logger;

    public async IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(string repoPath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running static analysis for {RepoPath}", repoPath);

        // Map extensions to ProgrammingLanguage
        var languageExtensions = new Dictionary<string, ProgrammingLanguage>
        {
            { ".cs", ProgrammingLanguage.CSharp },
            { ".php", ProgrammingLanguage.Php },
            { ".js", ProgrammingLanguage.JavaScript },
            { ".cpp", ProgrammingLanguage.Cpp },
            { ".h", ProgrammingLanguage.Cpp },
            { ".hpp", ProgrammingLanguage.Cpp }
        };

        var allFiles = Directory.GetFiles(repoPath, "*.*", SearchOption.AllDirectories);
        var filesByLanguage = new Dictionary<ProgrammingLanguage, List<CodebaseFileContent>>();

        foreach (var file in allFiles)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (languageExtensions.TryGetValue(ext, out var lang))
            {
                if (!filesByLanguage.ContainsKey(lang))
                {
                    filesByLanguage[lang] = new List<CodebaseFileContent>();
                }
                
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                filesByLanguage[lang].Add(new CodebaseFileContent
                {
                    AbsolutePath = file,
                    RelativePath = Path.GetRelativePath(repoPath, file).Replace('\\', '/'),
                    Extension = ext,
                    SizeBytes = new FileInfo(file).Length,
                    Content = content
                });
            }
        }

        var combinedGraph = new CodeGraph();
        int totalLangs = filesByLanguage.Count;
        int currentLangIdx = 0;

        if (totalLangs == 0)
        {
            yield return TreeSitterProgress<CodeGraph>.Completed(combinedGraph, "No supported files found.");
            yield break;
        }

        foreach (var (lang, files) in filesByLanguage)
        {
            var analyzer = LanguageAnalyzerFactory.CreateAnalyzer(lang);
            var snapshot = new CodebaseSnapshot
            {
                RootPath = repoPath,
                Files = files
            };

            await foreach (var progress in analyzer.AnalyzeAsync(snapshot, cancellationToken))
            {
                // scale progress relative to multiple languages
                double baseProgress = (double)currentLangIdx / totalLangs * 100;
                double scaledProgress = baseProgress + (progress.Percentage / totalLangs);
                
                if (progress.Percentage == 100 && progress.Result != null)
                {
                    combinedGraph.Nodes.AddRange(progress.Result.Nodes);
                    combinedGraph.SourceRelEdges.AddRange(progress.Result.SourceRelEdges);
                    combinedGraph.UseRelEdges.AddRange(progress.Result.UseRelEdges);
                }
                else
                {
                    yield return TreeSitterProgress<CodeGraph>.InProgress(scaledProgress, $"[{lang}] {progress.Message}");
                }
            }
            
            currentLangIdx++;
        }

        _logger.LogInformation("Analysis complete. Total nodes: {NodeCount}, Source edges: {SourceEdgeCount}, Use edges: {UseEdgeCount}", 
            combinedGraph.Nodes.Count, combinedGraph.SourceRelEdges.Count, combinedGraph.UseRelEdges.Count);
            
        yield return TreeSitterProgress<CodeGraph>.Completed(combinedGraph);
    }
}
