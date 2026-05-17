using GithubAnalyzer.WebApi.Models;
using GithubAnalyzer.WebApi.Services;

namespace GithubAnalyzer.WebApi.Tests.Services;

/// <summary>
/// Unit tests for <see cref="QueueProgressNotifier"/>.
/// </summary>
[Collection("Sequential")] // BroadcastsToAllSubscribers uses timing-sensitive CountdownEvent
public sealed class QueueProgressNotifierTests
{
    private readonly QueueProgressNotifier _sut = new();

    private static QueueProgressEvent MakeEvent(
        Guid? projectId = null,
        string jobType  = "Statistic",
        int progress    = 50) =>
        new(projectId ?? Guid.NewGuid(), Guid.NewGuid(), jobType,
            GithubAnalyzer.WebApi.Entities.QueueStatus.Running, progress, "test message");

    // ─────────────────────────────────────────────────────────────────────────
    // Notify with no subscribers
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Should complete without throwing
        await _sut.NotifyAsync(MakeEvent());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Subscribe → Notify → Receive
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_ReceivesPublishedEvent()
    {
        var projectId = Guid.NewGuid();
        const string jobType = "Statistic";
        var evt = MakeEvent(projectId, jobType);

        using var cts = new CancellationTokenSource();

        var receiveTask = Task.Run(async () =>
        {
            await foreach (var item in _sut.SubscribeAsync(projectId, jobType, cts.Token))
            {
                return item;   // return first item
            }
            throw new InvalidOperationException("Stream ended before item received");
        }, cts.Token);

        await Task.Delay(50);
        await _sut.NotifyAsync(evt);
        await Task.Delay(50);
        await cts.CancelAsync();

        var received = await receiveTask;
        Assert.Equal(evt.ProjectId,  received.ProjectId);
        Assert.Equal(evt.JobType,    received.JobType);
        Assert.Equal(evt.Status,     received.Status);
        Assert.Equal(evt.Progress,   received.Progress);
    }

    [Fact]
    public async Task SubscribeAsync_DoesNotReceiveEventsForOtherProjects()
    {
        var targetProjectId = Guid.NewGuid();
        var otherProjectId  = Guid.NewGuid();
        const string jobType = "Statistic";

        using var cts      = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var receivedEvents = new List<QueueProgressEvent>();

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var item in _sut.SubscribeAsync(targetProjectId, jobType, cts.Token))
                receivedEvents.Add(item);
        }, cts.Token);

        await Task.Delay(50);
        await _sut.NotifyAsync(MakeEvent(otherProjectId, jobType));
        await Task.Delay(50);
        await Task.WhenAny(subscribeTask, Task.Delay(400));

        Assert.Empty(receivedEvents);
    }

    [Fact]
    public async Task SubscribeAsync_DoesNotReceiveEventsForOtherJobTypes()
    {
        var projectId = Guid.NewGuid();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var received  = new List<QueueProgressEvent>();

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var item in _sut.SubscribeAsync(projectId, "Statistic", cts.Token))
                received.Add(item);
        }, cts.Token);

        await Task.Delay(50);
        await _sut.NotifyAsync(MakeEvent(projectId, "CodeGraph")); // different jobType
        await Task.WhenAny(subscribeTask, Task.Delay(400));

        Assert.Empty(received);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Multiple subscribers receive same event
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyAsync_BroadcastsToAllSubscribers()
    {
        var projectId = Guid.NewGuid();
        const string jobType = "Statistic";
        const int subscriberCount = 3;

        var receipts = new List<QueueProgressEvent>[subscriberCount];
        for (int i = 0; i < subscriberCount; i++)
            receipts[i] = [];

        // CountdownEvent confirms all subscribers received their event
        var countdown = new CountdownEvent(subscriberCount);
        using var cts = new CancellationTokenSource();

        var tasks = Enumerable.Range(0, subscriberCount)
            .Select(i => Task.Run(async () =>
            {
                await foreach (var item in _sut.SubscribeAsync(projectId, jobType, cts.Token))
                {
                    receipts[i].Add(item);
                    countdown.Signal();
                    break; // take only the first event
                }
            }, cts.Token))
            .ToArray();

        await Task.Delay(100); // let all subscriptions register
        var evt = MakeEvent(projectId, jobType);
        await _sut.NotifyAsync(evt);

        countdown.Wait(TimeSpan.FromSeconds(5));

        await cts.CancelAsync();
        await Task.WhenAll(tasks.Select(t => t.ContinueWith(_ => { })));

        for (int i = 0; i < subscriberCount; i++)
        {
            Assert.Single(receipts[i]);
            Assert.Equal(evt.ProjectId, receipts[i][0].ProjectId);
            Assert.Equal(evt.JobType,   receipts[i][0].JobType);
            Assert.Equal(evt.Status,    receipts[i][0].Status);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Concurrent publish safety
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyAsync_ConcurrentPublishes_DoNotThrow()
    {
        var projectId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var received  = new System.Collections.Concurrent.ConcurrentBag<QueueProgressEvent>();

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var item in _sut.SubscribeAsync(projectId, "Statistic", cts.Token))
                received.Add(item);
        }, cts.Token);

        await Task.Delay(50);

        const int count = 20;
        // All 20 concurrent publishes should complete without exception
        await Task.WhenAll(
            Enumerable.Range(0, count)
                .Select(_ => _sut.NotifyAsync(MakeEvent(projectId, "Statistic"))));

        await Task.Delay(100);
        await cts.CancelAsync();
        await Task.WhenAny(subscribeTask, Task.Delay(200));

        Assert.Equal(count, received.Count);
    }
}
