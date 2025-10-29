// <copyright file="ISyncCommand.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

namespace MyFolderSync.Commands;

/// <summary>
/// Interface for sync commands.
/// </summary>
public interface ISyncCommand
{
    /// <summary>
    /// Executes the sync command.
    /// </summary>
    /// <returns>Asynchronous task representing the operation.</returns>
    Task ExecuteAsync();
}
