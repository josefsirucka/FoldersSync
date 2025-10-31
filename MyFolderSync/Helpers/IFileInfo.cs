// <copyright file="IFileInfo.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>

namespace MyFolderSync.Helpers;

/// <summary>
/// Interface representing file information.
/// </summary>
public interface IFileInfo
{
    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    long FileSize { get; }

    /// <summary>
    /// Gets the last modified date and time of the file.
    /// </summary>
    DateTime LastModified { get; }
}
