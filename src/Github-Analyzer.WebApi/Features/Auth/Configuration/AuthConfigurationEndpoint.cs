namespace GithubAnalyzer.WebApi.Features.Auth.Configuration;

public static class AuthConfigurationEndpoint
{
    public static IEndpointRouteBuilder MapAuthConfigurationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/auth/options",
            (IConfiguration configuration) =>
            {
                var googleClientId = configuration["Authentication:Google:ClientId"];
                var googleClientSecret = configuration["Authentication:Google:ClientSecret"];

                return Results.Ok(new
                {
                    googleEnabled =
                        !string.IsNullOrWhiteSpace(googleClientId)
                        && !string.IsNullOrWhiteSpace(googleClientSecret)
                });
            })
            .WithName("GetAuthOptions")
            .WithTags("Auth");

        return app;
    }
}
