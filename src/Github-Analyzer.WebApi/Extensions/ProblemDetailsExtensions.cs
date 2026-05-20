using Microsoft.AspNetCore.Http.Features;

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
                var httpContext = context.HttpContext;

                // Include request path and method in the problem details instance
                var path = httpContext.Request.Path.Value ?? string.Empty;
                var method = httpContext.Request.Method ?? string.Empty;
                
                // Include request ID and trace ID for better correlation in logs
                var requestId = httpContext.TraceIdentifier ?? string.Empty;
                var traceId = httpContext.Features.Get<IHttpActivityFeature>()
                    ?.Activity?.Id ?? string.Empty;

                context.ProblemDetails.Instance = $"{method} {path}";
                context.ProblemDetails.Extensions["requestId"] = requestId;
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

}
