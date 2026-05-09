namespace GithubAnalyzer.WebApi.Endpoints.Testing;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/testing")
            .WithTags("Testing");

        group.MapBenchmarkEndpoint()
            .WithName("BenchmarkRepository")
            .Produces<BenchmarkResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }
}
