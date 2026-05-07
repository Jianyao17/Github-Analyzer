using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Entities.Auth;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    [Required]
    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty;
}
