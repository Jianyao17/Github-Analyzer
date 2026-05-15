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
                context.ProblemDetails.Extensions["requestId"] = httpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["path"] = httpContext.Request.Path.Value;
                context.ProblemDetails.Extensions["method"] = httpContext.Request.Method;

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
