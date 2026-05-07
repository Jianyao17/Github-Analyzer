namespace GithubAnalyzer.WebApi.Endpoints.Analysis;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var analysisGroup = app.MapGroup("/api/analysis").WithTags("Analysis");
        var repoGroup = app.MapGroup("/api/repo").WithTags("Analysis");

        analysisGroup.MapHistoryEndpoint()
            .WithName("GetAnalysisHistory")
            .Produces(StatusCodes.Status200OK);

        analysisGroup.MapResultEndpoint()
            .WithName("GetAnalysisResult")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        repoGroup.MapAnalyzeEndpoint()
            .WithName("AnalyzeRepository")
            .Accepts<AnalyzeRepositoryRequest>("application/json")
            .Produces<AnalyzeRepositoryResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        repoGroup.MapStreamEndpoint()
            .WithName("StreamRepositoryAnalysis")
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
