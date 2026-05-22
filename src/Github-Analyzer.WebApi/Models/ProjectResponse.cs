namespace GithubAnalyzer.WebApi.Models;

public sealed record ProjectResponse(
    Guid Id, 
    string RepositoryName, 
    string RepositoryUrl,
    
    string? BranchName, 
    string? LastCommitHash, 
    DateTime CreatedAtUtc,
    
    bool HasStatistic, 
    bool HasCodeGraph);