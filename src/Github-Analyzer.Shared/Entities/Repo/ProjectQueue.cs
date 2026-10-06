using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using GithubAnalyzer.Shared.Enums;
using GithubAnalyzer.Shared.Jobs;

namespace GithubAnalyzer.Shared.Entities;

[Table("ProjectQueues", Schema = "Repo")]
public class ProjectQueue : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = default!;

    [Column(TypeName = "jsonb")]
    public AnalysisOptions Options { get; set; } = new();

    [MaxLength(30)]
    public string? RedisStreamMessageId { get; set; }

    [Required]
    public JobQueueStatus Status { get; set; } = JobQueueStatus.Pending;

    [Required, Range(1, 100)]
    public int Priority { get; set; } = 10;

    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;

    [MaxLength(500)]
    public string? LastError { get; set; }
}