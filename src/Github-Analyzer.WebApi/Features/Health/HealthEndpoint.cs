namespace GithubAnalyzer.WebApi.Features.Health;

public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/health",
            () => Results.Ok(new
            {
                status = "ok",
                service = "Github-Analyzer.WebApi",
                timestampUtc = DateTime.UtcNow
            }))
            .WithName("ApiHealth")
            .WithTags("Health");

        return app;
    }
}
