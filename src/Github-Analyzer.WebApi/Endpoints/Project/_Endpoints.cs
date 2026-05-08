using GithubAnalyzer.WebApi.Entities.Analysis;

namespace GithubAnalyzer.WebApi.Endpoints.Project;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        // Project Management
        group.MapCreateProjectEndpoint()
            .WithName("CreateProject")
            .Accepts<CreateProjectRequest>("application/json")
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapListProjectsEndpoint()
            .WithName("ListProjects")
            .Produces<List<ProjectResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGetProjectEndpoint()
            .WithName("GetProject")
            .Produces<ProjectResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // Queues and Status
        group.MapStreamQueueProgressEndpoint()
            .WithName("StreamQueueProgress")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // Analysis Results
        group.MapGetStatisticAnalysisEndpoint()
            .WithName("GetStatisticAnalysis")
            .Produces<StatisticAnalysis>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGetCodeGraphAnalysisEndpoint()
            .WithName("GetCodeGraphAnalysis")
            .Produces<CodeGraphAnalysis>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // External Repositories (Github)
        group.MapFetchRepoInfoEndpoint()
            .WithName("FetchRepoInfo")
            .Produces<FetchRepoInfoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
