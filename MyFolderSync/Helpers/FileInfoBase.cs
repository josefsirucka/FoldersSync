// <copyright file="FileInfoBase.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>


namespace MyFolderSync.Helpers;

/// <summary>
/// Base FileInfo class for correct file information handling.
/// </summary>
public class FileInfoBase : IFileInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileInfoBase"/> class.
    /// </summary>
    /// <param name="fileSize">Size of the file.</param>
    /// <param name="lastModified">Last modified date and time.</param>
    public FileInfoBase(long fileSize, DateTime lastModified)
    {
        FileSize = fileSize;
        LastModified = lastModified;
    }

    /// <inheritdoc/>
    public long FileSize { get; }

    /// <inheritdoc/>
    public DateTime LastModified { get; }
}
