using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Services;
using GithubAnalyzer.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Features.Analysis;

public static class AnalysisEndpoint
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapGet("/api/analysis/history", [Authorize] async (ApplicationDbContext db) =>
            {
                var history = await db.AnalysisJobs
                    .OrderByDescending(j => j.CreatedAt)
                    .Select(j => new { j.JobId, j.RepoUrl, j.Status, j.Progress, j.CreatedAt, j.CompletedAt })
                    .ToListAsync();
                return Results.Ok(history);
            })
            .WithName("GetAnalysisHistory")
            .WithTags("Analysis")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/analysis/result/{jobId}", [Authorize] async (string jobId, ApplicationDbContext db) =>
            {
                var job = await db.AnalysisJobs.FirstOrDefaultAsync(j => j.JobId == jobId);
                if (job == null) return Results.NotFound();
                if (job.ResultJson == null) return Results.BadRequest(new { message = "Analysis is still in progress or failed." });
                
                return Results.Content(job.ResultJson, "application/json");
            })
            .WithName("GetAnalysisResult")
            .WithTags("Analysis")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/repo/analyze", async (
                AnalyzeRepositoryRequest request,
                Channel<AnalysisJob> queue,
                ProgressTracker progressTracker,
                ApplicationDbContext db,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.RepoUrl))
                {
                    return Results.BadRequest(new { message = "RepoUrl is required." });
                }

                if (!Uri.TryCreate(request.RepoUrl.Trim(), UriKind.Absolute, out var repoUri)
                    || (repoUri.Scheme != Uri.UriSchemeHttp && repoUri.Scheme != Uri.UriSchemeHttps))
                {
                    return Results.BadRequest(new { message = "RepoUrl must be a valid http or https URL." });
                }

                var jobId = Guid.NewGuid().ToString("N");
                var job = new AnalysisJob
                {
                    JobId = jobId,
                    RepoUrl = request.RepoUrl.Trim(),
                    ProgressPercentage = 0,
                    CurrentStatus = "Queued",
                    CreatedAt = DateTime.UtcNow
                };

                // Save to DB
                db.AnalysisJobs.Add(new AnalysisJobEntity
                {
                    JobId = jobId,
                    RepoUrl = job.RepoUrl,
                    Status = job.CurrentStatus,
                    Progress = job.ProgressPercentage,
                    CreatedAt = job.CreatedAt
                });
                await db.SaveChangesAsync(cancellationToken);

                progressTracker.TryAdd(job);
                await queue.Writer.WriteAsync(job, cancellationToken);

                return Results.Accepted(
                    $"/api/repo/analyze/stream/{jobId}",
                    new AnalyzeRepositoryResponse(jobId));
            })
            .WithName("AnalyzeRepository")
            .WithTags("Analysis")
            .WithSummary("Queue repository analysis")
            .WithDescription("Queues repository analysis and returns a JobId for progress tracking.")
            .Accepts<AnalyzeRepositoryRequest>("application/json")
            .Produces<AnalyzeRepositoryResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapGet("/api/repo/analyze/stream/{jobId}", (
                string jobId,
                ProgressTracker progressTracker,
                CancellationToken cancellationToken) =>
            {
                if (!progressTracker.TryGetSnapshot(jobId, out _))
                {
                    return Results.NotFound(new { message = "Job not found." });
                }

                var stream = StreamProgress(jobId, progressTracker, cancellationToken);
                return Results.ServerSentEvents(stream);
            })
            .WithName("StreamRepositoryAnalysis")
            .WithTags("Analysis")
            .WithSummary("Stream repository analysis progress")
            .WithDescription("Streams progress updates using Server-Sent Events (SSE).")
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async IAsyncEnumerable<AnalysisProgressUpdate> StreamProgress(
        string jobId,
        ProgressTracker progressTracker,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!progressTracker.TryGetSnapshot(jobId, out var job))
            {
                yield break;
            }

            var payload = new AnalysisProgressUpdate(job.ProgressPercentage, job.CurrentStatus);
            yield return payload;

            if (string.Equals(job.CurrentStatus, "Completed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(job.CurrentStatus, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }
}
