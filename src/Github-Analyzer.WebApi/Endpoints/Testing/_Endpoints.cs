using Asp.Versioning;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Endpoints.Testing;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/testing")
            .WithApiVersionSet(versionSet)
            .WithTags("Testing");

        group.MapBenchmarkEndpoint()
            .WithName("BenchmarkRepository")
            .Produces<ApiResponse<BenchmarkResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
