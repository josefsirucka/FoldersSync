// <copyright file="ComputeMD5HashExtension.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>

using System.Security.Cryptography;

using MyFolderSync.Helpers;

using PerfectResult;

namespace MyFolderSync;

/// <summary>
/// Command to compute MD5 hash of a file.
/// </summary>
public static class ComputeMD5HashExtension
{
    /// <summary>
    /// Executes the MD5 hash computation for the given file.
    /// </summary>
    /// <param name="file">IFile instance.</param>
    /// <returns></returns>
    public static IResult<string> CalculateMD5Hash(this IFile file)
    {
        try
        {
            using FileStream fileStream = new(file.GetFullPath(), FileMode.Open, FileAccess.Read, FileShare.Read);
            using MD5 md5 = MD5.Create();

            byte[] hashBytes = md5.ComputeHash(fileStream);
            string hash = Convert.ToHexString(hashBytes);

            return IResult.SuccessResult<string>(hash);
        }
        catch (Exception ex)
        {
            return IResult.FailureResult<string>(
                $"Error computing MD5 hash for file: {file.GetFullPath()}",
                ex
            );
        }
    }
}
