using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Config;
using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Shared.Git;

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
            AppDbContext dbContext, IGitService gitService,
            IAnalysisJobDispatcher jobDispatcher,
            IOptions<AnalysisConfig> configOptions,
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
                repoResult = await gitService.DownloadAndExtractAsync(
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
            var project = new Shared.Entities.Project
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

            // Add project to database and save
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(ct);

            // Create a single unified ProjectQueue job for analysis pipeline
            var queueJob = new Shared.Entities.ProjectQueue
            {
                ProjectId = project.Id,
                Status = JobQueueStatus.Pending,
                Priority = 10,

                Options = new AnalysisOptions
                {
                    Statistics = true,
                    CodeGraph = true
                }
            };

            dbContext.ProjectQueues.Add(queueJob);
            await dbContext.SaveChangesAsync(ct);

            // Construct AnalysisJobMessage for Redis Streams worker
            var analysisConfig = configOptions.Value;
            var jobMessage = new AnalysisJobMessage
            {
                JobId = queueJob.Id,
                ProjectId = project.Id,
                UserId = userId,

                RepositoryUrl = project.RepositoryUrl,
                RepositoryName = project.RepositoryName,
                Branch = project.BranchName ?? request.Branch,
                CommitHash = project.LastCommitHash,
                Options = queueJob.Options,

                StatisticsVersion = analysisConfig.StatisticVersion,
                CodeGraphVersion = analysisConfig.CodeGraphVersion
            };

            // Dispatch job to Redis message broker
            try
            {
                await jobDispatcher.DispatchAsync(jobMessage, ct);
            }
            catch (Exception ex)
            {
                queueJob.Status = JobQueueStatus.Failed;
                queueJob.LastError = $"Failed to dispatch job to message broker: {ex.Message}";

                await dbContext.SaveChangesAsync(CancellationToken.None);
                return ApiResults.InternalServerError($"Project created but failed to enqueue analysis job: {ex.Message}");
            }

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
