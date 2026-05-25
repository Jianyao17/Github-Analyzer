using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Entities.Repo;

namespace GithubAnalyzer.WebApi.Entities.Analysis;

[Table("CodeGraphAnalyses", Schema = "Repo")]
public class CodeGraphAnalysis : BaseEntity
{
    [Required] public Guid UserId { get; set; }
    [Required] public Guid ProjectId { get; set; }
    
    [ForeignKey(nameof(UserId))] 
    public ApplicationUser User { get; set; } = default!;
    
    [ForeignKey(nameof(ProjectId))] 
    public Project Project { get; set; } = default!;
    
    [MaxLength(64)] public string? Branch { get; set; }
    [MaxLength(64)] public string? CommitHash { get; set; }
    
    public DateTime? GeneratedAtUtc { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public JsonDocument GraphJson { get; set; } = default!;
    
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
}