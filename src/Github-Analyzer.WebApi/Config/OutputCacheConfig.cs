using Microsoft.AspNetCore.OutputCaching;
using System.Security.Claims;

namespace GithubAnalyzer.WebApi.Config;

/// <summary>
/// Konfigurasi Output Cache Redis yang dapat diatur melalui appsettings.
/// </summary>
public class OutputCacheSettings
{
    public const string SectionName = "OutputCache";

    public int ConnectTimeoutMs { get; init; } = 3000;

    public int SyncTimeoutMs { get; init; } = 1000;
}

public class UserSpecificCachePolicy : IOutputCachePolicy
{
    public const string UserTagPrefix = "user-";
    public const string UserIdVaryKey = "UserId";
    public const int CacheDurationMinutes = 5;

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // Only cache GET or HEAD requests
        var attemptOutputCaching = context.HttpContext.Request.Method == HttpMethods.Get || 
                                   context.HttpContext.Request.Method == HttpMethods.Head;
        
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Get user ID
        var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                        context.HttpContext.User.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(userIdStr))
        {
            // Tag with user specific ID for targeted eviction
            context.Tags.Add($"{UserTagPrefix}{userIdStr}");
            
            // Vary cache by user ID to isolate cache per user
            context.CacheVaryByRules.VaryByValues.Add(UserIdVaryKey, userIdStr);
        }

        // Apply duration
        context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(CacheDurationMinutes);

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) 
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) 
        => ValueTask.CompletedTask;
}
