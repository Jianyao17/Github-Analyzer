using Microsoft.AspNetCore.Http;

namespace GithubAnalyzer.WebApi.Features.Auth.Configuration;

public static class AuthConfigurationEndpoint
{
    public static IEndpointRouteBuilder MapAuthConfigurationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/options", (IConfiguration configuration) =>
            {
                var googleClientId = configuration["Authentication:Google:ClientId"];
                var googleClientSecret = configuration["Authentication:Google:ClientSecret"];

                return Results.Ok(new AuthOptionsResponse(
                    !string.IsNullOrWhiteSpace(googleClientId)
                    && !string.IsNullOrWhiteSpace(googleClientSecret)));
            })
            .WithName("GetAuthOptions")
            .WithTags("Auth")
            .WithSummary("Get authentication configuration")
            .WithDescription("Returns the enabled authentication providers and flags used by the frontend.")
            .Produces<AuthOptionsResponse>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record AuthOptionsResponse(bool GoogleEnabled);
