// <copyright file="ISyncCommand.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

using PerfectResult;

namespace MyFolderSync.Commands;

/// <summary>
/// Interface for sync commands.
/// </summary>
public interface ISyncCommand
{
    /// <summary>
    /// Gets the result of the command execution.
    /// </summary>
    IResult Result { get; }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async task.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
