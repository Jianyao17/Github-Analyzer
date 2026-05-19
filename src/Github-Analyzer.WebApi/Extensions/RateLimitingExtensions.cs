using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace GithubAnalyzer.WebApi.Extensions;

public static class RateLimitingExtensions
{
    public static IHostApplicationBuilder AddApiRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

				// Construct a ProblemDetails response for rate limit rejections	
                var retryAfterSeconds = TryGetRetryAfterSeconds(context.Lease)
                    ?? GetFallbackRetryAfterSeconds(httpContext);
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                    Type = "https://httpwg.org/specs/rfc9110.html#section-15.5.14"
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

				// Allow 60 requests per minute per IP address with a sliding window
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: ipAddress,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 60, 
                        SegmentsPerWindow = 6, 
                        Window = TimeSpan.FromMinutes(1), 
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 
                    });
            });

            options.AddPolicy(RateLimitPolicies.CreateProject, httpContext =>
            {
                var userKey = GetUserRateLimitKey(httpContext);

				// Allow 3 project creation every 30 seconds per user (identified by user ID or IP address)
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        SegmentsPerWindow = 3,
                        Window = TimeSpan.FromSeconds(30),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
            {
                var userKey = GetUserRateLimitKey(httpContext);

				// Allow 5 login attempts per minute per user (identified by user ID or IP address)
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: userKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        SegmentsPerWindow = 6,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(RateLimitPolicies.Register, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

				// Allow 2 registration attempts per minute per IP address
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey: ipAddress,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        SegmentsPerWindow = 6,
                        Window = TimeSpan.FromMinutes(1),
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

    private static int? GetFallbackRetryAfterSeconds(HttpContext httpContext)
    {
        var policyName = httpContext.GetEndpoint()
            ?.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            ?.PolicyName;

        return policyName switch
        {
            RateLimitPolicies.CreateProject => 30,
            RateLimitPolicies.Login => 60,
            RateLimitPolicies.Register => 60,
            _ => 60
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
