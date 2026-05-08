using System.Text.Json;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Workers;

public class CodeGraphAnalysisWorker : BaseQueueWorker
{
    public override string JobType => nameof(JobTypeEnum.CodeGraph);

    public CodeGraphAnalysisWorker(
        IServiceScopeFactory scopeFactory,
        IQueueProgressNotifier progressNotifier,
        ILogger<CodeGraphAnalysisWorker> logger) 
        : base(scopeFactory, progressNotifier, logger)
    {
    }

    protected override async Task ProcessJobAsync(ProjectQueue job, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var analyzer = scope.ServiceProvider.GetRequiredService<ICodeAnalyzer>();
        var reader = scope.ServiceProvider.GetRequiredService<ICodebaseReader>();
        var repoConfig = scope.ServiceProvider.GetRequiredService<RepoConfig>();

        if (job.Project == null) 
        {
            // Check if project is null
            throw new InvalidOperationException("Project data is missing from the queue job.");
        }

        var localPath = job.Project.LocalPath;
        if (!Directory.Exists(localPath)) 
        {
            // Check if local path exists
            throw new DirectoryNotFoundException($"Repository path not found: {localPath}");
        }

        // 1. Tentukan bahasa secara otomatis
        var excluded = repoConfig.ExcludedFolders ?? Array.Empty<string>();
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

        // 4. Simpan hasil Code Graph ke database
        var graphJson = JsonSerializer.Serialize(finalGraph);
        
        var analysis = new CodeGraphAnalysis
        {
            UserId = job.Project.UserId,
            ProjectId = job.ProjectId,

            CommitHash = job.Project.LastCommitHash,
            GeneratedAtUtc = DateTime.UtcNow,

            GraphJson = graphJson,
            NodeCount = finalGraph.Nodes?.Count ?? 0,
            EdgeCount = finalGraph.SourceRelEdges?.Count + 
                        finalGraph.UseRelEdges?.Count ?? 0
        };

        // Add CodeGraph analysis to database
        dbContext.CodeGraphAnalyses.Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("CodeGraphAnalysis saved successfully for project {ProjectId}", job.ProjectId);
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
