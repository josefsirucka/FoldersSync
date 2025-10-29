// <copyright file="TimerService.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 24.10 2025</summary>


using Serilog;

namespace MyFolderSync.Services;

/// <summary>
/// Timer Service for background tasks.
/// </summary>
public class TimerService : IDisposable
{
    private readonly PeriodicTimer _timer;
    private readonly ILogger _logger;
    private readonly int _interval;
    private readonly ISyncService _syncService;
    private bool _syncInProgress = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerService"/> class.
    /// </summary>
    /// <param name="interval">Interval in seconds.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="syncService">Sync service instance.</param>
    public TimerService(int interval, ILogger logger, ISyncService syncService)
    {
        _interval = interval;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        _logger = logger;
        _syncService = syncService;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer?.Dispose();
    }

    /// <summary>
    /// Starts the ticking process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task.</returns>
    public async Task StartTicking(CancellationToken cancellationToken)
    {
        // Initial run
        await RunAsync(cancellationToken);

        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            await RunAsync(cancellationToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_syncInProgress)
        {
            _logger.Warning("Sync is already in progress. Skipping this tick.");
            LogNextRun();
            return;
        }

        _syncInProgress = true;
        _logger.Information("Starting sync process...");
        try
        {
            await _syncService.SyncFoldersAsync(cancellationToken);
            _logger.Information("Folder sync completed.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred during folder sync.");
        }
        finally
        {
            _syncInProgress = false;
            LogNextRun();
        }
    }

    private void LogNextRun()
    {
        _logger.Information(
            "Next sync scheduled in {Interval} seconds. At: {NextRunTime}",
            _interval,
            DateTime.Now.AddSeconds(_interval)
        );
    }
}
