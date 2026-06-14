using GithubAnalyzer.WebApi.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GithubAnalyzer.WebApi.Extensions;

public static class OutputCacheExtensions
{
    public const string UserCachePolicyName = "UserCache";

    public static IServiceCollection AddProjectOutputCache(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            // Add custom cache policy for users
            options.AddPolicy(UserCachePolicyName, new UserSpecificCachePolicy());
        });
        
        return services;
    }

    public static TBuilder RequireUserCache<TBuilder>(this TBuilder builder) 
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.CacheOutput(UserCachePolicyName);
    }
}
