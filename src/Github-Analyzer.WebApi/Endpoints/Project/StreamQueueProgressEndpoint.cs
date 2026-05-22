using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class StreamQueueProgressEndpoint
{
    public static RouteHandlerBuilder MapStreamQueueProgressEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/{projectGuid:guid}/queue/event", async (
            Guid projectGuid, string job_type, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, IQueueProgressNotifier progressNotifier,
            HttpContext context, CancellationToken ct) =>
        {
            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            // Try to parse user ID
            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            var projectExists = await dbContext.Projects
                .AnyAsync(p => p.Id == projectGuid && p.UserId == userId, ct);

            if (!projectExists)
                return ApiResults.NotFound("Project not found or access denied.");

            // Set headers for Server-Sent Events (SSE)
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            // Get the progress stream for the job type
            var stream = progressNotifier.SubscribeAsync(projectGuid, job_type, ct);

            try
            {
                // Stream progress events to client as SSE
                await foreach (var progressEvent in stream.WithCancellation(ct))
                {
                    var data = JsonSerializer.Serialize(progressEvent);
                    await context.Response.WriteAsync($"data: {data}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }

            return Results.Empty; // Response has already started
        });
    }
}
