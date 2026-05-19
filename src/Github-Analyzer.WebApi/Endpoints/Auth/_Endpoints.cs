using GithubAnalyzer.WebApi.Models;

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
            .Produces<ApiResponse<string>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapLoginEndpoint()
            .WithName("Login")
            .Accepts<LoginRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGetCurrentUserEndpoint()
            .WithName("GetCurrentUser")
            .Produces<ApiResponse<UserProfileResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGoogleLoginEndpoint()
            .WithName("GoogleLogin")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGoogleCallbackEndpoint()
            .WithName("GoogleCallback")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return app;
    }
}
