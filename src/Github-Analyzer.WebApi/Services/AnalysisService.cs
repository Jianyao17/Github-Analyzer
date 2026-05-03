using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.TreeSitter;
using System.Runtime.CompilerServices;

namespace GithubAnalyzer.WebApi.Services;

public sealed class AnalysisService(
    ICodebaseReader codebaseReader,
    ICodeAnalysisPipeline analysisPipeline,
    ILogger<AnalysisService> logger) : IAnalysisService
{
    public async IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(
        string repoPath, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.LogInformation("Running static analysis for {RepoPath}", repoPath);

        // 1. Read codebase
        var options = new CodebaseReadOptions
        {
            AllowedExtensions = new[] { ".cs", ".js", ".php", ".cpp", ".hpp", ".h", ".c" },
            ExcludedFolders = new[] { "bin", "obj", "node_modules", ".git", "vendor" }
        };

        var snapshot = await codebaseReader.ReadAsync(repoPath, options, cancellationToken);
        logger.LogInformation("Read {FileCount} files for analysis.", snapshot.Files.Count);

        if (snapshot.Files.Count == 0)
        {
            yield break;
        }

        // 2. Determine primary language (simplified: based on most frequent extension)
        var language = DetermineLanguage(snapshot);
        logger.LogInformation("Determined primary language: {Language}", language);

        // 3. Run analysis pipeline
        foreach (var progress in analysisPipeline.AnalyzeAsync(snapshot, language))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            yield return progress;
        }
    }

    private SupportedLanguage DetermineLanguage(CodebaseSnapshot snapshot)
    {
        var extCount = snapshot.Files
            .GroupBy(f => f.Extension.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return extCount switch
        {
            ".js" => SupportedLanguage.JavaScript,
            ".php" => SupportedLanguage.Php,
            ".cpp" or ".hpp" or ".h" or ".c" => SupportedLanguage.Cpp,
            _ => SupportedLanguage.CSharp
        };
    }
}
