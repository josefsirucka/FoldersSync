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
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);
}
