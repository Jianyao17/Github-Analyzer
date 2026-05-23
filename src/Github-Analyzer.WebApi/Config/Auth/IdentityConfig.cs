using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Config;

public sealed class IdentityConfig : IdentityOptions
{
    public IdentityConfig()
    {
        Password.RequireDigit = true;
        Password.RequireLowercase = true;
        Password.RequireUppercase = false;
        Password.RequireNonAlphanumeric = false;
        Password.RequiredLength = 8;
        Password.RequiredUniqueChars = 1;
        User.RequireUniqueEmail = true;
    }
}
