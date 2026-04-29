using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Database;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
