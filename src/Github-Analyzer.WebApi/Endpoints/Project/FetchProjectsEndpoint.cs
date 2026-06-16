using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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
                return ApiResults.Unauthorized("Invalid user identifier.");

            // Get projects for the user, including analysis availability flags
            var projects = await dbContext.Projects
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Select(p => new ProjectResponse(
                    p.Id,
                    p.Title,
                    p.RepositoryName,
                    p.RepositoryUrl,
                    p.BranchName,
                    p.LastCommitHash,
                    p.CreatedAtUtc,
                    
                    // Check if analyses exist for this project to set availability flags
                    dbContext.StatisticAnalyses.Any(s => s.ProjectId == p.Id),
                    dbContext.CodeGraphAnalyses.Any(g => g.ProjectId == p.Id)))
                .ToListAsync(ct);

            return ApiResults.Ok(projects);
        });
    }

    public static RouteHandlerBuilder MapGetProjectEndpoint(this RouteGroupBuilder group)
    {
        // GET /api/projects/{project_guid}
        return group.MapGet("/{projectGuid:guid}", async (
            Guid projectGuid, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, IProjectCacheService cache, CancellationToken ct) =>
        {
            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            // Try to parse user ID
            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            var cachedJson = await cache.GetProjectJsonAsync(projectGuid, ct);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var cachedProject = JsonSerializer.Deserialize<ProjectResponse>(cachedJson);
                if (cachedProject != null)
                {
                    return ApiResults.Ok(cachedProject);
                }
            }

            // Get project for the user, including analysis availability flags
            var project = await dbContext.Projects
                .Where(p => p.Id == projectGuid && p.UserId == userId)
                .Select(p => new ProjectResponse(
                    p.Id, p.Title,
                    p.RepositoryName,
                    p.RepositoryUrl,
                    p.BranchName,
                    p.LastCommitHash,
                    p.CreatedAtUtc,

                    // Check if analyses exist for this project to set availability flags
                    dbContext.StatisticAnalyses.Any(s => s.ProjectId == p.Id),
                    dbContext.CodeGraphAnalyses.Any(g => g.ProjectId == p.Id)))
                .FirstOrDefaultAsync(ct);

            if (project == null)
                return ApiResults.NotFound("Project not found.");

            // Cache project data
            await cache.SetProjectJsonAsync(projectGuid, JsonSerializer.Serialize(project), ct);

            return ApiResults.Ok(project);
        });
    }
}
