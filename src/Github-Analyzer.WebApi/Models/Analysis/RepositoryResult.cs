namespace GithubAnalyzer.WebApi.Models.Analysis;

public record RepositoryResult(
    string ExtractPath,
    string RepositoryUrl,
    string RepositoryName,
    
    string? Description,
    string? AuthorName,
    string? BranchName,
    
    string? LastCommitHash,
    DateTime? LastCommitAtUtc
);
