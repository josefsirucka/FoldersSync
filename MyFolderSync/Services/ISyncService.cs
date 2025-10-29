// <copyright file="ISyncService.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 23.10 2025</summary>

namespace MyFolderSync.Services;

/// <summary>
/// Interface for folder synchronization service.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Starts the synchronization process.
    /// </summary>
    Task SyncFoldersAsync();
}
