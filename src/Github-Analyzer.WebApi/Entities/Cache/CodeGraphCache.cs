using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace GithubAnalyzer.WebApi.Entities.Cache;

[Table("CodeGraphCaches", Schema = "Cache")]
public class CodeGraphCache : BaseEntity
{
    [Required, MaxLength(50)]
    public string LookupKey { get; set; } = default!;

    [Required, MaxLength(200)]
    public string RepoUrl { get; set; } = default!;

    [MaxLength(64)] public string? Branch { get; set; }
    [MaxLength(64)] public string? CommitHash { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }

    [Required, Column(TypeName = "jsonb")]
    public JsonDocument GraphJson { get; set; } = default!;

    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
}
