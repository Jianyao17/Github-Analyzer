using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Channels;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Endpoints.Analysis;

public sealed record AnalyzeRepositoryRequest(
    [Required, Url] string RepoUrl);

public sealed record AnalyzeRepositoryResponse(string JobId);

public sealed record AnalysisProgressUpdate(
    int ProgressPercentage,
    string CurrentStatus);


public static class AnalysisEndpoint
{
    public static RouteHandlerBuilder MapHistoryEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/history", [Authorize] async (AppDbContext db) =>
        {
            var history = await db.ProjectQueues
                .AsNoTracking()
                .Include(q => q.Project)
                .OrderByDescending(q => q.CreatedAtUtc)
                .Select(q => new
                {
                    JobId = q.Id,
                    q.ProjectId,
                    RepoUrl = q.Project.RepositoryUrl,
                    Status = q.Status.ToString(),
                    JobType = q.JobType.ToString(),
                    q.CreatedAtUtc,
                    q.StartedAtUtc,
                    q.CompletedAtUtc
                })
                .ToListAsync();
            return Results.Ok(history);
        });
    }

    public static RouteHandlerBuilder MapResultEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/result/{jobId}", [Authorize] async (string jobId, AppDbContext db) =>
        {
            if (!Guid.TryParse(jobId, out var queueId))
            {
                return Results.BadRequest(new { message = "Invalid job id." });
            }

            var queue = await db.ProjectQueues
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == queueId);
            if (queue == null) return Results.NotFound();

            var graph = await db.CodeGraphAnalyses
                .AsNoTracking()
                .Where(cg => cg.ProjectId == queue.ProjectId && cg.GeneratedAtUtc >= queue.CreatedAtUtc)
                .OrderByDescending(cg => cg.GeneratedAtUtc)
                .FirstOrDefaultAsync();

            if (graph == null)
            {
                return Results.BadRequest(new { message = "Analysis is still in progress or failed." });
            }

            return Results.Content(graph.GraphJson, "application/json");
        });
    }

    public static RouteHandlerBuilder MapAnalyzeEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/analyze", async (
                AnalyzeRepositoryRequest request,
                Channel<AnalysisJob> queue,
                ProgressTracker progressTracker,
                ClaimsPrincipal user,
                AppDbContext db,
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

            var normalizedUrl = request.RepoUrl.Trim();
            var repoName = GetRepositoryName(normalizedUrl);
            var userId = ResolveUserId(user);

            var projectQuery = db.Projects.AsQueryable();
            if (userId != Guid.Empty)
            {
                projectQuery = projectQuery.Where(p => p.UserId == userId);
            }

            var project = await projectQuery
                .FirstOrDefaultAsync(p => p.RepositoryUrl == normalizedUrl, cancellationToken);

            if (project == null)
            {
                project = new Project
                {
                    UserId = userId,
                    RepositoryUrl = normalizedUrl,
                    RepositoryName = repoName,
                    LocalPath = repoName
                };
                db.Projects.Add(project);
                await db.SaveChangesAsync(cancellationToken);
            }

            var queueEntry = new ProjectQueue
            {
                ProjectId = project.Id,
                JobType = "CodeGraph",
                Status = QueueStatus.Pending,
                ScheduledAtUtc = DateTime.UtcNow
            };
            db.ProjectQueues.Add(queueEntry);
            await db.SaveChangesAsync(cancellationToken);

            var jobId = queueEntry.Id.ToString("N");
            var job = new AnalysisJob
            {
                JobId = jobId,
                QueueId = queueEntry.Id,
                ProjectId = project.Id,
                UserId = project.UserId,
                RepoUrl = normalizedUrl,
                ProgressPercentage = 0,
                CurrentStatus = "Queued",
                CreatedAt = DateTime.UtcNow
            };

            progressTracker.TryAdd(job);
            await queue.Writer.WriteAsync(job, cancellationToken);

            return Results.Accepted(
                $"/api/repo/analyze/stream/{jobId}",
                new AnalyzeRepositoryResponse(jobId));
        });
    }

    public static RouteHandlerBuilder MapStreamEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/analyze/stream/{jobId}", (
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
        });
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

    private static Guid ResolveUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(userId, out var parsed) ? parsed : Guid.Empty;
    }

    private static string GetRepositoryName(string repoUrl)
    {
        var trimmed = repoUrl.TrimEnd('/');
        var lastSegment = trimmed.Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastSegment))
        {
            return "unknown";
        }

        return lastSegment.Length > 50 ? lastSegment[..50] : lastSegment;
    }
}

