using System.ComponentModel.DataAnnotations;

namespace GithubAnalyzer.WebApi.Entities;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateTime  CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    
    [Required] 
    public bool IsDeleted { get; set; } = false;
}