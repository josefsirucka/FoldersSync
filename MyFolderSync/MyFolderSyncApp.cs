// <copyright file="MyFolderSyncApp.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 26.10 2025</summary>

using MyFolderSync.Arguments;
using MyFolderSync.Helpers;
using MyFolderSync.Services;
using PerfectResult;
using Serilog;

namespace MyFolderSync;

/// <summary>
/// Main application class for MyFolderSync.
/// </summary>
public class MyFolderSyncApp : IDisposable
{
    private readonly Resolver _resolver = new();
    private readonly ArgumentsModel _settingsModel;
    private readonly TimerService _timerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyFolderSyncApp"/> class.
    /// </summary>
    /// <param name="arguments">Command line arguments.</param>
    public MyFolderSyncApp(string[] arguments)
    {
        ConsoleHelper.Welcome();

        IResult<ArgumentsModel> result = ArgumentsHandler.ProcessArgsAndGetSettings(arguments);
        if (result.Success)
        {
            _settingsModel = result.Value;
        }
        else
        {
            ConsoleHelper.WriteError(
                $"Failed to process command line arguments. {result?.Message}"
            );
            Environment.Exit(1);
        }

        _timerService = InitServices();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Log.CloseAndFlush();
        _timerService?.Dispose();
    }

    /// <summary>
    /// Main method to run the application.
    /// </summary>
    public async Task Run()
    {
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        Task keyListener = ESCListenerTask(cts, token);

        try
        {
            await _timerService.StartTicking(token);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Sync app stopped by user (ESC pressed).");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while running the sync app.");
        }
        finally
        {
            Dispose();
            Log.Information("Sync app exited cleanly.");
        }
    }

    private Task ESCListenerTask(CancellationTokenSource source, CancellationToken token)
    {
        return Task.Run(
            () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        source.Cancel();
                        break;
                    }

                    Task.Delay(100).Wait();
                }
            },
            token
        );
    }

    private TimerService InitServices()
    {
        IResult<IFile> logFileResult = _resolver.ResolveFileName(_settingsModel.LogPath);
        if (!logFileResult.Success)
        {
            ConsoleHelper.WriteError(
                $"Log file path '{_settingsModel.LogPath}' is not resolvable. Error: {logFileResult.Message}"
            );
            Environment.Exit(1);
        }

        string logPath = logFileResult.Value.GetFullPath();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(_settingsModel.LogLevel)
            .WriteTo.Console()
            .MinimumLevel.Is(_settingsModel.LogLevel)
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Application starting... (press ESC to stop)");
        Log.Debug(
            "Logger initialized with path: {LogPath} and level: {LogLevel}",
            logPath,
            _settingsModel.LogLevel
        );

        Log.Debug("Setting Interval to {Interval} seconds", _settingsModel.Interval);

        SyncService syncService = new(_settingsModel.ResolvedFolders, Log.Logger);

        return new TimerService(_settingsModel.Interval, Log.Logger, syncService);
    }
}
