using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.Analysis.Application;
using GithubAnalyzer.Analysis.Domain;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Services;

public sealed class AnalysisService(
    CodeAnalysisService codeAnalysisService,
    ILogger<AnalysisService> logger) : IAnalysisService
{
    private readonly CodeAnalysisService _codeAnalysisService = codeAnalysisService;
    private readonly ILogger<AnalysisService> _logger = logger;

    public async Task<object> AnalyzeAsync(string repoPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running static analysis for {RepoPath}", repoPath);

        // Simple aggregation of all files in the repository
        var files = Directory.GetFiles(repoPath, "*.cs", SearchOption.AllDirectories);
        _logger.LogInformation("Found {Count} .cs files to analyze.", files.Length);
        
        var combinedGraph = new CodeGraph();

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var code = await File.ReadAllTextAsync(file, cancellationToken);
            var relativePath = Path.GetRelativePath(repoPath, file);
            
            _logger.LogDebug("Analyzing file: {FilePath} ({Size} bytes)", relativePath, code.Length);
            
            var json = _codeAnalysisService.Analyze(code, relativePath);
            var fileGraph = JsonSerializer.Deserialize<CodeGraph>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (fileGraph != null)
            {
                _logger.LogDebug("File {FilePath} produced {NodeCount} nodes and {EdgeCount} edges.", 
                    relativePath, fileGraph.Nodes.Count, fileGraph.Edges.Count);
                combinedGraph.Nodes.AddRange(fileGraph.Nodes);
                combinedGraph.Edges.AddRange(fileGraph.Edges);
            }
        }

        _logger.LogInformation("Analysis complete. Total nodes: {NodeCount}, Total edges: {EdgeCount}", 
            combinedGraph.Nodes.Count, combinedGraph.Edges.Count);
        return combinedGraph;
    }
}
