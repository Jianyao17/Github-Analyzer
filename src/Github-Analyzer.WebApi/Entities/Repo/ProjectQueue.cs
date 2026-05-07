using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GithubAnalyzer.WebApi.Entities.Repo;

[Table("ProjectQueues", Schema = "Repo")]
public class ProjectQueue : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = default!;

    [Required]
    public string JobType { get; set; } = string.Empty;

    [Required]
    public QueueStatus Status { get; set; } = QueueStatus.Pending;
    public int Progress { get; set; } = 0;

    [Required, Range(1, 100)]
    public int Priority { get; set; } = 10;

    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;

    public string? PayloadJson { get; set; }
    public string? LastError { get; set; }
}