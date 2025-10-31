// <copyright file="SyncService.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

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
            _logger.Information("Scanning source: {Source}", syncPair.Key.FullPath);
            IFile[] source = await GetAllFiles(syncPair.Key, cancellationToken);

            _logger.Information("Scanning target: {Target}", syncPair.Value.FullPath);
            IFile[] target = await GetAllFiles(syncPair.Value, cancellationToken);

            IFile[][] result = [source, target];

            IFile[][] analyzedFolders = AnylyzeFolders(result);

            Task createFileTask = CreateFile(analyzedFolders[0], syncPair.Value);
            Task overwriteFilesTask = MoveFile(analyzedFolders[1], syncPair.Value);
            Task deleteFilesTask = DeleteFilesFromTarget(analyzedFolders[2], syncPair.Value);

            await Task.WhenAll(createFileTask, overwriteFilesTask, deleteFilesTask);

            await _commandProcessor.DisposeAsync();
        }
    }

    private void ReportStatus(string message, int totalItems, int itemsProcessed)
    {
        double progress = itemsProcessed / (double)totalItems * 100;
        ConsoleHelper.WriteProcess(progress, message);
    }

    private async Task<IFile[]> GetAllFiles(IFolder folder, CancellationToken cancellationToken)
    {
        List<IFile> files = [];
        string rootPath = folder.FullPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        );

        IEnumerable<string> filePaths = Directory.EnumerateFiles(
            folder.FullPath,
            "*",
            SearchOption.AllDirectories
        );
        int i = 0;
        foreach (string filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            // Originally it was here, but I don't like it - due to operations overhead.
            // Better to compare file size and last modified date only and check the hash later if needed.
            // IResult<string> hashResult = sourceFile.CalculateMD5Hash();
            // if (!hashResult.Success)
            // {
            //     _logger.Error(hashResult.Message);
            //     continue;
            // }


            // Originally I used : Path.GetRelativePath(rootPath, sourceFile.Folder.FullPath)
            // But i created "." in same cases so I created my own method GetCleanRelative
            sourceFile.RelativePath = GetCleanRelative(rootPath, sourceFile.Folder.FullPath);

            FileInfoBase fileInfoBase = new(fileSize, lastUpdate);
            sourceFile.FileInfo = fileInfoBase;

            files.Add(sourceFile);

            ReportStatus(
                Path.Combine(sourceFile.RelativePath, sourceFile.Name),
                filePaths.Count(),
                ++i
            );

            if (i % 50 == 0)
            {
                await Task.Yield();
            }
        }

        ConsoleHelper.Clear();

        return files.ToArray();
    }

    private Dictionary<IFolder, IFolder> CheckStartUpConditions(
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
        IResult<IFolder> sourceResolvedResult = _resolver.ResolveFolderName(source.FullPath);
        IResult<IFolder> targetResolvedResult = _resolver.ResolveFolderName(target.FullPath);

        if (!sourceResolvedResult.Success)
        {
            return sourceResolvedResult;
        }

        if (!targetResolvedResult.Success)
        {
            return targetResolvedResult;
        }

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

    private IFile[][] AnylyzeFolders(IFile[][] files)
    {
        IFile[] sourceFiles = files[0].Where(f => f.RelativePath != null).ToArray();
        IFile[] targetFiles = files[1].Where(f => f.RelativePath != null).ToArray();

        IFile[] copyToTarget = sourceFiles
            .Where(sourceFile =>
                !targetFiles.Any(targetFile =>
                    sourceFile.Name == targetFile.Name
                    && sourceFile.RelativePath == targetFile.RelativePath
                )
            )
            .ToArray();

        IFile[] modifiedFiles = sourceFiles
            .Where(sourceFile =>
                targetFiles.Any(targetFile =>
                    targetFile.RelativePath == sourceFile.RelativePath
                    && (
                        //sourceFile.FileInfo?.Hash != targetFile.FileInfo?.Hash ||
                        sourceFile.FileInfo?.FileSize != targetFile.FileInfo?.FileSize
                        || sourceFile.FileInfo?.LastModified != targetFile.FileInfo?.LastModified
                    )
                )
            )
            .ToArray();

        IFile[] deleteFromTarget = targetFiles
            .Where(targetFile =>
                !sourceFiles.Any(sourceFile => sourceFile.RelativePath == targetFile.RelativePath)
            )
            .ToArray();

        _logger.Debug("Files to copy to target: {Count}", copyToTarget.Length);
        _logger.Debug("Files to modify in target: {Count}", modifiedFiles.Length);
        _logger.Debug("Files to delete from target: {Count}", deleteFromTarget.Length);

        IFile[][] result = [copyToTarget, modifiedFiles, deleteFromTarget];
        return result;
    }

    private async Task CreateFile(IFile[] sourceFile, IFolder targetFolder)
    {
        foreach (IFile item in sourceFile)
        {
            CopyCommand command = new(
                _logger,
                item,
                targetFolder,
                overWriteTarget: false,
                preserveAttributes: true
            );
            _commandProcessor.AddCommand(command);
            await Task.Yield();
        }
    }

    private async Task MoveFile(IFile[] sourceFile, IFolder targetFolder)
    {
        foreach (IFile item in sourceFile)
        {
            if (MD5IsDifferent(item, targetFolder))
            {
                _logger.Warning(
                    "File {File} is marked as modified but MD5 hashes are identical. Skipping overwrite.",
                    item.GetFullPath()
                );
                continue;
            }

            CopyCommand command = new(
                _logger,
                item,
                targetFolder,
                overWriteTarget: true,
                preserveAttributes: true
            );
            _commandProcessor.AddCommand(command);
            await Task.Yield();
        }
    }

    private async Task DeleteFilesFromTarget(IFile[] sourceFile, IFolder targetFolder)
    {
        foreach (IFile item in sourceFile)
        {
            DeleteCommand command = new(_logger, item, targetFolder, pruneEmptyDirectories: true);
            _commandProcessor.AddCommand(command);
            await Task.Yield();
        }
    }

    private static string GetCleanRelative(string rootPath, string folderFullPath)
    {
        string root = Path.GetFullPath(rootPath);
        string full = Path.GetFullPath(folderFullPath);

        string rel = Path.GetRelativePath(root, full);

        if (rel == "." || rel == "./" || rel == @".\")
        {
            return string.Empty;
        }

        rel = rel.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return rel;
    }

    private bool MD5IsDifferent(IFile sourceFile, IFolder targetFolder)
    {
        IResult<string> sourceHashResult = sourceFile.CalculateMD5Hash();
        if (!sourceHashResult.Success)
        {
            _logger.Error(
                "Failed to calculate MD5 hash for source file {File}: {Message}",
                sourceFile.GetFullPath(),
                sourceHashResult.Message
            );
            return true;
        }

        IFile targetFile = IFile.Create(sourceFile.Name, IFolder.Create(Path.Combine(targetFolder.FullPath, sourceFile.RelativePath ?? string.Empty)));

        IResult<string> targetHashResult = targetFile.CalculateMD5Hash();
        if (!targetHashResult.Success)
        {
            _logger.Error(
                "Failed to calculate MD5 hash for target file {File}: {Message}",
                targetFile.GetFullPath(),
                targetHashResult.Message
            );
            return true;
        }

        return sourceHashResult.Value != targetHashResult.Value;
    }
}
