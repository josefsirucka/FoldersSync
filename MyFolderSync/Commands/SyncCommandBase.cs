// <copyright file="SyncCommandBase.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>

using PerfectResult;
using Serilog;

namespace MyFolderSync.Commands;

/// <summary>
/// Base class for sync commands.
/// </summary>
public abstract class SyncCommandBase : ISyncCommand
{
    private const int _maxRetries = 5;
    private readonly TimeSpan _baseDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncCommandBase"/> class.
    /// </summary>
    /// <param name="logger">Basic Logger class.</param>
    protected SyncCommandBase(ILogger logger)
    {
        Logger = logger;
        Result = IResult.FailureResult("Not executed yet.");
    }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    public IResult Result { get; protected set; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.Debug("Starting command {CommandName}", GetType().Name);

        await CommandExecutionImplementation(cancellationToken);

        if (Result.Success)
        {
            Logger.Debug("Command {CommandName} executed successfully", GetType().Name);
        }
        else
        {
            Logger.Error(
                Result.Exception,
                "Command {CommandName} executed with error: {Message}",
                GetType().Name,
                Result.Message
            );
        }
    }

    /// <summary>
    /// Implementation of the command execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async task.</returns>
    public abstract Task CommandExecutionImplementation(CancellationToken cancellationToken);

    /// <summary>
    /// Retries the specified action with exponential backoff.
    /// </summary>
    /// <param name="action">Action to be retried.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async task.</returns>
    protected async Task RetryAsync(Func<int, CancellationToken, Task> action, CancellationToken ct)
    {
        Exception? last = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await action(attempt, ct).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException ex)
            {
                last = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                last = ex;
            }

            if (attempt == _maxRetries)
                break;

            TimeSpan delay = TimeSpan.FromMilliseconds(
                _baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)
            );
            Logger.Debug(
                "Delete retry {Attempt}/{Max} in {Delay} due to: {Error}",
                attempt,
                _maxRetries,
                delay,
                last?.Message
            );
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        throw new IOException($"Delete failed after {_maxRetries} attempts.", last);
    }
}
