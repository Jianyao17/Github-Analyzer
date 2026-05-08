using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public sealed record FetchRepoInfoResponse(
    IReadOnlyList<RepoBranch> Branches,
    IReadOnlyList<RepoCommit>? Commits);

public static class FetchRepoInfoEndpoint
{
    public static RouteHandlerBuilder MapFetchRepoInfoEndpoint(this RouteGroupBuilder group)
    {
        return group.MapGet("/github/info", async (
            string repoUrl, string? branch,
            IRepositoryFetcher repositoryFetcher,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(repoUrl))
                return Results.BadRequest(new { message = "repoUrl is required" });

            try
            {
                // Get branches for the repository
                var branches = await repositoryFetcher.GetBranchesAsync(repoUrl, ct);
                
                // Get commits for the branch if provided
                IReadOnlyList<RepoCommit>? commits = null;
                if (!string.IsNullOrWhiteSpace(branch))
                {
                    commits = await repositoryFetcher.GetCommitsAsync(repoUrl, branch, ct);
                }

                // Return the branches and commits
                return Results.Ok(new FetchRepoInfoResponse(branches, commits));
            }
            catch (NotSupportedException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, 
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });
    }
}
