namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapRegisterEndpoint()
            .WithName("Register")
            .Accepts<RegisterRequest>("application/json")
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapLoginEndpoint()
            .WithName("Login")
            .Accepts<LoginRequest>("application/json")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGetCurrentUserEndpoint()
            .WithName("GetCurrentUser")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGoogleLoginEndpoint()
            .WithName("GoogleLogin")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGoogleCallbackEndpoint()
            .WithName("GoogleCallback")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return app;
    }
}
