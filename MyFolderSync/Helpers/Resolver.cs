// <copyright file="Resolver.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

using System.Text.RegularExpressions;
using PerfectResult;

namespace MyFolderSync.Helpers;

/// <summary>
/// Path resolver for files and folders.
/// </summary>
public partial class Resolver
{
    /// <summary>
    /// It is difficult to determine whether the given path is a file or folder.
    /// This method tries to resolve the given path into either IFile or IFolder instance.
    /// </summary>
    /// <param name="path">Unresolved path string.</param>
    /// <param name="defaultFileName">Default file name to use if the path does not resolve to a file.</param>
    /// <returns>Result with File instance.</returns>
    public IResult<IFile> ResolveFileName(string path, string? defaultFileName = null)
    {
        path = FixWhiteSpaces(path);

        if (!IsPathValid(path))
        {
            return IResult.FailureResult<IFile>("Path is not valid.");
        }

        if (string.IsNullOrEmpty(path))
        {
            return IResult.FailureResult<IFile>("Path is null or empty.");
        }

        string fullPath = Path.IsPathFullyQualified(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));

        fullPath = NormalizePath(fullPath);

        bool isExistingFile = File.Exists(fullPath);
        bool isExistingFolder = Directory.Exists(fullPath);
        bool looksLikeFile = Path.HasExtension(path) && !isExistingFolder;

        if (isExistingFile || looksLikeFile)
        {
            string? directory = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(directory))
            {
                return IResult.FailureResult<IFile>("Cannot determine the directory of the file.");
            }

            return IResult.SuccessResult(
                IFile.Create(Path.GetFileName(fullPath), IFolder.Create(directory))
            );
        }

        if (string.IsNullOrEmpty(defaultFileName))
        {
            return IResult.FailureResult<IFile>(
                "Path does not point to a file and no default file name is provided."
            );
        }

        return IResult.SuccessResult(IFile.Create(defaultFileName, IFolder.Create(fullPath)));
    }

    /// <summary>
    /// It is difficult to determine whether the given path is a file or folder.
    /// This method tries to resolve the given path into an IFolder instance.
    /// </summary>
    /// <param name="path">Unresolved path string.</param>
    /// <returns>Result with Folder instance.</returns>
    public IResult<IFolder> ResolveFolderName(string path)
    {
        path = FixWhiteSpaces(path);

        if (!IsPathValid(path))
        {
            return IResult.FailureResult<IFolder>("Path is not valid.");
        }

        if (string.IsNullOrEmpty(path))
        {
            return IResult.FailureResult<IFolder>("Path is null or empty.");
        }

        string fullPath = Path.IsPathFullyQualified(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));

        fullPath = NormalizePath(fullPath);

        if (IsDriveRoot(fullPath))
        {
            return IResult.FailureResult<IFolder>(
                "Path cannot be only a drive root (e.g., C:\\ or D:)."
            );
        }

        bool isExistingFolder = Directory.Exists(fullPath);
        bool isExistingFile = File.Exists(fullPath);
        bool looksLikeFolder =
            !Path.HasExtension(path)
            || // nemá příponu → spíš složka
            path.EndsWith(Path.DirectorySeparatorChar.ToString())
            || path.EndsWith(Path.AltDirectorySeparatorChar.ToString());

        if (isExistingFile && !isExistingFolder)
        {
            return IResult.FailureResult<IFolder>("Path points to a file, not a folder.");
        }

        if (isExistingFolder || looksLikeFolder)
        {
            return IResult.SuccessResult(IFolder.Create(fullPath));
        }

        return IResult.SuccessResult(IFolder.Create(fullPath));
    }

    private static string FixWhiteSpaces(string path)
    {
        return path.Trim();
    }

    private static bool IsPathValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return false;
        }

        // I will be not lying at this point — this regex is magic to me :) And I used online resources to find it.
        if (Regex.IsMatch(path, @"^[^a-zA-Z]*:[^\\/].*"))
        {
            return false;
        }

        return true;
    }

    private static string NormalizePath(string path)
    {
        string normalized = path.Replace('/', '\\');

        while (normalized.Contains("\\\\"))
        {
            normalized = normalized.Replace("\\\\", "\\");
        }

        if (normalized.Length > 3 && normalized.EndsWith("\\"))
        {
            normalized = normalized.TrimEnd('\\');
        }

        return normalized;
    }

    private static bool IsDriveRoot(string path)
    {
        try
        {
            string? root = Path.GetPathRoot(path);

            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            root = root.TrimEnd('\\', '/');
            path = path.TrimEnd('\\', '/');

            return string.Equals(root, path, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
