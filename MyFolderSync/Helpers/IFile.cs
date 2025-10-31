// <copyright file="IFile.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

namespace MyFolderSync.Helpers;

/// <summary>
/// File interface for correct name and folder handling.
/// </summary>
public interface IFile
{
    /// <summary>
    /// Gets the file name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the full folder path.
    /// </summary>
    IFolder Folder { get; }

    /// <summary>
    /// Gets the file information.
    /// </summary>
    IFileInfo? FileInfo { get; set; }

    /// <summary>
    /// Gets or sets the relative file path - for comparison purposes.
    /// </summary>
    string? RelativePath { get; set; }

    /// <summary>
    /// Creates a new file instance.
    /// </summary>
    /// <param name="name">Name of the file.</param>
    /// <param name="folder">Folder instance.</param>
    /// <returns>Complete instance of IFile with clearly defined name and folder.</returns>
    public static IFile Create(string name, IFolder folder)
    {
        return new FileBase(name, folder);
    }
}
