using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class GetSourceContentEndpoint
{
    public static RouteHandlerBuilder MapGetSourceContentEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/{projectId:guid}/content", async (
            Guid projectId, string path,
            ClaimsPrincipal claimsPrincipal,
            ISourceCodeProvider sourceCodeProvider,
            AppDbContext dbContext,
            CancellationToken ct) =>
        {
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                            claimsPrincipal.FindFirstValue("sub");

            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            var project = await dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == projectId && 
                    p.UserId == userId, 
                ct);

            if (project == null)
                return ApiResults.NotFound("Project not found.");

            if (string.IsNullOrWhiteSpace(path))
                return ApiResults.BadRequest("Path is required.");

            var content = await sourceCodeProvider.GetFileContentAsync(project, path, ct);

            if (content == null)
                return ApiResults.NotFound("File not found or could not be fetched.");

            // Return content directly or wrap in JSON
            return Results.Ok(new { path, content });
        });
    }
}
