using GithubAnalyzer.WebApi.Services;

namespace GithubAnalyzer.WebApi.Tests.Workers;

/// <summary>
/// Tests for <see cref="RepoDownloadGate"/> focusing on:
/// <list type="bullet">
///   <item>Single-download guarantee (download func invoked exactly once per project)</item>
///   <item>Race condition: many concurrent callers for the same project</item>
///   <item>Isolation: separate projects get independent downloads</item>
///   <item>Gate cleanup after completion (allows re-download on next call)</item>
///   <item>Cancellation propagation</item>
///   <item>Error propagation to all waiters</item>
/// </list>
/// </summary>
public sealed class RepoDownloadGateTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Basic happy-path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_BasicDownload_ReturnsCorrectPath()
    {
        var gate      = new RepoDownloadGate();
        var projectId = Guid.NewGuid();

        var result = await gate.EnsureRepoAsync(
            projectId,
            _ => Task.FromResult("/repos/my-repo"),
            CancellationToken.None);

        Assert.Equal("/repos/my-repo", result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Race condition: concurrent callers for the SAME project
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_ConcurrentCallsForSameProject_InvokesDownloadFuncOnlyOnce()
    {
        var gate         = new RepoDownloadGate();
        var projectId    = Guid.NewGuid();
        int downloadCount = 0;

        // Simulated download takes 200 ms
        async Task<string> SlowDownload(CancellationToken _)
        {
            Interlocked.Increment(ref downloadCount);
            await Task.Delay(200);
            return "/repos/shared";
        }

        const int concurrency = 10;
        var barrier = new Barrier(concurrency); // all start at the same time

        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await gate.EnsureRepoAsync(projectId, SlowDownload, CancellationToken.None);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Download function must be invoked exactly once
        Assert.Equal(1, downloadCount);

        // All callers must receive the same path
        Assert.All(results, r => Assert.Equal("/repos/shared", r));
    }

    [Fact]
    public async Task EnsureRepoAsync_HighConcurrency_StillInvokesDownloadOnlyOnce()
    {
        var gate      = new RepoDownloadGate();
        var projectId = Guid.NewGuid();
        int downloadCount = 0;

        // Use a real async delay so the Lazy<Task> is established before callers race.
        // With Task.FromResult the factory completes synchronously before being stored,
        // so each caller sees an already-removed Lazy and creates a new one.
        async Task<string> CountingDownload(CancellationToken _)
        {
            Interlocked.Increment(ref downloadCount);
            await Task.Delay(50); // ensure async so all concurrent callers share one Lazy
            return "/repos/high";
        }

        // 50 concurrent callers — all should wait on the same download
        var barrier = new Barrier(50);
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await gate.EnsureRepoAsync(projectId, CountingDownload, CancellationToken.None);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, downloadCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Isolation: different projects are downloaded independently
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_DifferentProjects_EachDownloadedOnce()
    {
        var gate   = new RepoDownloadGate();
        var counts = new System.Collections.Concurrent.ConcurrentDictionary<Guid, int>();

        async Task<string> TrackingDownload(Guid id, CancellationToken _)
        {
            counts.AddOrUpdate(id, 1, (_, c) => c + 1);
            await Task.Delay(50);
            return $"/repos/{id}";
        }

        const int projectCount = 5;
        var projectIds = Enumerable.Range(0, projectCount).Select(_ => Guid.NewGuid()).ToArray();

        // Each project is requested 3 times concurrently
        var allTasks = projectIds
            .SelectMany(pid => Enumerable.Range(0, 3).Select(
                _ => gate.EnsureRepoAsync(pid, ct => TrackingDownload(pid, ct), CancellationToken.None)))
            .ToArray();

        var results = await Task.WhenAll(allTasks);

        // Each project: exactly 1 download
        foreach (var pid in projectIds)
            Assert.Equal(1, counts[pid]);

        // Each project returns its own path
        foreach (var pid in projectIds)
        {
            var expected = $"/repos/{pid}";
            Assert.Equal(3, results.Count(r => r == expected));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gate cleanup: second call after first completes triggers a new download
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_AfterFirstDownloadCompletes_AllowsSecondDownload()
    {
        var gate      = new RepoDownloadGate();
        var projectId = Guid.NewGuid();
        int count     = 0;

        Task<string> Downloader(CancellationToken _)
        {
            Interlocked.Increment(ref count);
            return Task.FromResult($"/repos/attempt-{count}");
        }

        var first  = await gate.EnsureRepoAsync(projectId, Downloader, CancellationToken.None);
        var second = await gate.EnsureRepoAsync(projectId, Downloader, CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal("/repos/attempt-1", first);
        Assert.Equal("/repos/attempt-2", second);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Error propagation: all concurrent waiters receive the exception
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_DownloadFails_AllWaitersPropagateException()
    {
        var gate      = new RepoDownloadGate();
        var projectId = Guid.NewGuid();

        async Task<string> FailingDownload(CancellationToken _)
        {
            await Task.Delay(50);
            throw new HttpRequestException("Simulated network failure");
        }

        const int concurrency = 5;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => gate.EnsureRepoAsync(projectId, FailingDownload, CancellationToken.None))
            .ToArray();

        var exceptions = new List<Exception>();
        foreach (var t in tasks)
        {
            try { await t; }
            catch (Exception ex) { exceptions.Add(ex); }
        }

        Assert.Equal(concurrency, exceptions.Count);
        Assert.All(exceptions, ex => Assert.IsType<HttpRequestException>(ex));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cancellation: waitToken cancellation propagates
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_WaitTokenCancelled_ThrowsOperationCancelled()
    {
        var gate      = new RepoDownloadGate();
        var projectId = Guid.NewGuid();

        async Task<string> VerySlowDownload(CancellationToken _)
        {
            await Task.Delay(TimeSpan.FromSeconds(30)); // intentionally long
            return "/repos/slow";
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // TaskCanceledException is a subclass of OperationCanceledException — both are valid
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => gate.EnsureRepoAsync(projectId, VerySlowDownload, cts.Token));
        Assert.NotNull(ex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Results are consistent across all concurrent callers
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureRepoAsync_ConcurrentCallers_AllReceiveIdenticalResult()
    {
        var gate      = new RepoDownloadGate();
        var projectId = Guid.NewGuid();
        var expected  = $"/repos/{Guid.NewGuid()}";

        Task<string> Download(CancellationToken _) => Task.FromResult(expected);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => gate.EnsureRepoAsync(projectId, Download, CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expected, r));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stress test: many projects, many callers, no deadlock
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(Timeout = 10_000)] // 10-second safety net
    public async Task EnsureRepoAsync_StressTest_NoDeadlockOrDataCorruption()
    {
        var gate = new RepoDownloadGate();
        const int projects    = 20;
        const int callersEach = 15;

        var projectIds = Enumerable.Range(0, projects)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var downloadCounts = new System.Collections.Concurrent.ConcurrentDictionary<Guid, int>();

        async Task<string> Downloader(Guid pid, CancellationToken _)
        {
            downloadCounts.AddOrUpdate(pid, 1, (_, c) => c + 1);
            await Task.Delay(10);
            return $"/repos/{pid}";
        }

        var allTasks = projectIds
            .SelectMany(pid => Enumerable.Range(0, callersEach)
                .Select(_ => gate.EnsureRepoAsync(pid, ct => Downloader(pid, ct), CancellationToken.None)))
            .ToList();

        var results = await Task.WhenAll(allTasks);

        // 1. No project downloaded more than once
        foreach (var (pid, cnt) in downloadCounts)
            Assert.Equal(1, cnt);

        // 2. Total results match expected count
        Assert.Equal(projects * callersEach, results.Length);

        // 3. Each result belongs to a known project
        Assert.All(results, r =>
            Assert.Contains(projectIds, pid => r == $"/repos/{pid}"));
    }
}
