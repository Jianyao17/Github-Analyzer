using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace GithubAnalyzer.WebApi.Extensions;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddApiProblemDetails(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                // Enrich the ProblemDetails with additional information
                var httpContext = context.HttpContext;
                var path = httpContext.Request.Path.Value ?? string.Empty;
                var method = httpContext.Request.Method ?? string.Empty;
                
                var traceId = httpContext.Features.Get<IHttpActivityFeature>()
                    ?.Activity?.Id ?? string.Empty;

                context.ProblemDetails.Type = GetProblemType(context.ProblemDetails.Status);
                context.ProblemDetails.Instance = $"{method} {path}";
                context.ProblemDetails.Extensions["traceId"] = traceId;

                if (context.ProblemDetails is HttpValidationProblemDetails validationProblem &&
                    !context.ProblemDetails.Extensions.ContainsKey("details"))
                {
                    context.ProblemDetails.Extensions["details"] = validationProblem.Errors;
                }

                if (context.Exception is not null)
                {
                    context.ProblemDetails.Extensions["errorType"] =
                        context.Exception.GetType().Name;

                    if (environment.IsDevelopment() || environment.IsStaging())
                    {
                        context.ProblemDetails.Extensions["exceptionMessage"] =
                            context.Exception.Message;

                        if (context.Exception.InnerException is not null)
                        {
                            context.ProblemDetails.Extensions["innerError"] =
                                context.Exception.InnerException.Message;
                        }
                    }
                }
            };
        });

        return services;
    }

    private static string? GetProblemType(int? status)
    {
        return status switch
        {
            StatusCodes.Status400BadRequest =>
                "https://httpwg.org/specs/rfc9110.html#section-15.5.1",
            StatusCodes.Status401Unauthorized =>
                "https://httpwg.org/specs/rfc9110.html#section-15.5.2",
            StatusCodes.Status404NotFound =>
                "https://httpwg.org/specs/rfc9110.html#section-15.5.5",
            StatusCodes.Status409Conflict =>
                "https://httpwg.org/specs/rfc9110.html#section-15.5.10",
            StatusCodes.Status500InternalServerError =>
                "https://httpwg.org/specs/rfc9110.html#section-15.6.1",
            StatusCodes.Status503ServiceUnavailable =>
                "https://httpwg.org/specs/rfc9110.html#section-15.6.4",
            _ => null
        };
    }
}
