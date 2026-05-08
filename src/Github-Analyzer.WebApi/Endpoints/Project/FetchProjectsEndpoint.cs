using System.Security.Claims;
using GithubAnalyzer.WebApi.Database;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class FetchProjectsEndpoint
{
    public static RouteHandlerBuilder MapListProjectsEndpoint(this RouteGroupBuilder group)
    {
        // GET /api/projects
        return group.MapGet("/", async (
            ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext,
            CancellationToken ct) =>
        {
            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            // Try to parse user ID
            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            // Get projects for the user
            var projects = await dbContext.Projects
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Select(p => new ProjectResponse(
                    p.Id,
                    p.RepositoryName,
                    p.RepositoryUrl,
                    p.BranchName,
                    p.LastCommitHash,
                    p.CreatedAtUtc))
                .ToListAsync(ct);

            return Results.Ok(projects);
        });
    }

    public static RouteHandlerBuilder MapGetProjectEndpoint(this RouteGroupBuilder group)
    {
        // GET /api/projects/{project_guid}
        return group.MapGet("/{projectGuid:guid}", async (
            Guid projectGuid, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, CancellationToken ct) =>
        {
            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            // Try to parse user ID
            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            // Get project for the user
            var project = await dbContext.Projects
                .Where(p => p.Id == projectGuid && p.UserId == userId)
                .Select(p => new ProjectResponse(
                    p.Id,
                    p.RepositoryName,
                    p.RepositoryUrl,
                    p.BranchName,
                    p.LastCommitHash,
                    p.CreatedAtUtc))
                .FirstOrDefaultAsync(ct);

            if (project == null)
                return Results.NotFound();

            return Results.Ok(project);
        });
    }
}
