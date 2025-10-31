// <copyright file="TimerServiceTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Services;
using Serilog;
using Serilog.Core;

namespace MyFolderSync.Tests.Services;

/// <summary>
/// Tests for <see cref="TimerService"/> class.
/// </summary>
[TestFixture]
public class TimerServiceTests
{
    private Logger _logger;
    private TestSyncService _syncService;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

        _syncService = new TestSyncService();
    }

    [TearDown]
    public void TearDown()
    {
        _logger?.Dispose();
    }

    [TestCase(1, TestName = "Timer with 1 second interval")]
    [TestCase(5, TestName = "Timer with 5 second interval")]
    [TestCase(60, TestName = "Timer with 60 second interval")]
    public void Constructor_Should_Initialize_With_Various_Intervals(int intervalInSeconds)
    {
        Assert.DoesNotThrow(() =>
        {
            using TimerService timerService = new(intervalInSeconds, _logger, _syncService);
        });
    }

    [Test]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new TimerService(1, null!, _syncService));
    }

    [Test]
    public void Constructor_Should_Throw_When_SyncService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new TimerService(1, _logger, null!));
    }

    [TestCase(0, TestName = "Zero interval should be handled")]
    [TestCase(-1, TestName = "Negative interval should be handled")]
    [TestCase(-60, TestName = "Large negative interval should be handled")]
    public void Constructor_Should_Handle_Invalid_Intervals(int intervalInSeconds)
    {
        Assert.DoesNotThrow(() =>
        {
            using TimerService timerService = new(intervalInSeconds, _logger, _syncService);
        });
    }

    [Test]
    public async Task StartTicking_Should_Execute_Initial_Sync()
    {
        using TimerService timerService = new(60, _logger, _syncService);
        using CancellationTokenSource cts = new();

        Task tickingTask = timerService.StartTicking(cts.Token);

        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await tickingTask;
        }
        catch (OperationCanceledException) { }

        Assert.That(
            _syncService.SyncCallCount,
            Is.GreaterThanOrEqualTo(1),
            "Initial sync should be executed"
        );
    }

    [Test]
    public async Task StartTicking_Should_Handle_Cancellation()
    {
        using TimerService timerService = new(1, _logger, _syncService);
        using CancellationTokenSource cts = new();

        Task tickingTask = timerService.StartTicking(cts.Token);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await tickingTask);
    }

    [Test]
    public async Task StartTicking_Should_Handle_Sync_Failures()
    {
        TestSyncService failingSyncService = new(shouldFail: true);
        using TimerService timerService = new(60, _logger, failingSyncService);
        using CancellationTokenSource cts = new();

        Task tickingTask = timerService.StartTicking(cts.Token);

        await Task.Delay(100);
        cts.Cancel();

        try
        {
            await tickingTask;
        }
        catch (OperationCanceledException) { }

        Assert.That(
            failingSyncService.SyncCallCount,
            Is.GreaterThanOrEqualTo(1),
            "Sync should be attempted even if it fails"
        );
    }

    [Test]
    public async Task StartTicking_Should_Skip_Overlapping_Syncs()
    {
        TestSyncService slowSyncService = new(delay: TimeSpan.FromSeconds(2));
        using TimerService timerService = new(1, _logger, slowSyncService);
        using CancellationTokenSource cts = new();

        Task tickingTask = timerService.StartTicking(cts.Token);

        await Task.Delay(3000);
        cts.Cancel();

        try
        {
            await tickingTask;
        }
        catch (OperationCanceledException) { }

        Assert.That(slowSyncService.SyncCallCount, Is.LessThan(4), "Should skip overlapping syncs");
        Assert.That(
            slowSyncService.SyncCallCount,
            Is.GreaterThan(0),
            "Should execute at least one sync"
        );
    }

    [Test]
    public async Task StartTicking_Should_Continue_After_Sync_Exception()
    {
        TestSyncService intermittentFailureService = new(shouldFailIntermittently: true);
        using TimerService timerService = new(60, _logger, intermittentFailureService);
        using CancellationTokenSource cts = new();

        Task tickingTask = timerService.StartTicking(cts.Token);

        await Task.Delay(200);
        cts.Cancel();

        try
        {
            await tickingTask;
        }
        catch (OperationCanceledException) { }

        Assert.That(
            intermittentFailureService.SyncCallCount,
            Is.GreaterThanOrEqualTo(1),
            "Should attempt sync despite exceptions"
        );
    }

    [Test]
    public void Dispose_Should_Complete_Successfully()
    {
        TimerService timerService = new(1, _logger, _syncService);

        Assert.DoesNotThrow(() => timerService.Dispose());
    }

    [Test]
    public void Dispose_Should_Be_Idempotent()
    {
        TimerService timerService = new(1, _logger, _syncService);

        Assert.DoesNotThrow(() =>
        {
            timerService.Dispose();
            timerService.Dispose(); // Second call should be safe
        });
    }

    /// <summary>
    /// Test implementation of <see cref="ISyncService"/> for testing purposes.
    /// </summary>
    private class TestSyncService : ISyncService
    {
        private readonly bool _shouldFail;
        private readonly bool _shouldFailIntermittently;
        private readonly TimeSpan _delay;
        private int _callCount;

        public TestSyncService(
            bool shouldFail = false,
            bool shouldFailIntermittently = false,
            TimeSpan? delay = null
        )
        {
            _shouldFail = shouldFail;
            _shouldFailIntermittently = shouldFailIntermittently;
            _delay = delay ?? TimeSpan.Zero;
            _callCount = 0;
        }

        public int SyncCallCount => _callCount;

        public async Task SyncFoldersAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_shouldFail)
            {
                throw new InvalidOperationException("Test sync service configured to fail");
            }

            if (_shouldFailIntermittently && _callCount % 2 == 0)
            {
                throw new InvalidOperationException("Test sync service intermittent failure");
            }
        }
    }
}
