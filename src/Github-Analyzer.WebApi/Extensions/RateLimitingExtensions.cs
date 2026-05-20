using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using GithubAnalyzer.WebApi.Config;

namespace GithubAnalyzer.WebApi.Extensions;

public static class RateLimitingExtensions
{
    public static IHostApplicationBuilder AddApiRateLimiting(this IHostApplicationBuilder builder)
    {
        var rateLimitingConfig = builder.Configuration
            .GetSection("RateLimiting")
            .Get<RateLimitingConfig>() ?? new RateLimitingConfig();

        builder.Services.AddSingleton(rateLimitingConfig);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

				// Construct a ProblemDetails response for rate limit rejections	
                var retryAfterSeconds = TryGetRetryAfterSeconds(context.Lease)
                    ?? GetFallbackRetryAfterSeconds(httpContext, rateLimitingConfig);

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please try again later.",
                };

                if (retryAfterSeconds.HasValue)
                {
                    problemDetails.Extensions["retryAfter"] = retryAfterSeconds.Value;
                    httpContext.Response.Headers.RetryAfter =
                        retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
                }

                var problemDetailsService = httpContext.RequestServices.GetService<IProblemDetailsService>();
                if (problemDetailsService is not null)
                {
                    await problemDetailsService.WriteAsync(
						new ProblemDetailsContext
						{
							HttpContext = httpContext,
							ProblemDetails = problemDetails
						});
                    return;
                }

                await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };

			// Use a sliding window rate limiter partitioned by client IP address
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

				// Allow requests based on configured global options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: ipAddress,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.Global.PermitLimit, 
                        SegmentsPerWindow = rateLimitingConfig.Global.SegmentsPerWindow, 
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.Global.WindowInSeconds), 
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 
                    });
            });

            options.AddPolicy(RateLimitPolicies.CreateProject, httpContext =>
            {
                var userKey = GetUserRateLimitKey(httpContext);

				// Allow project creation based on configured policy options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.CreateProject.PermitLimit,
                        SegmentsPerWindow = rateLimitingConfig.CreateProject.SegmentsPerWindow,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.CreateProject.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
            {
                var userKey = GetUserRateLimitKey(httpContext);

				// Allow logins based on configured policy options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.Login.PermitLimit,
                        SegmentsPerWindow = rateLimitingConfig.Login.SegmentsPerWindow,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.Login.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitPolicies.Register, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

				// Allow registration attempts based on configured policy options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: ipAddress,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.Register.PermitLimit,
                        SegmentsPerWindow = rateLimitingConfig.Register.SegmentsPerWindow,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.Register.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return builder;
    }

    private static int? TryGetRetryAfterSeconds(RateLimitLease lease)
    {
        return lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int?)Math.Ceiling(retryAfter.TotalSeconds)
            : null;
    }

    private static int? GetFallbackRetryAfterSeconds(HttpContext httpContext, RateLimitingConfig config)
    {
        var policyName = httpContext.GetEndpoint()
            ?.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            ?.PolicyName;

        return policyName switch
        {
            RateLimitPolicies.CreateProject => config.CreateProject.WindowInSeconds,
            RateLimitPolicies.Login         => config.Login.WindowInSeconds,
            RateLimitPolicies.Register      => config.Register.WindowInSeconds,
            _ => config.Global.WindowInSeconds
        };
    }

    private static string GetUserRateLimitKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }
}

public static class RateLimitPolicies
{
    public const string CreateProject = "CreateProject";
    public const string Login = "Login";
    public const string Register = "Register";
}

