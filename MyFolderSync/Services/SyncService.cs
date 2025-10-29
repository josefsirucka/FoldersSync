// <copyright file="SyncService.cs" company="Papirfly Group">
// Copyright (c) Papirfly Group. All rights reserved.
// </copyright>

using MyFolderSync.Commands;
using MyFolderSync.Helpers;

using PerfectResult;

using Serilog;

namespace MyFolderSync.Services;

/// <summary>
/// Service for folder synchronization.
/// </summary>
public class SyncService : ISyncService
{
    private readonly ILogger _logger;
    private readonly Resolver _resolver = new();
    private readonly IReadOnlyDictionary<IFolder, IFolder> _foldersToSync;
    private readonly CommandProcessorService _commandProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncService"/> class.
    /// </summary>
    /// <param name="folders">Folders to sync.</param>
    /// <param name="logger">Logger instance.</param>
    public SyncService(IReadOnlyDictionary<IFolder, IFolder> folders, ILogger logger)
    {
        _logger = logger;
        _foldersToSync = CheckStartUpConditions(folders);
        _commandProcessor = new CommandProcessorService(_logger);
    }

    /// <inheritdoc/>
    public async Task SyncFoldersAsync(CancellationToken cancellationToken)
    {
        _commandProcessor.InitProcessor(cancellationToken);

        foreach (KeyValuePair<IFolder, IFolder> syncPair in _foldersToSync)
        {
            _logger.Information(
                "Syncing from {Source} to {Destination}",
                syncPair.Key.FullPath,
                syncPair.Value.FullPath
            );

            Task<IFile[]> source = GetAllFiles(syncPair.Key);
            Task<IFile[]> target = GetAllFiles(syncPair.Value);
            IFile[][] result = await Task.WhenAll(source, target);

            await SyncFolders(result);

        }
    }

    private async Task<IFile[]> GetAllFiles(IFolder folder)
    {
        List<IFile> files = [];
        string rootPath = folder.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (string filePath in Directory.EnumerateFiles(folder.FullPath, "*", SearchOption.AllDirectories))
        {
            FileInfo fileInfo = new(filePath);

            DateTime lastUpdate = fileInfo.LastWriteTimeUtc;
            long fileSize = fileInfo.Length;

            IResult<IFile> fileResult = _resolver.ResolveFileName(fileInfo.FullName);
            if (!fileResult.Success)
            {
                _logger.Error(fileResult.Message);
                continue;
            }

            IFile sourceFile = fileResult.Value;

            IResult<string> hashResult = await sourceFile.CalculateMD5Hash();
            if (!hashResult.Success)
            {
                _logger.Error(hashResult.Message);
                continue;
            }

            sourceFile.RelativePath = Path.GetRelativePath(rootPath, fileInfo.FullName).Replace('\\', '/');

            FileInfoBase fileInfoBase = new(fileSize, lastUpdate, hashResult.Value);
            sourceFile.FileInfo = fileInfoBase;

            files.Add(sourceFile);
        }

        return files.ToArray();
    }

    private IReadOnlyDictionary<IFolder, IFolder> CheckStartUpConditions(
        IReadOnlyDictionary<IFolder, IFolder> sourceFolders
    )
    {
        Dictionary<IFolder, IFolder> validFolders = [];
        foreach (KeyValuePair<IFolder, IFolder> syncPair in sourceFolders)
        {
            IResult checkResult = CheckSourceAndTargetFolders(syncPair.Key, syncPair.Value);
            if (!checkResult.Success)
            {
                Log.Error(
                    "Folder pair ({Source} => {Destination}) check failed (folder pair will be excluded from sync): {Message}",
                    syncPair.Key.FullPath,
                    syncPair.Value.FullPath,
                    checkResult.Message
                );
                continue;
            }

            if (validFolders.ContainsKey(syncPair.Key))
            {
                Log.Warning(
                    "Source folder {Source} is already mapped to another target folder. Skipping duplicate mapping to {Destination}.",
                    syncPair.Key.FullPath,
                    syncPair.Value.FullPath
                );
                continue;
            }

            validFolders.Add(syncPair.Key, syncPair.Value);
            Log.Information(
                "Folder pair ({Source} => {Destination}) is \u001b[32mVALID\u001b[0m",
                syncPair.Key.FullPath,
                syncPair.Value.FullPath
            );
        }

        if (validFolders.Count == 0)
        {
            Log.Error("No valid folders to sync. Exiting application.");
            Environment.Exit(1);
        }

        return validFolders;
    }

    private IResult CheckSourceAndTargetFolders(IFolder source, IFolder target)
    {
        source = _resolver.ResolveFolderName(source.FullPath).Value;
        target = _resolver.ResolveFolderName(target.FullPath).Value;

        if (source.FullPath == target.FullPath)
        {
            return IResult.FailureResult("Source and target folder paths are identical.");
        }

        if (!source.DoesExist())
        {
            return IResult.FailureResult($"Source folder '{source.FullPath}' does not exist.");
        }

        if (target.InitTargetFolder() is IResult targetInit && !targetInit.Success)
        {
            return targetInit;
        }

        return IResult.SuccessResult();
    }

    private async Task SyncFolders(IFile[][] files)
    {
        IFile[] sourceFiles = files[0].Where(f => f.RelativePath != null).ToArray();
        IFile[] targetFiles = files[1].Where(f => f.RelativePath != null).ToArray();
        
        IFile[] copyToTarget = sourceFiles
            .Where(sourceFile => !targetFiles.Any(targetFile => sourceFile.Name == targetFile.Name && sourceFile.RelativePath == targetFile.RelativePath))
            .ToArray();

        IFile[] modifiedFiles = sourceFiles
            .Where(sourceFile => targetFiles.Any(targetFile =>
                targetFile.RelativePath == sourceFile.RelativePath &&
                (
                    sourceFile.FileInfo?.Hash != targetFile.FileInfo?.Hash ||
                    sourceFile.FileInfo?.FileSize != targetFile.FileInfo?.FileSize ||
                    sourceFile.FileInfo?.LastModified != targetFile.FileInfo?.LastModified
                )))
            .ToArray();

        
        IFile[] deleteFromTarget = targetFiles
            .Where(targetFile => !sourceFiles.Any(sourceFile => sourceFile.RelativePath == targetFile.RelativePath))
            .ToArray();
    }
}
