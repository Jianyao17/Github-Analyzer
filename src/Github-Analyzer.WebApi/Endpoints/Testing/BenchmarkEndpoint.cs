using System.Diagnostics;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Languages;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Interfaces;
using GithubAnalyzer.Analysis.Languages;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Models.Analysis;
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Endpoints.Testing;

public sealed record BenchmarkResponse(
    string RepositoryUrl,
    string RepositoryName,
    string? BranchName,
    string? CommitHash,
    string Language,
    int FileCount,
    long TotalDurationMs,
    long TotalCpuTimeMs,
    BenchmarkGcTotals GcTotals,
    BenchmarkGraphSummary Graph,
    IReadOnlyList<StageMetrics> Stages);

public sealed record BenchmarkGcTotals(
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);

public sealed record BenchmarkGraphSummary(
    int NodeCount,
    int EdgeCount);

public sealed record StageMetrics(
    string Stage,
    long DurationMs,
    long CpuTimeMs,
    long ManagedBytesDelta,
    long WorkingSetBytesDelta,
    long PeakWorkingSetBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    IReadOnlyDictionary<string, long>? ExtraTimingsMs);

public static class BenchmarkEndpoint
{
    private const string RepoUrl = "https://github.com/Jianyao17/Web-Bioskop";
    private const string Branch = "main";
    private const string? CommitHash = null;

    public static RouteHandlerBuilder MapBenchmarkEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/benchmark", async (
            IRepositoryFetcher repositoryFetcher,
            ICodebaseReader reader,
            ICodeAnalyzer analyzer,
            ILanguageDetector languageDetector,
            ILanguageRegistry languageRegistry,
            AnalysisConfig analysisConfig,
            CancellationToken ct) =>
        {
            var stages = new List<StageMetrics>();
            var overallStart = CaptureRuntime();
            var overallStopwatch = Stopwatch.StartNew();

            RepositoryResult repoResult;
            try
            {
                var downloadMetrics = await MeasureStageAsync(
                    "download_extract",
                    async () => await repositoryFetcher.DownloadAndExtractAsync(
                        RepoUrl, Branch, CommitHash, ct));

                repoResult = downloadMetrics.Result;
                stages.Add(downloadMetrics.Metrics);
            }
            catch (NotSupportedException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError(ex.Message);
            }

            var extractPath = repoResult.ExtractPath;
            try
            {
                CodebaseSnapshot snapshot;
                var excluded = analysisConfig.ExcludedFolders ?? Array.Empty<string>();
                var detection = languageDetector.Detect(repoResult.ExtractPath, new LanguageDetectionOptions
                {
                    ExcludedFolders = excluded
                });
                var language = detection.PrimaryLanguage ?? AnalysisLanguage.CSharp;
                var options = new CodebaseReadOptions
                {
                    AllowedExtensions = language.GetSupportedExtensions(languageRegistry),
                    ExcludedFolders = excluded
                };

                var readMetrics = await MeasureStageAsync(
                    "read_codebase",
                    async () => await reader.ReadAsync(repoResult.ExtractPath, options, ct));

                snapshot = readMetrics.Result;
                stages.Add(readMetrics.Metrics);

                if (snapshot.Files.Count == 0)
                {
                    return ApiResults.InternalServerError(
                        "No source files found for the selected language.");
                }

                CodeGraph? finalGraph = null;
                long pass1Ms = 0;
                long pass2Ms = 0;

                var analyzeMetrics = await MeasureStageAsync(
                    "analyze",
                    async () =>
                    {
                        var analyzeStopwatch = Stopwatch.StartNew();
                        await foreach (var progress in analyzer.AnalyzeAsync(snapshot, language, ct))
                        {
                            if (pass1Ms == 0 && progress.Percentage >= 60)
                            {
                                pass1Ms = analyzeStopwatch.ElapsedMilliseconds;
                            }

                            if (progress.IsCompleted && progress.Result != null)
                            {
                                finalGraph = progress.Result;
                            }
                        }

                        analyzeStopwatch.Stop();
                        if (pass1Ms == 0)
                        {
                            pass1Ms = analyzeStopwatch.ElapsedMilliseconds;
                        }

                        pass2Ms = Math.Max(0, analyzeStopwatch.ElapsedMilliseconds - pass1Ms);
                    });

                stages.Add(analyzeMetrics with
                {
                    ExtraTimingsMs = new Dictionary<string, long>
                    {
                        ["pass1_ms"] = pass1Ms,
                        ["pass2_ms"] = pass2Ms
                    }
                });

                overallStopwatch.Stop();
                var overallEnd = CaptureRuntime();

                if (finalGraph == null)
                {
                    return ApiResults.InternalServerError(
                        "Analysis completed without a CodeGraph result.");
                }

                var response = new BenchmarkResponse(
                    RepositoryUrl: repoResult.RepositoryUrl,
                    RepositoryName: repoResult.RepositoryName,
                    BranchName: repoResult.BranchName ?? Branch,
                    CommitHash: repoResult.LastCommitHash ?? CommitHash,
                    Language: language.ToString(),
                    FileCount: snapshot.Files.Count,
                    TotalDurationMs: overallStopwatch.ElapsedMilliseconds,
                    TotalCpuTimeMs: (long)(overallEnd.CpuTime - overallStart.CpuTime).TotalMilliseconds,
                    GcTotals: new BenchmarkGcTotals(
                        overallEnd.Gen0Collections - overallStart.Gen0Collections,
                        overallEnd.Gen1Collections - overallStart.Gen1Collections,
                        overallEnd.Gen2Collections - overallStart.Gen2Collections),
                    Graph: new BenchmarkGraphSummary(
                        NodeCount: finalGraph.Nodes?.Count ?? 0,
                        EdgeCount: (finalGraph.SourceRelEdges?.Count ?? 0) + (finalGraph.UseRelEdges?.Count ?? 0)),
                    Stages: stages);

                return ApiResults.Ok(response);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError(ex.Message);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(extractPath))
                {
                    TryCleanup(extractPath);
                }
            }
        });
    }

    private static async Task<(T Result, StageMetrics Metrics)> MeasureStageAsync<T>(
        string stage,
        Func<Task<T>> action)
    {
        var start = CaptureRuntime();
        var stopwatch = Stopwatch.StartNew();

        var result = await action();

        stopwatch.Stop();
        var end = CaptureRuntime();

        var metrics = new StageMetrics(
            Stage: stage,
            DurationMs: stopwatch.ElapsedMilliseconds,
            CpuTimeMs: (long)(end.CpuTime - start.CpuTime).TotalMilliseconds,
            ManagedBytesDelta: end.ManagedBytes - start.ManagedBytes,
            WorkingSetBytesDelta: end.WorkingSetBytes - start.WorkingSetBytes,
            PeakWorkingSetBytes: end.PeakWorkingSetBytes,
            Gen0Collections: end.Gen0Collections - start.Gen0Collections,
            Gen1Collections: end.Gen1Collections - start.Gen1Collections,
            Gen2Collections: end.Gen2Collections - start.Gen2Collections,
            ExtraTimingsMs: null);

        return (result, metrics);
    }

    private static async Task<StageMetrics> MeasureStageAsync(
        string stage,
        Func<Task> action)
    {
        var start = CaptureRuntime();
        var stopwatch = Stopwatch.StartNew();

        await action();

        stopwatch.Stop();
        var end = CaptureRuntime();

        return new StageMetrics(
            Stage: stage,
            DurationMs: stopwatch.ElapsedMilliseconds,
            CpuTimeMs: (long)(end.CpuTime - start.CpuTime).TotalMilliseconds,
            ManagedBytesDelta: end.ManagedBytes - start.ManagedBytes,
            WorkingSetBytesDelta: end.WorkingSetBytes - start.WorkingSetBytes,
            PeakWorkingSetBytes: end.PeakWorkingSetBytes,
            Gen0Collections: end.Gen0Collections - start.Gen0Collections,
            Gen1Collections: end.Gen1Collections - start.Gen1Collections,
            Gen2Collections: end.Gen2Collections - start.Gen2Collections,
            ExtraTimingsMs: null);
    }

    private static RuntimeSnapshot CaptureRuntime()
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();

        return new RuntimeSnapshot(
            ManagedBytes: GC.GetTotalMemory(false),
            WorkingSetBytes: process.WorkingSet64,
            PeakWorkingSetBytes: process.PeakWorkingSet64,
            CpuTime: process.TotalProcessorTime,
            Gen0Collections: GC.CollectionCount(0),
            Gen1Collections: GC.CollectionCount(1),
            Gen2Collections: GC.CollectionCount(2));
    }

    private static void TryCleanup(string extractPath)
    {
        try
        {
            var repoRoot = Directory.GetParent(extractPath)?.FullName;
            if (!string.IsNullOrWhiteSpace(repoRoot) && Directory.Exists(repoRoot))
            {
                Directory.Delete(repoRoot, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Log and ignore cleanup failures - not critical and we don't want to throw from a finally block
            Console.Error.WriteLine($"Failed to clean up extracted repository at '{extractPath}': {ex.Message}");
        }
    }



    private readonly record struct RuntimeSnapshot(
        long ManagedBytes,
        long WorkingSetBytes,
        long PeakWorkingSetBytes,
        TimeSpan CpuTime,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections);
}
