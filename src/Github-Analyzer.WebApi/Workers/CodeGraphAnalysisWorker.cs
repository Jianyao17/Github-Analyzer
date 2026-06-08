using System.Text.Json;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Entities.Cache;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Services.Repo;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Config;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Workers;

public class CodeGraphAnalysisWorker : BaseQueueWorker
{
    public override string JobType => nameof(AnalysisType.CodeGraph);

    public CodeGraphAnalysisWorker(
        IServiceScopeFactory scopeFactory,
        IQueueProgressNotifier progressNotifier,
        ILogger<CodeGraphAnalysisWorker> logger) 
        : base(scopeFactory, progressNotifier, logger)
    {
    }

    protected override async Task ProcessJobAsync(ProjectQueue job, CancellationToken cancellationToken)
    {
        using var scope     = _scopeFactory.CreateScope();
        var dbContext       = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (job.Project == null) 
        {
            // Check if project is null
            throw new InvalidOperationException("Project data is missing from the queue job.");
        }

        var analysisConfig  = scope.ServiceProvider.GetRequiredService<AnalysisConfig>();
        var cacheService    = scope.ServiceProvider.GetRequiredService<IAnalysisCacheService>();

        var repoUrl    = job.Project.RepositoryUrl;
        var branch     = job.Project.BranchName;
        var commitHash = job.Project.LastCommitHash;
        var version    = analysisConfig.CodeGraphVersion;

        // ─────────────────────────────────────────────────────────────────────
        // Cache check — copy at DB level if a previous analysis exists
        // ─────────────────────────────────────────────────────────────────────
        var cacheHit = await cacheService.TryCopyCacheToProjectAsync(
            AnalysisType.CodeGraph, job.ProjectId, job.Project.UserId, 
            repoUrl, branch, commitHash, version, cancellationToken);

        if (cacheHit)
        {
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cache miss — run full analysis pipeline
        // ─────────────────────────────────────────────────────────────────────
        var analyzer        = scope.ServiceProvider.GetRequiredService<ICodeAnalyzer>();
        var reader          = scope.ServiceProvider.GetRequiredService<ICodebaseReader>();
        var repoFetcher     = scope.ServiceProvider.GetRequiredService<IRepositoryFetcher>();
        var downloadGate    = scope.ServiceProvider.GetRequiredService<RepoDownloadGate>();

        var localPath = job.Project.LocalPath;
        if (!Directory.Exists(localPath)) 
        {
            // Re-download repository if local file is missing
            // EnsureRepoAsync will coordinate concurrent download attempts for the same project
            localPath = await downloadGate.EnsureRepoAsync(
                job.ProjectId,
                async token =>
                {
                    var repoResult = await repoFetcher.DownloadAndExtractAsync(
                        job.Project.RepositoryUrl, job.Project.BranchName ?? "main",
                        job.Project.LastCommitHash, token);
                    return repoResult.ExtractPath;
                },
                cancellationToken);

            if (!Directory.Exists(localPath))
                throw new DirectoryNotFoundException($"Repository path not found after re-download: {localPath}");
        }

        // 1. Tentukan bahasa secara otomatis
        var excluded = analysisConfig.ExcludedFolders ?? Array.Empty<string>();
        var language = DetermineLanguage(localPath, excluded);
        _logger.LogInformation("Auto-detected language {Language} for project {ProjectId}", language, job.ProjectId);

        // 2. Baca Codebase Snapshot
        var options = new CodebaseReadOptions
        {
            AllowedExtensions = GetExtensionsForLanguage(language),
            ExcludedFolders = excluded
        };

        var snapshot = await reader.ReadAsync(localPath, options, cancellationToken);
        if (snapshot.Files.Count == 0)
        {
            // Check if snapshot files count is zero
            throw new Exception($"No source code files found for language {language} in the repository.");
        }

        // 3. Eksekusi analisis secara inkremental dan stream ke Event Bus
        CodeGraph? finalGraph = null;
        
        await foreach (var progress in analyzer.AnalyzeAsync(snapshot, language, cancellationToken))
        {
            var progressEvent = ToEvent(progress, job.ProjectId, job.Id, JobType);
            await _progressNotifier.NotifyAsync(progressEvent);

            if (progress.Percentage == 100 && 
                progress.Result != null)
            {
                // Get final graph result
                finalGraph = progress.Result;
            }
        }

        if (finalGraph == null)
        {
            // Check if final graph is null
            throw new Exception("Analysis completed but no CodeGraph result was produced.");
        }

        // 4. Serialize ke JsonDocument untuk JSONB storage
        var graphDocument = JsonSerializer.SerializeToDocument(finalGraph);
        var nodeCount = finalGraph.Nodes?.Count ?? 0;
        var edgeCount = finalGraph.SourceRelEdges?.Count + 
                        finalGraph.UseRelEdges?.Count ?? 0;
        var generatedAt = DateTime.UtcNow;

        // 5. Simpan analysis per-user/project dulu (pasti unik, Id = Guid baru)
        var analysis = new CodeGraphAnalysis
        {
            UserId    = job.Project.UserId,
            ProjectId = job.ProjectId,

            Branch         = branch,
            CommitHash     = commitHash,
            GeneratedAtUtc = generatedAt,

            GraphJson = graphDocument,
            NodeCount = nodeCount,
            EdgeCount = edgeCount,
            
            AnalysisVersion = version
        };

        dbContext.CodeGraphAnalyses.Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 6. Simpan ke cache — delegasikan ke service
        var cache = new CodeGraphCache
        {
            LookupKey      = CacheLookupKey.Generate(repoUrl, branch, commitHash, version),
            RepoUrl        = repoUrl,
            Branch         = branch,
            CommitHash     = commitHash,
            GeneratedAtUtc = generatedAt,
            GraphJson      = graphDocument,
            NodeCount      = nodeCount,
            EdgeCount      = edgeCount,
            AnalysisVersion = version
        };

        await cacheService.SetCacheAsync(cache, cancellationToken);
        
        _logger.LogInformation(
            "CodeGraphAnalysis saved (+ cached) for project {ProjectId}", job.ProjectId);
    }

    private static AnalysisLanguage DetermineLanguage(
        string localPath, string[] excludedFolders)
    {
        var extCount = new Dictionary<AnalysisLanguage, int>
        {
            { AnalysisLanguage.CSharp, 0 },
            { AnalysisLanguage.JavaScript, 0 },
            { AnalysisLanguage.Php, 0 },
            { AnalysisLanguage.Cpp, 0 }
        };

        var dirsToProcess = new Stack<string>();
        var excludedSet = new HashSet<string>(excludedFolders, StringComparer.OrdinalIgnoreCase);
        dirsToProcess.Push(localPath);

        while (dirsToProcess.Count > 0)
        {
            var currentDir = dirsToProcess.Pop();

            try
            {
                foreach (var file in Directory.EnumerateFiles(currentDir))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    switch (ext)
                    {
                        case ".cs": extCount[AnalysisLanguage.CSharp]++; break;
                        case ".js":
                        case ".ts": extCount[AnalysisLanguage.JavaScript]++; break;
                        case ".php": extCount[AnalysisLanguage.Php]++; break;
                        case ".cpp":
                        case ".cxx":
                        case ".cc":
                        case ".h":
                        case ".hpp": extCount[AnalysisLanguage.Cpp]++; break;
                    }
                }

                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (!excludedSet.Contains(dirName) && !dirName.StartsWith("."))
                    {
                        dirsToProcess.Push(subDir);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        // Pilih bahasa dengan jumlah file terbanyak
        var majorityLanguage = extCount.OrderByDescending(x => x.Value).First();
        
        // Default fallback ke CSharp jika kosong
        return majorityLanguage.Value > 0 ? majorityLanguage.Key : AnalysisLanguage.CSharp;
    }

    private static IReadOnlyCollection<string> 
        GetExtensionsForLanguage(AnalysisLanguage language)
    {
        return language switch
        {
            AnalysisLanguage.CSharp     => new[] { ".cs" },
            AnalysisLanguage.JavaScript => new[] { ".js", ".ts" },
            AnalysisLanguage.Php        => new[] { ".php" },
            AnalysisLanguage.Cpp        => new[] { ".cpp", ".cxx", ".cc", ".h", ".hpp" },
            _                           => Array.Empty<string>()
        };
    }

    private static QueueProgressEvent ToEvent(
        TreeSitterProgress<CodeGraph> progress, 
        Guid projectId, Guid queueId, string jobType)
    {
        return new QueueProgressEvent(
            ProjectId: projectId,
            QueueId: queueId,
            JobType: jobType,
            Status: QueueStatus.Running,
            Progress: progress.Percentage,
            Message: progress.Message
        );
    }
}

