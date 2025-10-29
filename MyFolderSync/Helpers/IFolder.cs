// <copyright file="IFolder.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

namespace MyFolderSync.Helpers;

/// <summary>
/// Folder interface for correct folder handling.
/// </summary>
public interface IFolder
{
    /// <summary>
    /// Gets the full folder path.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Creates a new folder instance.
    /// </summary>
    /// <param name="fullPath">Full path to the folder.</param>
    /// <returns>New instance of the base folder.</returns>
    public static IFolder Create(string fullPath)
    {
        return new BaseFolder(fullPath);
    }
}
