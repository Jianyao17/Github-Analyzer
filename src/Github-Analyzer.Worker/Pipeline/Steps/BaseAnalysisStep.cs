using GithubAnalyzer.Shared.Enums;

namespace GithubAnalyzer.Worker.Pipeline.Steps;

/// <summary>
/// Base class untuk tahapan analisis dengan Template Method Pattern dan dynamic logging via GetType().
/// </summary>
public abstract class BaseAnalysisStep : IAnalysisStep
{
    public abstract AnalysisType AnalysisType { get; }
    public abstract bool ShouldExecute(PipelineContext context);

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct = default)
    {
        var logger = context.LoggerFactory.CreateLogger(GetType());

        logger.LogInformation("Starting {Step} for Project {ProjectId}", AnalysisType, context.Job.ProjectId);

        // 1. Cek DB Cache terlebih dahulu
        var isCacheHit = await TryCopyFromCacheAsync(context, ct);
        if (isCacheHit)
        {
            logger.LogInformation("{Step} Cache HIT. Skipping computation.", AnalysisType);
            await context.ReportProgressAsync(AnalysisType, 100, $"{AnalysisType} restored from cache", ct);
            return;
        }

        // 2. Cache MISS: Pastikan repository lokal sudah diunduh & tervalidasi
        await context.ReportProgressAsync(AnalysisType, 5, $"Preparing repository for {AnalysisType}...", ct);
        var localPath = await context.EnsureRepositoryDownloadedAsync(ct);

        // 3. Jalankan logika analisis inti masing-masing step dengan logger spesifik
        await ExecuteCoreAsync(context, localPath, logger, ct);

        // 4. Pastikan progress akhir 100%
        await context.ReportProgressAsync(AnalysisType, 100, $"{AnalysisType} analysis completed", ct);
        logger.LogInformation("{Step} completed successfully for Project {ProjectId}", AnalysisType, context.Job.ProjectId);
    }

    protected abstract Task<bool> TryCopyFromCacheAsync(PipelineContext context, CancellationToken ct);
    protected abstract Task ExecuteCoreAsync(PipelineContext context, string localPath, ILogger logger, CancellationToken ct);
}
