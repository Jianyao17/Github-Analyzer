using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Infrastructure.Authentication;

public sealed class PasswordOptionsSettings
{
    public const string SectionName = "Identity:Password";

    public bool RequireDigit { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireUppercase { get; set; }

    public bool RequireNonAlphanumeric { get; set; }

    public int RequiredLength { get; set; } = 8;

    public int RequiredUniqueChars { get; set; } = 1;

    public static void Apply(IdentityOptions options, PasswordOptionsSettings settings)
    {
        options.Password.RequireDigit = settings.RequireDigit;
        options.Password.RequireLowercase = settings.RequireLowercase;
        options.Password.RequireUppercase = settings.RequireUppercase;
        options.Password.RequireNonAlphanumeric = settings.RequireNonAlphanumeric;
        options.Password.RequiredLength = settings.RequiredLength;
        options.Password.RequiredUniqueChars = settings.RequiredUniqueChars;
    }
}
