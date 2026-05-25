using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Services.Auth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public sealed record StreamTokenResponse(string Token, DateTimeOffset ExpiresAt);

public static class IssueStreamTokenEndpoint
{
    public static RouteHandlerBuilder MapIssueStreamTokenEndpoint(this RouteGroupBuilder group)
    {
        // POST /api/v1/projects/{projectGuid}/queue/stream-token
        return group.MapPost("/{projectGuid:guid}/queue/stream-token", async (
            Guid projectGuid, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, StreamTokenService streamTokenService,
            CancellationToken ct) =>
        {
            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                            claimsPrincipal.FindFirstValue("sub");

            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            // Ensure the project exists and belongs to the user
            var projectExists = await dbContext.Projects
                .AnyAsync(p => p.Id == projectGuid && p.UserId == userId, ct);

            if (!projectExists)
                return ApiResults.NotFound("Project not found or access denied.");

            // Create a stream token valid for 5 minutes
            // UserId is omitted from the token — ownership is already enforced by the DB check above
            var (token, expiresAt) = streamTokenService.CreateToken(projectGuid);

            return ApiResults.Ok(new StreamTokenResponse(token, expiresAt));
        });
    }
}
