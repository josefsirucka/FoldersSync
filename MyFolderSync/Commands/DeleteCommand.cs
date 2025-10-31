// <copyright file="DeleteCommand.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

#pragma warning disable CS1998

using MyFolderSync.Helpers;
using PerfectResult;
using Serilog;

namespace MyFolderSync.Commands;

/// <summary>
/// Command to delete a file from the target folder.
/// </summary>
public sealed class DeleteCommand : SyncCommandBase
{
    private readonly IFile _file;
    private readonly IFolder _targetFolder;
    private readonly bool _pruneEmptyDirectories;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCommand"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="file">File to delete.</param>
    /// <param name="targetFolder">Target folder.</param>
    /// <param name="pruneEmptyDirectories">Whether to prune empty directories.</param>
    public DeleteCommand(
        ILogger logger,
        IFile file,
        IFolder targetFolder,
        bool pruneEmptyDirectories = true
    )
        : base(logger)
    {
        _file = file;
        _targetFolder = targetFolder;
        _pruneEmptyDirectories = pruneEmptyDirectories;
    }

    /// <inheritdoc/>
    public override async Task CommandExecutionImplementation(CancellationToken cancellationToken)
    {
        string root = Path.GetFullPath(_targetFolder.FullPath);
        string destDir = Path.GetFullPath(Path.Combine(root, _file.RelativePath ?? string.Empty));
        string destFile = Path.GetFullPath(Path.Combine(destDir, _file.Name));

        if (
            !destFile.StartsWith(
                root,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal
            )
        )
        {
            Logger.Error(
                "Refusing to delete outside of root. File={File}, Root={Root}",
                destFile,
                root
            );
            Result = IResult.FailureResult("Delete outside of target root refused.");
            return;
        }

        if (!File.Exists(destFile))
        {
            Logger.Information("Delete skipped, file not found: {File}", destFile);
            Result = IResult.SuccessResult("File already absent.");
            return;
        }

        try
        {
            await RetryAsync(
                async (attempt, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    TryClearReadOnly(destFile);

                    File.Delete(destFile);

                    if (File.Exists(destFile))
                    {
                        Logger.Error(
                            "Delete failed, file still exists: {File} (attempt {Attempt})",
                            destFile,
                            attempt
                        );
                        throw new IOException("File still exists after delete attempt.");
                    }

                    Logger.Information("Deleted {File} (attempt {Attempt})", destFile, attempt);
                },
                cancellationToken
            );

            if (_pruneEmptyDirectories)
            {
                PruneEmptyDirectories(destDir, root);
            }

            Result = IResult.SuccessResult();
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Delete canceled: {File}", destFile);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Delete failed: {File}", destFile);
            throw;
        }
    }

    private static void TryClearReadOnly(string path)
    {
        try
        {
            FileAttributes attrs = File.GetAttributes(path);
            if ((attrs & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
            }
        }
        catch { }
    }

    private void PruneEmptyDirectories(string startDir, string root)
    {
        try
        {
            string? dir = startDir;
            while (!string.IsNullOrEmpty(dir))
            {
                string full = Path.GetFullPath(dir);
                if (
                    !full.StartsWith(
                        root,
                        OperatingSystem.IsWindows()
                            ? StringComparison.OrdinalIgnoreCase
                            : StringComparison.Ordinal
                    )
                )
                {
                    break;
                }

                if (!Directory.Exists(full))
                {
                    dir = Path.GetDirectoryName(full);
                    continue;
                }

                if (Directory.EnumerateFileSystemEntries(full).Any())
                {
                    break;
                }

                Directory.Delete(full);
                dir = Path.GetDirectoryName(full);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "PruneEmptyDirectories failed from {StartDir}", startDir);
        }
    }
}

#pragma warning restore CS1998
