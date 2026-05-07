using GithubAnalyzer.WebApi.Config;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Extensions;

public static class IdentityConfigExtensions
{
    public static Action<IdentityOptions> LoadIdentityConfig(this IHostApplicationBuilder builder)
    {
        var identityConfig = builder.Configuration
            .GetSection("Identity")
            .Get<IdentityConfig>() ?? new IdentityConfig();

        return options =>
        {
            options.User = identityConfig.User;
            options.Password = identityConfig.Password;
            options.ClaimsIdentity = identityConfig.ClaimsIdentity;
            options.Lockout = identityConfig.Lockout;
            options.SignIn = identityConfig.SignIn;
            options.Tokens = identityConfig.Tokens;
            options.Stores = identityConfig.Stores;
        };
    }
}
