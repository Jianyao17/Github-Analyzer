using GithubAnalyzer.WebApi.Entities;

namespace GithubAnalyzer.WebApi.Models;

public record QueueProgressEvent(
  Guid ProjectId, Guid QueueId, string JobType, 
  QueueStatus Status, int Progress, string? Message = null);
