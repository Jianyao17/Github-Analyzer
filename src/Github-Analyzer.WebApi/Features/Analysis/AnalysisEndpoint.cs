using GithubAnalyzer.Analysis;
using Microsoft.AspNetCore.Authorization;

namespace GithubAnalyzer.WebApi.Features.Analysis;

public static class AnalysisEndpoint
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/analysis/sample",
            [Authorize] () =>
            {
                var snapshot = RepositoryAnalysisFactory.CreateSample();
                return Results.Ok(snapshot);
            })
            .WithName("GetSampleAnalysis")
            .WithTags("Analysis");

        return app;
    }
}
