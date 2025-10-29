// <copyright file="SyncService.cs" company="Papirfly Group">
// Copyright (c) Papirfly Group. All rights reserved.
// </copyright>

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

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncService"/> class.
    /// </summary>
    /// <param name="folders">Folders to sync.</param>
    /// <param name="logger">Logger instance.</param>
    public SyncService(IReadOnlyDictionary<IFolder, IFolder> folders, ILogger logger)
    {
        _logger = logger;
        _foldersToSync = CheckStartUpConditions(folders);
    }

    /// <inheritdoc/>
    public Task SyncFoldersAsync()
    {
        _logger.Information("SyncService: SyncFoldersAsync called.");

        foreach (KeyValuePair<IFolder, IFolder> syncPair in _foldersToSync)
        {
            _logger.Information(
                "Syncing from {Source} to {Destination}",
                syncPair.Key.FullPath,
                syncPair.Value.FullPath
            );
        }
        // Implementation of folder synchronization logic goes here.
        return Task.CompletedTask;
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
}
