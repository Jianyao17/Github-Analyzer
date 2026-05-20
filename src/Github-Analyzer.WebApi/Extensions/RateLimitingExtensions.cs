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
            .Get<RateLimitConfig>() ?? new RateLimitConfig();

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

            options.AddPolicy(RateLimitPolicies.Write, httpContext =>
            {
                var userKey = GetUserRateLimitKey(httpContext);

				// Allow data write operations based on configured policy options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.Write.PermitLimit,
                        SegmentsPerWindow = rateLimitingConfig.Write.SegmentsPerWindow,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.Write.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitPolicies.Authentication, httpContext =>
            {
                var userKey = GetUserRateLimitKey(httpContext);

				// Allow authentication operations based on configured policy options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.Authentication.PermitLimit,
                        SegmentsPerWindow = rateLimitingConfig.Authentication.SegmentsPerWindow,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.Authentication.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitPolicies.AccountManagement, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

				// Allow account management operations based on configured policy options
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: ipAddress,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.AccountManagement.PermitLimit,
                        SegmentsPerWindow = rateLimitingConfig.AccountManagement.SegmentsPerWindow,
                        Window = TimeSpan.FromSeconds(rateLimitingConfig.AccountManagement.WindowInSeconds),
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

    private static int? GetFallbackRetryAfterSeconds(HttpContext httpContext, RateLimitConfig config)
    {
        var policyName = httpContext.GetEndpoint()
            ?.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            ?.PolicyName;

        return policyName switch
        {
            RateLimitPolicies.Write         => config.Write.WindowInSeconds,
            RateLimitPolicies.Authentication => config.Authentication.WindowInSeconds,
            RateLimitPolicies.AccountManagement => config.AccountManagement.WindowInSeconds,
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
    public const string Write = "Write";
    public const string Authentication = "Authentication";
    public const string AccountManagement = "AccountManagement";
}

