namespace GithubAnalyzer.Shared.Jobs;

public class AnalysisJobMessage
{
    public Guid JobId { get; set; }       // = ProjectQueue.Id (correlation + idempotency key)
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    
    // Repository info — dicopy dari Project entity agar Worker tidak perlu query DB tambahan
    public string RepositoryUrl { get; set; } = default!;
    public string RepositoryName { get; set; } = default!;
    public string Branch { get; set; } = "main";
    public string? CommitHash { get; set; }
    
    // Analysis options — menentukan step mana yang dijalankan
    public AnalysisOptions Options { get; set; } = new();
    
    // Version string per analysis type — digunakan untuk cache lookup key
    public string StatisticsVersion { get; set; } = "dev";
    public string CodeGraphVersion { get; set; } = "dev";
}
