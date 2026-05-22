using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GithubAnalyzer.WebApi.Entities.Auth;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty; 
    
    [MaxLength(256)]
    public string? AvatarUrl { get; set; } = string.Empty;

    [Required]
    public DateTime  CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
