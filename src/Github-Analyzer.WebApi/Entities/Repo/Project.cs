using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GithubAnalyzer.WebApi.Entities.Analysis;

namespace GithubAnalyzer.WebApi.Entities.Repo;

[Table("Projects", Schema = "Repo")]
public class Project : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(50)]
    public string Title { get; set; } = default!;
    
    [Required, MaxLength(200)]
    public string RepositoryUrl { get; set; } = default!;

    [Required, MaxLength(50)]
    public string RepositoryName { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string LocalPath { get; set; } = default!;

    [MaxLength(200)] public string? Description { get; set; }
    [MaxLength(50)]  public string? AuthorName  { get; set; }
    [MaxLength(50)]  public string? BranchName  { get; set; }
    
    [MaxLength(50)] 
    public string?   LastCommitHash  { get; set; }
    public DateTime? LastCommitAtUtc { get; set; }
    
    public ICollection<ProjectQueue> Queues { get; set; } = new List<ProjectQueue>();
    
    public ICollection<StatisticAnalysis> Statistics { get; set; } = new List<StatisticAnalysis>();
    public ICollection<CodeGraphAnalysis> CodeGraphs { get; set; } = new List<CodeGraphAnalysis>();
}
