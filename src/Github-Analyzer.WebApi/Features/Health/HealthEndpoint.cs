using Microsoft.AspNetCore.Http;

namespace GithubAnalyzer.WebApi.Features.Health;

public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new HealthResponse(
                "ok",
                "Github-Analyzer.WebApi",
                DateTime.UtcNow)))
            .WithName("ApiHealth")
            .WithTags("Health")
            .WithSummary("Health check")
            .WithDescription("Returns API health status and current UTC timestamp.")
            .Produces<HealthResponse>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record HealthResponse(string Status, string Service, DateTime TimestampUtc);
