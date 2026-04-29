using GithubAnalyzer.Analysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace GithubAnalyzer.WebApi.Features.Analysis;

public static class AnalysisEndpoint
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analysis/sample", [Authorize] () =>
            {
                var snapshot = RepositoryAnalysisFactory.CreateSample();
                return Results.Ok(snapshot);
            })
            .WithName("GetSampleAnalysis")
            .WithTags("Analysis")
            .WithSummary("Get sample analysis")
            .WithDescription("Returns a sample repository analysis snapshot. Requires Authorization: Bearer token.")
            .Produces<RepositoryAnalysisSnapshot>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
