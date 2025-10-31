// <copyright file="CopyCommand.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>


using MyFolderSync.Helpers;
using PerfectResult;
using Serilog;

namespace MyFolderSync.Commands;

/// <summary>
/// Command for copying files.
/// </summary>
public sealed class CopyCommand : SyncCommandBase
{
    private const int _bufferSize = 1_048_576;
    private readonly IFile _file;
    private readonly IFolder _targetFolder;
    private readonly bool _preserveAttributes;
    private readonly bool _overWriteTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyCommand"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="file">File to copy.</param>
    /// <param name="targetFolder">Target folder.</param>
    /// <param name="overWriteTarget">Whether to overwrite the target file if it exists.</param>
    /// <param name="preserveAttributes">Whether to preserve file attributes.</param>
    public CopyCommand(
        ILogger logger,
        IFile file,
        IFolder targetFolder,
        bool overWriteTarget,
        bool preserveAttributes
    )
        : base(logger)
    {
        _file = file;
        _targetFolder = targetFolder;
        _preserveAttributes = preserveAttributes;
        _overWriteTarget = overWriteTarget;
    }

    /// <inheritdoc/>
    public override async Task CommandExecutionImplementation(CancellationToken cancellationToken)
    {
        string srcFile = _file.GetFullPath();
        string destDir = Path.Combine(_targetFolder.FullPath, _file.RelativePath ?? string.Empty);
        string destFile = Path.Combine(destDir, _file.Name);
        string tempFile = Path.Combine(destDir, $"{_file.Name}.copytmp-{Guid.CreateVersion7():N}");

        if (!File.Exists(srcFile))
        {
            Logger.Error("File {File} disapeared from source folder. Skipping copy.", srcFile);
            Result = IResult.FailureResult("Source file missing. Copy skipped.");
            return;
        }

        if (File.Exists(destFile) && !_overWriteTarget)
        {
            Logger.Information(
                "File {Dest} already exists and overwrite=false. Skipping copy.",
                destFile
            );
            Result = IResult.SuccessResult("Destination file exists; copy skipped.");
            return;
        }

        if (!Directory.Exists(destDir))
        {
            try
            {
                Directory.CreateDirectory(destDir);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create target directory {Dir}.", destDir);
                Result = IResult.FailureResult($"Failed to create target directory: {ex.Message}");
                return;
            }
        }

        try
        {
            await RetryAsync(
                async (attempt, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    // 1) Kopie do dočasného souboru v cílovém adresáři
                    await CopyToTempAsync(srcFile, tempFile, ct);

                    // 2) Atomický commit na cílové jméno
                    AtomicCommit(tempFile, destFile, _overWriteTarget);

                    // 3) Metadata po commitu
                    if (_preserveAttributes)
                    {
                        FileInfo srcInfo = new FileInfo(srcFile);
                        if (srcInfo.Exists && File.Exists(destFile))
                            TryPreserveBasicMetadata(srcInfo, destFile);
                    }

                    Logger.Information(
                        "Copied {Source} => {Destination} (attempt {Attempt}).",
                        srcFile,
                        destFile,
                        attempt
                    );
                },
                cancellationToken
            );

            Result = IResult.SuccessResult();
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Copy canceled: {Source} => {Destination}", srcFile, destFile);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Copy failed: {Source} => {Destination}", srcFile, destFile);
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch (Exception cleanupEx)
            {
                Logger.Debug(cleanupEx, "Failed to delete temp file {Temp}", tempFile);
            }
        }
    }

    private async Task CopyToTempAsync(string sourceFile, string tempFile, CancellationToken ct)
    {
        await using FileStream inStream = new(
            sourceFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        await using FileStream outStream = new(
            tempFile,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            _bufferSize,
            FileOptions.Asynchronous | FileOptions.WriteThrough
        );

        await inStream.CopyToAsync(outStream, _bufferSize, ct).ConfigureAwait(false);
        await outStream.FlushAsync(ct).ConfigureAwait(false);
        outStream.Flush(true);
    }

    private IResult AtomicCommit(string tempFileName, string destFile, bool overwrite)
    {
        if (File.Exists(destFile))
        {
            if (!overwrite)
            {
                Logger.Error("Destination file {Dest} exists and overwrite is disabled.", destFile);
                return IResult.FailureResult($"Destination exists and overwrite=false: {destFile}");
            }

            try
            {
                File.Replace(
                    tempFileName,
                    destFile,
                    destinationBackupFileName: null,
                    ignoreMetadataErrors: true
                );
                return IResult.SuccessResult();
            }
            catch (PlatformNotSupportedException)
            {
                Logger.Debug("File.Replace not supported; falling back to Move overwrite.");
            }
            catch (IOException)
            {
                throw;
            }

            File.Move(tempFileName, destFile, overwrite: true);
            return IResult.SuccessResult();
        }
        else
        {
            File.Move(tempFileName, destFile);
            return IResult.SuccessResult();
        }
    }

    private void TryPreserveBasicMetadata(FileInfo sourceFileInfo, string destPath)
    {
        try
        {
            File.SetCreationTimeUtc(destPath, sourceFileInfo.CreationTimeUtc);
            File.SetLastWriteTimeUtc(destPath, sourceFileInfo.LastWriteTimeUtc);
            File.SetAttributes(destPath, sourceFileInfo.Attributes);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to preserve metadata for {Dest}.", destPath);
        }
    }
}
