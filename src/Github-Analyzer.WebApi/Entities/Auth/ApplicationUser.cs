using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Entities.Auth;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(200)]
    public string AvatarUrl { get; set; } = string.Empty;
}
