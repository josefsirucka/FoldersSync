// <copyright file="DeleteCommandTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Commands;
using MyFolderSync.Helpers;
using Serilog;
using Serilog.Core;

namespace MyFolderSync.Tests.Commands;

/// <summary>
/// Tests for <see cref="DeleteCommand"/> class.
/// </summary>
[TestFixture]
public class DeleteCommandTests
{
    private Logger _logger;
    private string _testBaseDirectory;
    private string _targetFolder;
    private IFolder _targetFolderInstance;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

        _testBaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "DeleteCommandTests",
            Guid.NewGuid().ToString()
        );
        _targetFolder = Path.Combine(_testBaseDirectory, "Target");

        Directory.CreateDirectory(_targetFolder);
        _targetFolderInstance = IFolder.Create(_targetFolder);
    }

    [TearDown]
    public void TearDown()
    {
        _logger?.Dispose();

        if (Directory.Exists(_testBaseDirectory))
        {
            try
            {
                Directory.Delete(_testBaseDirectory, recursive: true);
            }
            catch { }
        }
    }

    [TestCase(true, TestName = "Delete with prune empty directories enabled")]
    [TestCase(false, TestName = "Delete with prune empty directories disabled")]
    public async Task ExecuteAsync_Should_Delete_File_With_Prune_Option(bool pruneEmptyDirectories)
    {
        string subDirectory = "subdir";
        string targetSubDir = Path.Combine(_targetFolder, subDirectory);
        Directory.CreateDirectory(targetSubDir);

        string targetFilePath = Path.Combine(targetSubDir, "delete-me.txt");
        await File.WriteAllTextAsync(targetFilePath, "File to delete");

        IFolder fileFolderInstance = IFolder.Create(targetSubDir);
        IFile fileToDelete = IFile.Create("delete-me.txt", fileFolderInstance);
        fileToDelete.RelativePath = subDirectory;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(File.Exists(targetFilePath), Is.False, "File should be deleted");

            if (pruneEmptyDirectories)
            {
                Assert.That(
                    Directory.Exists(targetSubDir),
                    Is.False,
                    "Empty directory should be pruned"
                );
            }
            else
            {
                Assert.That(
                    Directory.Exists(targetSubDir),
                    Is.True,
                    "Directory should remain when prune is disabled"
                );
            }
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Non_Existent_File()
    {
        IFolder fileFolderInstance = IFolder.Create(_targetFolder);
        IFile fileToDelete = IFile.Create("nonexistent.txt", fileFolderInstance);
        fileToDelete.RelativePath = string.Empty;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(
                command.Result.Success,
                Is.True,
                "Command should succeed when file doesn't exist"
            );
            Assert.That(
                command.Result.Message,
                Does.Contain("already absent"),
                "Result should indicate file was already absent"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Read_Only_File()
    {
        string targetFilePath = Path.Combine(_targetFolder, "readonly.txt");
        await File.WriteAllTextAsync(targetFilePath, "Read-only file content");
        File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);

        IFolder fileFolderInstance = IFolder.Create(_targetFolder);
        IFile fileToDelete = IFile.Create("readonly.txt", fileFolderInstance);
        fileToDelete.RelativePath = string.Empty;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(
                command.Result.Success,
                Is.True,
                "Command should succeed even with read-only file"
            );
            Assert.That(File.Exists(targetFilePath), Is.False, "Read-only file should be deleted");
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Not_Delete_Outside_Root()
    {
        string outsideFolder = Path.Combine(Path.GetTempPath(), "OutsideFolder");
        Directory.CreateDirectory(outsideFolder);

        try
        {
            string outsideFilePath = Path.Combine(outsideFolder, "outside.txt");
            await File.WriteAllTextAsync(outsideFilePath, "Outside file");

            IFolder outsideFolderInstance = IFolder.Create(outsideFolder);
            IFile fileToDelete = IFile.Create("outside.txt", outsideFolderInstance);
            fileToDelete.RelativePath = "..\\..\\OutsideFolder";

            DeleteCommand command = new(
                _logger,
                fileToDelete,
                _targetFolderInstance,
                pruneEmptyDirectories: false
            );
            using CancellationTokenSource cts = new();

            await command.ExecuteAsync(cts.Token);

            Assert.Multiple(() =>
            {
                Assert.That(
                    command.Result.Success,
                    Is.False,
                    "Command should fail when trying to delete outside root"
                );
                Assert.That(
                    command.Result.Message,
                    Does.Contain("outside of target root"),
                    "Error message should indicate security violation"
                );
                Assert.That(
                    File.Exists(outsideFilePath),
                    Is.True,
                    "File outside root should remain untouched"
                );
            });
        }
        finally
        {
            if (Directory.Exists(outsideFolder))
            {
                Directory.Delete(outsideFolder, recursive: true);
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_Should_Not_Prune_Non_Empty_Directory()
    {
        string subDirectory = "subdir";
        string targetSubDir = Path.Combine(_targetFolder, subDirectory);
        Directory.CreateDirectory(targetSubDir);

        string targetFilePath = Path.Combine(targetSubDir, "delete-me.txt");
        string keepFilePath = Path.Combine(targetSubDir, "keep-me.txt");

        await File.WriteAllTextAsync(targetFilePath, "File to delete");
        await File.WriteAllTextAsync(keepFilePath, "File to keep");

        IFolder fileFolderInstance = IFolder.Create(targetSubDir);
        IFile fileToDelete = IFile.Create("delete-me.txt", fileFolderInstance);
        fileToDelete.RelativePath = subDirectory;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories: true
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(File.Exists(targetFilePath), Is.False, "Target file should be deleted");
            Assert.That(File.Exists(keepFilePath), Is.True, "Other file should remain");
            Assert.That(
                Directory.Exists(targetSubDir),
                Is.True,
                "Non-empty directory should not be pruned"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Cancellation()
    {
        string targetFilePath = Path.Combine(_targetFolder, "test.txt");
        await File.WriteAllTextAsync(targetFilePath, "Test content");

        IFolder fileFolderInstance = IFolder.Create(_targetFolder);
        IFile fileToDelete = IFile.Create("test.txt", fileFolderInstance);
        fileToDelete.RelativePath = string.Empty;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories: false
        );
        using CancellationTokenSource cts = new();

        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await command.ExecuteAsync(cts.Token)
        );
    }

    [TestCase("", "file.txt", TestName = "Delete file in root folder")]
    [TestCase("subdir", "file.txt", TestName = "Delete file in subdirectory")]
    [TestCase("deep/nested/path", "file.txt", TestName = "Delete file in deeply nested path")]
    public async Task ExecuteAsync_Should_Handle_Various_Relative_Paths(
        string relativePath,
        string fileName
    )
    {
        string fullTargetPath = Path.Combine(_targetFolder, relativePath);
        if (!string.IsNullOrEmpty(relativePath))
        {
            Directory.CreateDirectory(fullTargetPath);
        }

        string targetFilePath = Path.Combine(fullTargetPath, fileName);
        string testContent = $"Content for {fileName} in {relativePath}";
        await File.WriteAllTextAsync(targetFilePath, testContent);

        IFolder fileFolderInstance = IFolder.Create(fullTargetPath);
        IFile fileToDelete = IFile.Create(fileName, fileFolderInstance);
        fileToDelete.RelativePath = relativePath;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories: true
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(File.Exists(targetFilePath), Is.False, "File should be deleted");
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Prune_Multiple_Empty_Directories()
    {
        string deepPath = "level1/level2/level3";
        string fullTargetPath = Path.Combine(_targetFolder, deepPath);
        Directory.CreateDirectory(fullTargetPath);

        string targetFilePath = Path.Combine(fullTargetPath, "only-file.txt");
        await File.WriteAllTextAsync(targetFilePath, "Only file in deep path");

        IFolder fileFolderInstance = IFolder.Create(fullTargetPath);
        IFile fileToDelete = IFile.Create("only-file.txt", fileFolderInstance);
        fileToDelete.RelativePath = deepPath;

        DeleteCommand command = new(
            _logger,
            fileToDelete,
            _targetFolderInstance,
            pruneEmptyDirectories: true
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(File.Exists(targetFilePath), Is.False, "File should be deleted");
            Assert.That(
                Directory.Exists(fullTargetPath),
                Is.False,
                "Deepest directory should be pruned"
            );
            Assert.That(
                Directory.Exists(Path.Combine(_targetFolder, "level1/level2")),
                Is.False,
                "Middle directory should be pruned"
            );
            Assert.That(
                Directory.Exists(Path.Combine(_targetFolder, "level1")),
                Is.False,
                "Top directory should be pruned"
            );
        });
    }
}
