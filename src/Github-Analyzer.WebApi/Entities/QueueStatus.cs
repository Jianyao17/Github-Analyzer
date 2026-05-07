namespace GithubAnalyzer.WebApi.Entities;

public enum QueueStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5
}