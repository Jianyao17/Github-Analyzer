using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Entities.Repo;

namespace GithubAnalyzer.WebApi.Entities.Analysis;

[Table("StatisticAnalyses", Schema = "Repo")]
public class StatisticAnalysis : BaseEntity
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

    // Structural Statistics
    public int? TotalFolders { get; set; }
    public int? TotalFiles { get; set; }
    public int? SizeInBytes { get; set; }

    // Code Statistics
    public long? TotalLinesOfCode { get; set; }
    public long? CodeLines { get; set; }
    public long? CommentLines { get; set; }
    public long? BlankLines { get; set; }

    // Git Statistics
    public int? TotalCommits { get; set; }
    public int? TotalContributors { get; set; }
    public int? TotalBranches { get; set; }
    
    [Required, MaxLength(50)]
    public string AnalysisVersion { get; set; } = default!;
}
