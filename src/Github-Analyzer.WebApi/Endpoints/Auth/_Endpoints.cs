using GithubAnalyzer.WebApi.Extensions;
using GithubAnalyzer.WebApi.Models;
using Asp.Versioning;

namespace GithubAnalyzer.WebApi.Endpoints.Auth;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .WithTags("Auth");

        group.MapRegisterEndpoint()
            .WithName("Register")
            .RequireRateLimiting(RateLimitPolicies.AccountManagement)
            .Accepts<RegisterRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapLoginEndpoint()
            .WithName("Login")
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .Accepts<LoginRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGetCurrentUserEndpoint()
            .WithName("GetCurrentUser")
            .Produces<ApiResponse<UserProfileResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapVerifyEmailEndpoint()
            .WithName("VerifyEmail")
            .RequireRateLimiting(RateLimitPolicies.AccountManagement)
            .Accepts<VerifyEmailRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapResendVerifyEmailEndpoint()
            .WithName("ResendVerifyEmail")
            .RequireRateLimiting(RateLimitPolicies.AccountManagement)
            .Accepts<ResendVerifyEmailRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapForgotPasswordEndpoint()
            .WithName("ForgotPassword")
            .RequireRateLimiting(RateLimitPolicies.AccountManagement)
            .Accepts<ForgotPasswordRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK);

        group.MapResetPasswordEndpoint()
            .WithName("ResetPassword")
            .RequireRateLimiting(RateLimitPolicies.AccountManagement)
            .Accepts<ResetPasswordRequest>("application/json")
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGoogleAuthIsEnabledEndpoint()
            .WithName("GoogleAuthIsEnabled")
            .Produces(StatusCodes.Status200OK);
            
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
