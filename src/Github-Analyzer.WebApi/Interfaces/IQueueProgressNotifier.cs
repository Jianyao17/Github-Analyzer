using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Interfaces;

public interface IQueueProgressNotifier
{
    Task NotifyAsync(QueueProgressEvent progressEvent);
    IAsyncEnumerable<QueueProgressEvent> SubscribeAsync(Guid projectId, string jobType, CancellationToken cancellationToken);
}
