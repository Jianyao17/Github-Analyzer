using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Services;

public class QueueProgressNotifier : IQueueProgressNotifier
{
    // Key: $"{projectId}_{jobType}", Value: Dictionary of subscriber channels
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<QueueProgressEvent>>> _subscribers = new();

    public async Task NotifyAsync(QueueProgressEvent progressEvent)
    {
        string topicKey = GetTopicKey(progressEvent.ProjectId, progressEvent.JobType);
        if (_subscribers.TryGetValue(topicKey, out var channels))
        {
            foreach (var channel in channels.Values)
            {
                // We use TryWrite to avoid blocking if the channel is somehow full (though it's unbounded)
                // and to prevent one slow subscriber from affecting the worker.
                channel.Writer.TryWrite(progressEvent);
            }
        }
        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<QueueProgressEvent> SubscribeAsync(
        Guid projectId, string jobType, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string topicKey = GetTopicKey(projectId, jobType);
        var subscriberId = Guid.NewGuid();
        
        // Use unbounded channel for each subscriber
        var channel = Channel.CreateUnbounded<QueueProgressEvent>();

        // Add the subscriber's channel to the topic's subscriber list
        var channels = _subscribers.GetOrAdd(topicKey, 
            _ => new ConcurrentDictionary<Guid, Channel<QueueProgressEvent>>());
        
        channels.TryAdd(subscriberId, channel);
        try
        {
            // Read from the channel until cancellation is requested
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                yield return item;
        }
        finally
        {
            channels.TryRemove(subscriberId, out _);
            if (channels.IsEmpty) {
                _subscribers.TryRemove(topicKey, out _);
            }
        }
    }

    private static string GetTopicKey(Guid projectId, string jobType) 
        => $"{projectId}_{jobType.ToLowerInvariant()}";
}
