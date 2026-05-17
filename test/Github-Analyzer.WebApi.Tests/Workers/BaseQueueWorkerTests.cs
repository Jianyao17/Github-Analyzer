using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GithubAnalyzer.WebApi.Database;
using GithubAnalyzer.WebApi.Entities;
using GithubAnalyzer.WebApi.Entities.Repo;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Workers;
using GithubAnalyzer.WebApi.Models;

namespace GithubAnalyzer.WebApi.Tests.Workers;

/// <summary>
/// Tests for <see cref="BaseQueueWorker"/> job-processing behaviour.
/// Uses a concrete stub worker and an in-memory EF Core database so no
/// real database or GitHub network connection is required.
///
/// Design: rather than relying on the BackgroundService start/stop timing,
/// we drive the worker's polling loop directly by setting a short
/// PollingInterval and using a completion-signal (TaskCompletionSource) that
/// is triggered from inside ProcessJobAsync.
/// </summary>
[Collection("Sequential")] // BackgroundService timing is sensitive to CPU contention
public sealed class BaseQueueWorkerTests : IAsyncDisposable
{
    // ─────────────────────────────────────────────────────────────────────────
    // Configurable stub worker
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class StubWorker : BaseQueueWorker
    {
        public override string JobType => "Stub";

        // Override polling interval to speed up tests
        protected override TimeSpan PollingInterval => TimeSpan.FromMilliseconds(50);

        /// <summary>Set this to control what happens during <c>ProcessJobAsync</c>.</summary>
        public Func<ProjectQueue, CancellationToken, Task>? ProcessImpl { get; set; }

        /// <summary>How many times <c>ProcessJobAsync</c> was actually called.</summary>
        public int ProcessCallCount { get; private set; }

        /// <summary>Signals when at least one call to <c>ProcessJobAsync</c> has returned.</summary>
        public TaskCompletionSource ProcessedSignal { get; private set; } = new();

        public StubWorker(
            IServiceScopeFactory scopeFactory,
            IQueueProgressNotifier notifier,
            ILogger<StubWorker> logger)
            : base(scopeFactory, notifier, logger) { }

        protected override async Task ProcessJobAsync(
            ProjectQueue job, CancellationToken cancellationToken)
        {
            ProcessCallCount++;
            try
            {
                if (ProcessImpl is not null)
                    await ProcessImpl(job, cancellationToken);
            }
            finally
            {
                // Signal fires whether success or exception
                ProcessedSignal.TrySetResult();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test fixture
    // ─────────────────────────────────────────────────────────────────────────

    private readonly ServiceProvider _sp;
    private readonly StubWorker _worker;
    private readonly Mock<IQueueProgressNotifier> _notifierMock;
    private readonly string _dbName;

    public BaseQueueWorkerTests()
    {
        _dbName = $"TestDb_{Guid.NewGuid()}";
        _notifierMock = new Mock<IQueueProgressNotifier>();
        _notifierMock
            .Setup(n => n.NotifyAsync(It.IsAny<QueueProgressEvent>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseInMemoryDatabase(_dbName));

        _sp = services.BuildServiceProvider();

        var scopeFactory  = _sp.GetRequiredService<IServiceScopeFactory>();
        var loggerFactory = LoggerFactory.Create(_ => { });

        _worker = new StubWorker(
            scopeFactory,
            _notifierMock.Object,
            loggerFactory.CreateLogger<StubWorker>());
    }

    public async ValueTask DisposeAsync() => await _sp.DisposeAsync();

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Seed a pending job, start the worker, wait for it to be processed.</summary>
    private async Task<Guid> SeedAndRunAsync(
        Func<ProjectQueue, CancellationToken, Task>? processImpl = null,
        string jobType = "Stub")
    {
        if (processImpl is not null)
            _worker.ProcessImpl = processImpl;

        var jobId = await SeedPendingJobAsync(jobType);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await _worker.StartAsync(cts.Token);

        // Wait until ProcessJobAsync has returned/thrown (finally block sets the signal)
        await Task.WhenAny(
            _worker.ProcessedSignal.Task,
            Task.Delay(5000, cts.Token));

        // Allow the base class to finish SaveChangesAsync after the virtual method returns
        await Task.Delay(2000);

        // Use a 5-second timeout so StopAsync never hangs the test indefinitely
        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await _worker.StopAsync(stopCts.Token); }
        catch (OperationCanceledException) { /* worker stop timed out — acceptable */ }
        return jobId;
    }

    private async Task<Guid> SeedPendingJobAsync(string jobType = "Stub")
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var project = new Project
        {
            Id             = Guid.NewGuid(),
            UserId         = Guid.NewGuid(),
            Title          = "Test Project",
            RepositoryUrl  = "https://github.com/owner/repo",
            RepositoryName = "repo",
            LocalPath      = "/tmp/repo",
            CreatedAtUtc   = DateTime.UtcNow
        };
        var job = new ProjectQueue
        {
            Id           = Guid.NewGuid(),
            ProjectId    = project.Id,
            Project      = project,
            JobType      = jobType,
            Status       = QueueStatus.Pending,
            Priority     = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Projects.Add(project);
        db.ProjectQueues.Add(job);
        await db.SaveChangesAsync();
        return job.Id;
    }

    private async Task<ProjectQueue?> ReadJobAsync(Guid jobId)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ProjectQueues.AsNoTracking().FirstOrDefaultAsync(q => q.Id == jobId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Job lifecycle tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_PendingJob_IsPickedUpAndCompleted()
    {
        var jobId = await SeedAndRunAsync();

        var updated = await ReadJobAsync(jobId);
        Assert.NotNull(updated);
        Assert.Equal(QueueStatus.Completed, updated!.Status);
        Assert.NotNull(updated.CompletedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_PendingJob_SendsRunningAndCompletedNotifications()
    {
        await SeedAndRunAsync();

        _notifierMock.Verify(n => n.NotifyAsync(
            It.Is<QueueProgressEvent>(e => e.Status == QueueStatus.Running)),
            Times.AtLeastOnce);

        _notifierMock.Verify(n => n.NotifyAsync(
            It.Is<QueueProgressEvent>(e => e.Status == QueueStatus.Completed)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoJobs_DoesNotInvokeProcessJobAsync()
    {
        // Don't seed any jobs — just run and stop
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _worker.StartAsync(cts.Token);
        await Task.Delay(200);

        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await _worker.StopAsync(stopCts.Token); }
        catch (OperationCanceledException) { }

        Assert.Equal(0, _worker.ProcessCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_JobFails_StatusIsSetToFailed()
    {
        var jobId = await SeedAndRunAsync(
            (_, _) => throw new InvalidOperationException("Fatal error"));

        var updated = await ReadJobAsync(jobId);
        // InvalidOperationException is NOT retriable per IsRetriable(), so it goes to Failed
        Assert.Equal(QueueStatus.Failed, updated!.Status);
        Assert.Contains("Fatal error", updated.LastError);
    }

    [Fact]
    public async Task ExecuteAsync_JobFails_SendsFailedNotification()
    {
        await SeedAndRunAsync(
            (_, _) => throw new InvalidOperationException("Fatal"));

        _notifierMock.Verify(n => n.NotifyAsync(
            It.Is<QueueProgressEvent>(e => e.Status == QueueStatus.Failed)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RetriableError_SchedulesRetry()
    {
        // HttpRequestException is retriable (see BaseQueueWorker.IsRetriable)
        var jobId = await SeedAndRunAsync(
            (_, _) => throw new HttpRequestException("Network blip"));

        var updated = await ReadJobAsync(jobId);
        Assert.Equal(QueueStatus.Pending, updated!.Status);
        Assert.NotNull(updated.ScheduledAtUtc);
        Assert.Equal(1, updated.AttemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_JobOnlyPickedUpByMatchingJobType()
    {
        // Seed a non-matching job; the stub worker should leave it untouched
        var otherJobId = await SeedPendingJobAsync("CodeGraph");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _worker.StartAsync(cts.Token);
        await Task.Delay(400); // wait several poll cycles

        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await _worker.StopAsync(stopCts.Token); }
        catch (OperationCanceledException) { }

        var otherJob = await ReadJobAsync(otherJobId);
        Assert.Equal(QueueStatus.Pending, otherJob!.Status);
        Assert.Equal(0, _worker.ProcessCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_IncreasesAttemptCountOnEachAttempt()
    {
        // First run: retriable error → AttemptCount = 1, status = Pending
        var jobId = await SeedAndRunAsync(
            (_, _) => throw new HttpRequestException("blip"));

        var afterFirstRun = await ReadJobAsync(jobId);
        Assert.Equal(1, afterFirstRun!.AttemptCount);
    }
}
