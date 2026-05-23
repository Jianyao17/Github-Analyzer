using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public sealed record StreamProgressRequest(
    [property: FromRoute(Name = "projectGuid")]  Guid ProjectGuid,
    [property: FromQuery(Name = "job_type")]     string JobType,
    [property: FromQuery(Name = "stream_token")] string StreamToken);

public static class StreamQueueProgressEndpoint
{
    public static RouteHandlerBuilder MapStreamQueueProgressEndpoint(this RouteGroupBuilder group)
    {
        // GET /api/v1/projects/{projectGuid}/queue/event?job_type=analysis&stream_token=abc123
        return group.MapGet("/{projectGuid:guid}/queue/event", async (
            [AsParameters] StreamProgressRequest request,
            StreamTokenService streamTokenService,
            IQueueProgressNotifier progressNotifier,
            HttpContext context, CancellationToken ct) =>
        {
            // Validate stream token to ensure the client is authorized to receive events for this project
            var payload = streamTokenService.ValidateToken(request.StreamToken, request.ProjectGuid);
            if (payload is null)
                return ApiResults.Unauthorized("Stream token invalid, expired, or does not match project.");

            // Set headers for Server-Sent Events (SSE)
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            // Subscribe to progress events for the specified project and job type
            var stream = progressNotifier.SubscribeAsync(request.ProjectGuid, request.JobType, ct);

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
                // Client disconnected — normal
            }

            return Results.Empty; 
        })
        .AllowAnonymous(); // Auth is handled via stream token, not Authorization header
    }
}
