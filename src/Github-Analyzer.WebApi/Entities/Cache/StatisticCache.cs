using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GithubAnalyzer.WebApi.Entities.Cache;

[Table("StatisticCaches", Schema = "Cache")]
public class StatisticCache : BaseEntity
{
    [Required, MaxLength(50)]
    public string LookupKey { get; set; } = default!;

    [Required, MaxLength(200)]
    public string RepoUrl { get; set; } = default!;

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
}
