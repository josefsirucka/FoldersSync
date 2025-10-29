// <copyright file="FolderIsAccessible.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

using MyFolderSync.Helpers;
using PerfectResult;

namespace MyFolderSync;

/// <summary>
/// Folder accessibility extension methods.
/// </summary>
public static class FolderIsAccessible
{
    /// <summary>
    /// Checks if the folder is accessible.
    /// </summary>
    /// <param name="folder">The folder to check.</param>
    /// <returns>A result indicating whether the folder is accessible.</returns>
    public static IResult CheckIfFolderIsAccessible(this IFolder folder)
    {
        if (folder == null)
        {
            return IResult.FailureResult("Folder is null");
        }

        try
        {
            string testFile = Path.Combine(folder.FullPath, Path.GetRandomFileName());
            using (File.Create(testFile, 1, FileOptions.DeleteOnClose)) { }
            return IResult.SuccessResult();
        }
        catch (Exception ex)
        {
            return IResult.FailureResult($"Path is not accessible: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the folder exists.
    /// </summary>
    /// <param name="folder">IFolder instance.</param>
    /// <returns>Returns true if folder exists, false otherwise.</returns>
    public static bool DoesExist(this IFolder folder)
    {
        return Directory.Exists(folder.FullPath);
    }

    /// <summary>
    /// Creates the folder if it does not exist.
    /// </summary>
    /// <param name="folder">Folder to create.</param>
    /// <returns>Result of the operation.</returns>
    public static IResult CreateFolder(this IFolder folder)
    {
        try
        {
            Directory.CreateDirectory(folder.FullPath);
            return IResult.SuccessResult();
        }
        catch (Exception ex)
        {
            return IResult.FailureResult($"Failed to create folder: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes the target folder by ensuring it exists and is accessible.
    /// </summary>
    /// <param name="folder">IFolder instance.</param>
    /// <returns>Returns the result of the operation.</returns>
    public static IResult InitTargetFolder(this IFolder folder)
    {
        if (!folder.DoesExist())
        {
            IResult createResult = folder.CreateFolder();
            if (!createResult.Success)
            {
                return createResult;
            }
        }

        IResult accessResult = folder.CheckIfFolderIsAccessible();
        if (!accessResult.Success)
        {
            return IResult.FailureResult(
                $"Target folder is not accessible. {accessResult.Message}"
            );
        }

        return IResult.SuccessResult();
    }

    /// <summary>
    /// Checks if the directory is empty in a safe manner.
    /// </summary>
    /// <param name="folder">IFolder instance.</param>
    /// <returns>Result of the operation.</returns>
    public static IResult IsDirectoryEmptySafe(IFolder folder)
    {
        try
        {
            if (!Directory.EnumerateFileSystemEntries(folder.FullPath).Any())
            {
                return IResult.SuccessResult();
            }
            else
            {
                return IResult.FailureResult("Directory is not empty.");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return IResult.FailureResult($"Unauthorized access: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            return IResult.FailureResult($"IO error: {ex.Message}", ex);
        }
    }
}
