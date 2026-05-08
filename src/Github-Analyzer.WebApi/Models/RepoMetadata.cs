namespace GithubAnalyzer.WebApi.Models;


public record RepoBranch(string Name, string CommitHash);

public record RepoCommit(string Hash, string Message, string Author, DateTimeOffset Date);