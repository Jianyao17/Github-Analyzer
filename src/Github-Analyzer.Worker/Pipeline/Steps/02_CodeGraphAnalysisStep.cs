using System.Text.Json;
using TreeSitter.CodeGraph.Domain.Graph;
using TreeSitter.CodeGraph.Domain.Reader;
using TreeSitter.CodeGraph.Domain.Languages;
using TreeSitter.CodeGraph.Domain.TreeSitter;
using TreeSitter.CodeGraph.Interfaces;
using TreeSitter.CodeGraph.Languages;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Entities;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Models;

namespace GithubAnalyzer.Worker.Pipeline.Steps;

/// <summary>
/// Tahapan analisis 2: Membangun graph AST (nodes & edges) menggunakan engine Tree-sitter.
/// Mendukung streaming realtime setiap ada update progress dari engine.
/// </summary>
public sealed class CodeGraphAnalysisStep(
    ICodeAnalyzer analyzer,
    ICodebaseReader reader,
    ILanguageDetector languageDetector,
    ILanguageRegistry languageRegistry,
    IAnalysisRepository analysisRepository) : BaseAnalysisStep
{
    public override AnalysisType AnalysisType => AnalysisType.CodeGraph;
    public override bool ShouldExecute(PipelineContext context) => context.Job.Options.CodeGraph;

    protected override Task<bool> TryCopyFromCacheAsync(PipelineContext context, CancellationToken ct) =>
        context.CacheService.TryCopyCodeGraphCacheAsync(
            context.Job.ProjectId,
            context.Job.UserId,
            context.Job.RepositoryUrl,
            context.Job.Branch,
            context.Job.CommitHash,
            context.Job.CodeGraphVersion,
            ct);

    protected override async Task ExecuteCoreAsync(
        PipelineContext context, string localPath,
        ILogger logger, CancellationToken ct)
    {
        // 1. Deteksi Bahasa Utama
        await context.ReportProgressAsync(AnalysisType, 10, "Detecting primary language...", ct);
        var detection = languageDetector.Detect(localPath, new LanguageDetectionOptions
        {
            ExcludedFolders = context.Config.ExcludedFolders
        });

        var language = detection.PrimaryLanguage ?? AnalysisLanguage.CSharp;
        logger.LogInformation("Auto-detected language {Language} (matched {Count} files) for Project {ProjectId}",
            language, detection.TotalMatchedFiles, context.Job.ProjectId);

        // 2. Baca Codebase Snapshot
        await context.ReportProgressAsync(AnalysisType, 20, $"Reading codebase for {language}...", ct);
        var snapshot = await reader.ReadAsync(localPath, new CodebaseReadOptions
        {
            AllowedExtensions = language.GetSupportedExtensions(languageRegistry),
            ExcludedFolders = context.Config.ExcludedFolders
        }, ct);

        snapshot.RepositoryName = context.Job.RepositoryName;
        if (snapshot.Files.Count == 0)
        {
            throw new InvalidOperationException($"No source code files found for language {language} in repository.");
        }

        // 3. Streaming AST Analysis secara inkremental dari Tree-sitter engine
        CodeGraph? finalGraph = null;
        await foreach (var progress in analyzer.AnalyzeAsync(snapshot, language, ct))
        {
            // Stream progress ke publisher setiap ada perubahan persentase
            await context.ReportProgressAsync(AnalysisType, progress.Percentage, progress.Message, ct);

            if (progress.Percentage == 100 && progress.Result != null)
            {
                finalGraph = progress.Result;
            }
        }

        if (finalGraph is null)
        {
            throw new InvalidOperationException("Analysis completed without producing a valid CodeGraph result.");
        }

        // 4. Native AOT serialization via WorkerJsonContext ke string JSONB
        var generatedAt = DateTime.UtcNow;
        var nodeCount = finalGraph.Nodes?.Count ?? 0;
        var edgeCount = (finalGraph.SourceRelEdges?.Count ?? 0) + (finalGraph.UseRelEdges?.Count ?? 0);
        var graphJson = JsonSerializer.Serialize(finalGraph, WorkerJsonContext.Default.CodeGraph);

        var analysis = new CodeGraphAnalysis
        {
            UserId = context.Job.UserId,
            ProjectId = context.Job.ProjectId,
            Branch = context.Job.Branch,
            CommitHash = context.Job.CommitHash,
            AnalysisVersion = context.Job.CodeGraphVersion,
            GeneratedAtUtc = generatedAt,
            NodeCount = nodeCount,
            EdgeCount = edgeCount,
        };
        await analysisRepository.SaveCodeGraphAnalysisAsync(analysis, graphJson, ct);

        // 5. Simpan ke cache
        var cache = new CodeGraphCache
        {
            RepoUrl = context.Job.RepositoryUrl,
            Branch = context.Job.Branch,
            CommitHash = context.Job.CommitHash,
            AnalysisVersion = context.Job.CodeGraphVersion,
            GeneratedAtUtc = generatedAt,
            NodeCount = nodeCount,
            EdgeCount = edgeCount,
        };
        await context.CacheService.SetCodeGraphCacheAsync(cache, graphJson, ct);
    }
}
