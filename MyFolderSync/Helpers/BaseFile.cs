// <copyright file="BaseFile.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

namespace MyFolderSync.Helpers;

/// <summary>
/// Base File class for correct name and folder handling.
/// </summary>
public class FileBase : IFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileBase"/> class.
    /// </summary>
    /// <param name="name">File name.</param>
    /// <param name="folder">Folder instance.</param>
    public FileBase(string name, IFolder folder)
    {
        Name = name;
        Folder = folder;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IFolder Folder { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Path.Combine(Folder.FullPath, Name);
    }
}
