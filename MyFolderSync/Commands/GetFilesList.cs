// <copyright file="GetFilesList.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>


using MyFolderSync.Helpers;

using PerfectResult;

using Serilog;

namespace MyFolderSync.Commands;

/// <summary>
/// Gets the list of files in the specified folder.
/// </summary>
public class GetFilesList : SyncCommandBase
{
    private readonly IFolder _folder;
    private readonly List<FileInfo> files = [];
    private readonly EnumerationOptions options = new()
    {
        AttributesToSkip = 0,       
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="GetFilesList"/> class.
    /// </summary>
    /// <param name="folder">Folder.</param>
    /// <param name="logger">Logger instance.</param>
    public GetFilesList(IFolder folder, ILogger logger)
    : base(logger)
    {
        _folder = folder;
    }

    /// <inheritdoc/>
    public override Task<IResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.Information($"Getting files list from folder: {_folder.FullPath}");

        IEnumerable<string> allFile = Directory.EnumerateFileSystemEntries(_folder.FullPath, "*", SearchOption.AllDirectories);

        Result = IResult.SuccessResult();
        return Task.FromResult(Result);
    }

    private IFile[] AllFilesInFolder()
    {
        List<IFile> fileList = [];

        foreach (string filePath in Directory.EnumerateFiles(_folder.FullPath, "*", SearchOption.AllDirectories))
        {
            FileInfo fileInfo = new(filePath);

            DateTime lastUpdate = fileInfo.LastWriteTimeUtc;
            long fileSize = fileInfo.Length;


        }

        return fileList.ToArray();
    }
}