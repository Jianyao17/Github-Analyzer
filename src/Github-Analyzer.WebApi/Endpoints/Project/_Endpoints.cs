using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Models;
using Asp.Versioning;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/projects")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization()
            .WithTags("Projects");

        // Project Management
        group.MapCreateProjectEndpoint()
            .WithName("CreateProject")
            .RequireRateLimiting(RateLimitPolicies.Write)
            .Accepts<CreateProjectRequest>("application/json")
            .Produces<ApiResponse<ProjectResponse>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem();

        group.MapListProjectsEndpoint()
            .WithName("ListProjects")
            .Produces<ApiResponse<List<ProjectResponse>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireUserCache();

        group.MapGetProjectEndpoint()
            .WithName("GetProject")
            .Produces<ApiResponse<ProjectResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireUserCache();

        group.MapRenameProjectEndpoint()
            .WithName("RenameProject")
            .RequireRateLimiting(RateLimitPolicies.Write)
            .Accepts<RenameProjectRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapDeleteProjectEndpoint()
            .WithName("DeleteProject")
            .RequireRateLimiting(RateLimitPolicies.Write)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);


        // Queues and Status
        group.MapIssueStreamTokenEndpoint()
            .WithName("IssueStreamToken")
            .Produces<ApiResponse<StreamTokenResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapStreamQueueProgressEndpoint()
            .WithName("StreamQueueProgress")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Analysis Results
        group.MapGetAnalysisEndpoint()
            .WithName("GetAnalysis")
            .Produces<ApiResponse<StatisticAnalysis>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<CodeGraphAnalysis>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireUserCache();

        group.MapGetSourceContentEndpoint()
            .WithName("GetSourceContent")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireUserCache();

        // External Repositories (Github)
        group.MapFetchRepoInfoEndpoint()
            .WithName("FetchRepoInfo")
            .Produces<ApiResponse<FetchRepoInfoResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
