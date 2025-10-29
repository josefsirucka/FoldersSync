// <copyright file="FileToStringExtension.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

using MyFolderSync.Helpers;

namespace MyFolderSync;

/// <summary>
/// File to string extension method.
/// </summary>
public static class FileToStringExtension
{
    /// <summary>
    /// Converts IFile to readable string format.
    /// </summary>
    /// <param name="file">IFile instance.</param>
    /// <returns>Readable string representation of the file's full path.</returns>
    public static string GetFullPath(this IFile file)
    {
        return Path.Combine(file.Folder.FullPath, file.Name);
    }
}
