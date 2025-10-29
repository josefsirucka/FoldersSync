// <copyright file="BaseFolder.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

namespace MyFolderSync.Helpers;

/// <summary>
/// Base Folder class for correct folder handling.
/// </summary>
public class BaseFolder : IFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFolder"/> class.
    /// </summary>
    /// <param name="fullPath">Full path of the folder.</param>
    public BaseFolder(string fullPath)
    {
        FullPath = fullPath;
    }

    /// <inheritdoc/>
    public string FullPath { get; }
}
