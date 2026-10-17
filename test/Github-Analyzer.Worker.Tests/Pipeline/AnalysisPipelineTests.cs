using GithubAnalyzer.Shared.Jobs;
using GithubAnalyzer.Worker.Database;
using GithubAnalyzer.Worker.Interfaces;
using GithubAnalyzer.Worker.Pipeline;
using GithubAnalyzer.Worker.Services;
using GithubAnalyzer.Worker.Tests.Pipeline.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace GithubAnalyzer.Worker.Tests.Pipeline;

/// <summary>
/// Test suite untuk AnalysisPipeline: orchestrasi step, manajemen status DB, dan penanganan exception.
/// </summary>
public sealed class AnalysisPipelineTests
{
    private readonly Mock<IProjectQueueRepository> _mockQueue = new();
    private readonly Mock<IDbAnalysisCacheService> _mockCache = new();
    private readonly CapturingProgressPublisher _publisher = new();

    // ─── Step Orchestration ──────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WithNoSteps_CompletesSuccessfully()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob();
        SetupJobSuccessFlow(job.JobId);

        var pipeline = CreatePipeline(steps: []);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert
        _mockQueue.Verify(q => q.MarkJobRunningAsync(job.JobId, It.IsAny<CancellationToken>()), Times.Once);
        _mockQueue.Verify(q => q.MarkJobCompletedAsync(job.JobId, It.IsAny<CancellationToken>()), Times.Once);
        _mockQueue.Verify(q => q.MarkJobFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithOneActiveStep_ExecutesThatStep()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob(statistics: true, codeGraph: false);
        SetupJobSuccessFlow(job.JobId);

        var mockStep = CreateStep(shouldExecute: true);
        var pipeline = CreatePipeline([mockStep.Object]);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert
        mockStep.Verify(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepShouldNotExecute_SkipsThatStep()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob(statistics: false, codeGraph: false);
        SetupJobSuccessFlow(job.JobId);

        var mockStep = CreateStep(shouldExecute: false);
        var pipeline = CreatePipeline([mockStep.Object]);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert: ShouldExecute dipanggil, tapi ExecuteAsync tidak
        mockStep.Verify(s => s.ShouldExecute(It.IsAny<PipelineContext>()), Times.Once);
        mockStep.Verify(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleSteps_ExecutesInRegistrationOrder()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob(statistics: true, codeGraph: true);
        SetupJobSuccessFlow(job.JobId);

        var executionOrder = new List<int>();
        var step1 = CreateOrderedStep(executionOrder, id: 1, shouldExecute: true);
        var step2 = CreateOrderedStep(executionOrder, id: 2, shouldExecute: true);
        var step3 = CreateOrderedStep(executionOrder, id: 3, shouldExecute: true);

        var pipeline = CreatePipeline([step1, step2, step3]);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert
        Assert.Equal([1, 2, 3], executionOrder);
    }

    [Fact]
    public async Task ExecuteAsync_MixOfActiveAndSkippedSteps_OnlyActiveStepsExecute()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob(statistics: true, codeGraph: false);
        SetupJobSuccessFlow(job.JobId);

        var executionOrder = new List<int>();
        var step1 = CreateOrderedStep(executionOrder, id: 1, shouldExecute: true);
        var step2 = CreateOrderedStep(executionOrder, id: 2, shouldExecute: false);
        var step3 = CreateOrderedStep(executionOrder, id: 3, shouldExecute: true);

        var pipeline = CreatePipeline([step1, step2, step3]);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert: step 2 tidak dieksekusi
        Assert.Equal([1, 3], executionOrder);
    }

    // ─── DB Status Management ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Always_MarksJobRunningBeforeSteps()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob();
        var runningCalledBefore = false;
        var stepExecuted = false;

        _mockQueue
            .Setup(q => q.MarkJobRunningAsync(job.JobId, It.IsAny<CancellationToken>()))
            .Callback(() => runningCalledBefore = !stepExecuted)
            .Returns(Task.CompletedTask);
        _mockQueue
            .Setup(q => q.MarkJobCompletedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new Mock<IAnalysisStep>();
        step.Setup(s => s.ShouldExecute(It.IsAny<PipelineContext>())).Returns(true);
        step.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => stepExecuted = true)
            .Returns(Task.CompletedTask);

        var pipeline = CreatePipeline([step.Object]);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert: MarkJobRunning dipanggil sebelum step mulai berjalan
        Assert.True(runningCalledBefore);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_MarksJobCompleted()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob();
        SetupJobSuccessFlow(job.JobId);

        var pipeline = CreatePipeline([]);

        // Act
        await pipeline.ExecuteAsync(job);

        // Assert
        _mockQueue.Verify(q => q.MarkJobCompletedAsync(job.JobId, It.IsAny<CancellationToken>()), Times.Once);
        _mockQueue.Verify(q => q.MarkJobFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepThrows_MarksJobFailedAndRethrows()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob();
        var errorMessage = "Simulated analysis failure";

        _mockQueue.Setup(q => q.MarkJobRunningAsync(job.JobId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockQueue.Setup(q => q.MarkJobFailedAsync(job.JobId, errorMessage, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var failingStep = new Mock<IAnalysisStep>();
        failingStep.Setup(s => s.ShouldExecute(It.IsAny<PipelineContext>())).Returns(true);
        failingStep.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        var pipeline = CreatePipeline([failingStep.Object]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ExecuteAsync(job));

        Assert.Equal(errorMessage, ex.Message);
        _mockQueue.Verify(q => q.MarkJobFailedAsync(job.JobId, errorMessage, It.IsAny<CancellationToken>()), Times.Once);
        _mockQueue.Verify(q => q.MarkJobCompletedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepThrows_DoesNotExecuteSubsequentSteps()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob();
        _mockQueue.Setup(q => q.MarkJobRunningAsync(job.JobId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockQueue.Setup(q => q.MarkJobFailedAsync(job.JobId, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var failingStep = CreateStep(shouldExecute: true, throws: new Exception("Step 1 failed"));
        var nextStep = CreateStep(shouldExecute: true);
        var pipeline = CreatePipeline([failingStep.Object, nextStep.Object]);

        // Act
        await Assert.ThrowsAsync<Exception>(() => pipeline.ExecuteAsync(job));

        // Assert: segundo step nunca é executado
        nextStep.Verify(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        var job = PipelineTestHelpers.CreateJob();
        using var cts = new CancellationTokenSource();

        _mockQueue.Setup(q => q.MarkJobRunningAsync(job.JobId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockQueue.Setup(q => q.MarkJobFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var step = new Mock<IAnalysisStep>();
        step.Setup(s => s.ShouldExecute(It.IsAny<PipelineContext>())).Returns(true);
        step.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .Returns<PipelineContext, CancellationToken>((_, ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        var pipeline = CreatePipeline([step.Object]);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pipeline.ExecuteAsync(job, cts.Token));
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private void SetupJobSuccessFlow(Guid jobId)
    {
        _mockQueue.Setup(q => q.MarkJobRunningAsync(jobId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockQueue.Setup(q => q.MarkJobCompletedAsync(jobId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private AnalysisPipeline CreatePipeline(IEnumerable<IAnalysisStep> steps)
    {
        return new AnalysisPipeline(
            steps,
            PipelineTestHelpers.CreateConfig(),
            new Mock<GithubAnalyzer.Shared.Git.IGitService>().Object,
            _mockQueue.Object,
            _mockCache.Object,
            _publisher,
            NullLoggerFactory.Instance);
    }

    private static Mock<IAnalysisStep> CreateStep(bool shouldExecute, Exception? throws = null)
    {
        var mock = new Mock<IAnalysisStep>();
        mock.Setup(s => s.ShouldExecute(It.IsAny<PipelineContext>())).Returns(shouldExecute);

        if (shouldExecute)
        {
            var setup = mock.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()));
            if (throws != null)
                setup.ThrowsAsync(throws);
            else
                setup.Returns(Task.CompletedTask);
        }
        return mock;
    }

    private static IAnalysisStep CreateOrderedStep(List<int> executionOrder, int id, bool shouldExecute)
    {
        var mock = new Mock<IAnalysisStep>();
        mock.Setup(s => s.ShouldExecute(It.IsAny<PipelineContext>())).Returns(shouldExecute);
        if (shouldExecute)
        {
            mock.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add(id))
                .Returns(Task.CompletedTask);
        }
        return mock.Object;
    }
}
