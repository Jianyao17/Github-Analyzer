using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OutputCaching;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Models.Analysis;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public sealed record CreateProjectRequest(
    [Required, Url] string RepoUrl,
    [Required, StringLength(100)] string Branch,
    [StringLength(50)] string? CommitHash);

public static class CreateProjectEndpoint
{
    public static RouteHandlerBuilder MapCreateProjectEndpoint(this RouteGroupBuilder group)
    {
        return group.MapPost("/new", async (
            CreateProjectRequest request, ClaimsPrincipal claimsPrincipal,
            AppDbContext dbContext, IRepositoryFetcher repositoryFetcher,
            CancellationToken ct) =>
        {
            // Get User ID from claims
            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            claimsPrincipal.FindFirstValue("sub");
            
            // Try to parse user ID
            if (!Guid.TryParse(userIdStr, out var userId))
                return ApiResults.Unauthorized("Invalid user identifier.");

            // Fetch and Extract Code
            RepositoryResult repoResult;
            try
            {
                repoResult = await repositoryFetcher.DownloadAndExtractAsync(
                    request.RepoUrl, request.Branch, request.CommitHash, ct);
            }
            catch (NotSupportedException ex)
            {
                return ApiResults.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResults.InternalServerError(ex.Message);
            }

            // Create Project Entity
            var project = new Entities.Repo.Project
            {
                UserId          = userId,
                Title           = repoResult.RepositoryName,
                RepositoryName  = repoResult.RepositoryName,
                RepositoryUrl   = repoResult.RepositoryUrl,
                
                AuthorName      = repoResult.AuthorName,
                LocalPath       = repoResult.ExtractPath,
                BranchName      = repoResult.BranchName ?? request.Branch,
                LastCommitHash  = repoResult.LastCommitHash,
                LastCommitAtUtc = repoResult.LastCommitAtUtc,
                Description     = repoResult.Description
            };

            // Add project to database
            // and save to prevent race conditions with 
            // queued jobs that reference the project
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(ct);

            // Queue jobs for analysis (Statistic & CodeGraph)
            var statisticJob = new ProjectQueue
            {
                Project = project,
                JobType = AnalysisType.Statistic.ToString(),
                Status = Entities.QueueStatus.Pending,
                Priority = 10
            };
            
            var codeGraphJob = new ProjectQueue
            {
                Project = project,
                JobType = AnalysisType.CodeGraph.ToString(),
                Status = Entities.QueueStatus.Pending,
                Priority = 10
            };

            // Save project and jobs to database
            dbContext.ProjectQueues.AddRange(statisticJob, codeGraphJob);
            await dbContext.SaveChangesAsync(ct);

            // Return the created project
            var response = new ProjectResponse(
                project.Id,
                project.Title,
                project.RepositoryName,
                project.RepositoryUrl,
                project.BranchName,
                project.LastCommitHash,
                project.CreatedAtUtc,
                HasStatistic: false,
                HasCodeGraph: false);

            return ApiResults.Created(
                $"/api/v1/projects/{project.Id}",
                response, "Project created successfully.");
        });
    }
}
