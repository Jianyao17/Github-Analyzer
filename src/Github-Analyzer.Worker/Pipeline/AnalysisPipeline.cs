using GithubAnalyzer.Shared.Git;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Services;
using Microsoft.Extensions.Options;

namespace GithubAnalyzer.Worker.Pipeline;

/// <summary>
/// Orchestrator pipeline analisis yang mengelola lifecycle eksekusi job, idempotensi, dan sinkronisasi status ke DB.
/// </summary>
public sealed class AnalysisPipeline(
    IEnumerable<IAnalysisStep> steps,
    IOptions<AnalysisConfig> config, IGitService gitService,
    IProjectQueueRepository queueRepository, IDbAnalysisCacheService cacheService,
    IAnalysisProgressPublisher progressPublisher, ILoggerFactory loggerFactory)
  : IAnalysisPipeline
{
    private readonly AnalysisConfig _config = config.Value;
    private readonly ILogger<AnalysisPipeline> _logger = loggerFactory.CreateLogger<AnalysisPipeline>();

    public async Task ExecuteAsync(AnalysisJobMessage job, CancellationToken ct = default)
    {
        _logger.LogInformation("Pipeline execution started for Job {JobId} (Project {ProjectId})", job.JobId, job.ProjectId);

        // 1. Sinkronisasi DB: Tandai Job Running
        await queueRepository.MarkJobRunningAsync(job.JobId, ct);

        // 2. Inisialisasi PipelineContext pembawa runtime services & ILoggerFactory
        await using var context = new PipelineContext(
            job, _config, gitService, cacheService,
            progressPublisher, loggerFactory);

        try
        {
            // 3. Eksekusi urut tiap step yang aktif
            foreach (var step in steps)
            {
                if (step.ShouldExecute(context))
                {
                    await step.ExecuteAsync(context, ct);
                }
            }

            // 4. Sukses: Tandai Job Completed di DB
            await queueRepository.MarkJobCompletedAsync(job.JobId, ct);
            _logger.LogInformation("Pipeline execution completed successfully for Job {JobId}", job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline execution failed for Job {JobId}: {Message}", job.JobId, ex.Message);
            await queueRepository.MarkJobFailedAsync(job.JobId, ex.Message, ct);
            throw;
        }
    }
}
