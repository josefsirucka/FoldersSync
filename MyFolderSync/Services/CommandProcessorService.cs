// <copyright file="CommandProcessorService.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>

using System.Threading.Channels;
using MyFolderSync.Commands;
using Serilog;

namespace MyFolderSync.Services;

/// <summary>
/// Service for processing commands.
/// </summary>
public class CommandProcessorService : IAsyncDisposable
{
    private readonly Channel<ISyncCommand> _channel;
    private readonly ILogger _logger;
    private Task? _consumerTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandProcessorService"/> class.
    /// </summary>
    public CommandProcessorService(ILogger logger)
    {
        _channel = Channel.CreateUnbounded<ISyncCommand>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        _logger = logger;
    }

    /// <summary>
    /// Initializes the command processor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void InitProcessor(CancellationToken cancellationToken)
    {
        _consumerTask = Task.Run(() => ProcessCommandsAsync(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Adds a command to the processing queue.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <returns>Return task.</returns>
    public void AddCommand(ISyncCommand command)
    {
        if (!_channel.Writer.TryWrite(command))
        {
            _logger.Error("Failed to enqueue command!");
        }
    }

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        await foreach (ISyncCommand command in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Command failed: {Message}", command.Result.Message);
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        if (_consumerTask != null && !_consumerTask.IsCompleted)
        {
            await _consumerTask;
        }
    }
}
