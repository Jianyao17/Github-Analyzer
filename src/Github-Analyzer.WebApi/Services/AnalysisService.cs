using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Pipeline.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Services;

public sealed class AnalysisService(
    ICodeAnalyzer codeAnalyzer,
    ICodebaseReader codebaseReader,
    ILogger<AnalysisService> logger) : IAnalysisService
{
    private readonly ICodeAnalyzer _codeAnalyzer = codeAnalyzer;
    private readonly ICodebaseReader _codebaseReader = codebaseReader;
    private readonly ILogger<AnalysisService> _logger = logger;

    public async Task<object> AnalyzeAsync(string repoPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running static analysis for {RepoPath}", repoPath);

        // Read codebase snapshot (allow common source extensions)
        var options = new CodebaseReadOptions
        {
            AllowedExtensions = new[] { ".cs", ".js", ".ts", ".php", ".cpp", ".c", ".h", ".hpp" },
            ExcludedFolders = new[] { "node_modules", "vendor", ".git" }
        };

        var snapshot = await _codebaseReader.ReadAsync(repoPath, options, cancellationToken);

        // Heuristic: pick dominant language from files
        var extCounts = snapshot.Files
            .GroupBy(f => (f.Extension ?? string.Empty).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        AnalysisLanguage language = AnalysisLanguage.CSharp;
        extCounts.TryGetValue(".js", out var jsCount);
        extCounts.TryGetValue(".ts", out var tsCount);
        var totalJsTs = (jsCount + tsCount);
        if (totalJsTs > extCounts.GetValueOrDefault(".cs", 0))
        {
            language = AnalysisLanguage.JavaScript;
        }
        else if (extCounts.TryGetValue(".php", out var php) && php > extCounts.GetValueOrDefault(".cs", 0))
        {
            language = AnalysisLanguage.Php;
        }
        else if (extCounts.Keys.Any(k => k == ".cpp" || k == ".c" || k == ".h" || k == ".hpp")
                 && extCounts.GetValueOrDefault(".cs", 0) == 0)
        {
            language = AnalysisLanguage.Cpp;
        }

        // Run analyzer (streaming). Take final result when IsCompleted
        CodeGraph finalGraph = new();
        await foreach (var progress in _codeAnalyzer.AnalyzeAsync(snapshot, language, cancellationToken))
        {
            if (progress.IsCompleted && progress.Result is not null)
            {
                finalGraph = progress.Result;
            }
        }

        // Flatten nodes and edges into frontend-friendly shapes
        var nodes = finalGraph.Nodes.Select(n => new
        {
            id = n.PathId,
            label = n.Label,
            type = n.Type.ToString()
        }).ToList();

        var edges = finalGraph.SourceRelEdges.Concat(finalGraph.UseRelEdges)
            .Select(e => new
            {
                source = e.From,
                target = e.To,
                type = e.Type.ToString()
            })
            .ToList();

        _logger.LogInformation("Analysis complete. Nodes: {NodeCount}, Edges: {EdgeCount}", nodes.Count, edges.Count);

        return new { nodes, edges };
    }
}
