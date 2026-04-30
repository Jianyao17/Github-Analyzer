using System.ComponentModel.DataAnnotations;

namespace GithubAnalyzer.WebApi.Database;

public sealed class AnalysisJobEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string JobId { get; set; }
    
    public required string RepoUrl { get; set; }
    
    public string Status { get; set; } = "Queued";
    
    public int Progress { get; set; }
    
    public string? ResultJson { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
}
