// <copyright file="CopyCommandTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Commands;
using MyFolderSync.Helpers;
using Serilog;
using Serilog.Core;

namespace MyFolderSync.Tests.Commands;

/// <summary>
/// Tests for <see cref="CopyCommand"/> class.
/// </summary>
[TestFixture]
public class CopyCommandTests
{
    private Logger _logger;
    private string _testBaseDirectory;
    private string _sourceFolder;
    private string _targetFolder;
    private IFolder _targetFolderInstance;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

        _testBaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "CopyCommandTests",
            Guid.NewGuid().ToString()
        );
        _sourceFolder = Path.Combine(_testBaseDirectory, "Source");
        _targetFolder = Path.Combine(_testBaseDirectory, "Target");

        Directory.CreateDirectory(_sourceFolder);
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

    [TestCase(true, true, TestName = "Copy with overwrite enabled and preserve attributes")]
    [TestCase(true, false, TestName = "Copy with overwrite enabled without preserve attributes")]
    [TestCase(false, true, TestName = "Copy without overwrite but preserve attributes")]
    [TestCase(false, false, TestName = "Copy without overwrite and without preserve attributes")]
    public async Task ExecuteAsync_Should_Copy_File_With_Various_Options(
        bool overWriteTarget,
        bool preserveAttributes
    )
    {
        string sourceFilePath = Path.Combine(_sourceFolder, "test.txt");
        string testContent = "Test file content";
        await File.WriteAllTextAsync(sourceFilePath, testContent);

        IFolder sourceFolderInstance = IFolder.Create(_sourceFolder);
        IFile sourceFile = IFile.Create("test.txt", sourceFolderInstance);
        sourceFile.RelativePath = string.Empty;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget,
            preserveAttributes
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        string targetFilePath = Path.Combine(_targetFolder, "test.txt");
        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(File.Exists(targetFilePath), Is.True, "Target file should exist");
            Assert.That(
                File.ReadAllText(targetFilePath),
                Is.EqualTo(testContent),
                "File content should match"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Create_Target_Directory_Structure()
    {
        string subDirectory = "subdir";
        string sourceSubDir = Path.Combine(_sourceFolder, subDirectory);
        Directory.CreateDirectory(sourceSubDir);

        string sourceFilePath = Path.Combine(sourceSubDir, "nested.txt");
        string testContent = "Nested file content";
        await File.WriteAllTextAsync(sourceFilePath, testContent);

        IFolder sourceFolderInstance = IFolder.Create(sourceSubDir);
        IFile sourceFile = IFile.Create("nested.txt", sourceFolderInstance);
        sourceFile.RelativePath = subDirectory;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget: true,
            preserveAttributes: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        string targetFilePath = Path.Combine(_targetFolder, subDirectory, "nested.txt");
        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(
                File.Exists(targetFilePath),
                Is.True,
                "Target file should exist in subdirectory"
            );
            Assert.That(
                File.ReadAllText(targetFilePath),
                Is.EqualTo(testContent),
                "File content should match"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Missing_Source_File()
    {
        IFolder sourceFolderInstance = IFolder.Create(_sourceFolder);
        IFile sourceFile = IFile.Create("nonexistent.txt", sourceFolderInstance);
        sourceFile.RelativePath = string.Empty;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget: true,
            preserveAttributes: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(
                command.Result.Success,
                Is.False,
                "Command should fail when source file is missing"
            );
            Assert.That(
                command.Result.Message,
                Does.Contain("Source file missing"),
                "Error message should indicate missing source"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Skip_When_Target_Exists_And_Overwrite_Disabled()
    {
        string sourceFilePath = Path.Combine(_sourceFolder, "test.txt");
        string targetFilePath = Path.Combine(_targetFolder, "test.txt");

        await File.WriteAllTextAsync(sourceFilePath, "New content");
        await File.WriteAllTextAsync(targetFilePath, "Existing content");

        IFolder sourceFolderInstance = IFolder.Create(_sourceFolder);
        IFile sourceFile = IFile.Create("test.txt", sourceFolderInstance);
        sourceFile.RelativePath = string.Empty;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget: false,
            preserveAttributes: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed (skip scenario)");
            Assert.That(
                File.ReadAllText(targetFilePath),
                Is.EqualTo("Existing content"),
                "Target file should remain unchanged"
            );
            Assert.That(
                command.Result.Message,
                Does.Contain("copy skipped"),
                "Result message should indicate skip"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Overwrite_When_Target_Exists_And_Overwrite_Enabled()
    {
        // Arrange
        string sourceFilePath = Path.Combine(_sourceFolder, "test.txt");
        string targetFilePath = Path.Combine(_targetFolder, "test.txt");

        await File.WriteAllTextAsync(sourceFilePath, "New content");
        await File.WriteAllTextAsync(targetFilePath, "Existing content");

        IFolder sourceFolderInstance = IFolder.Create(_sourceFolder);
        IFile sourceFile = IFile.Create("test.txt", sourceFolderInstance);
        sourceFile.RelativePath = string.Empty;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget: true,
            preserveAttributes: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(
                File.ReadAllText(targetFilePath),
                Is.EqualTo("New content"),
                "Target file should be overwritten"
            );
        });
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Cancellation()
    {
        string sourceFilePath = Path.Combine(_sourceFolder, "test.txt");
        await File.WriteAllTextAsync(sourceFilePath, "Test content");

        IFolder sourceFolderInstance = IFolder.Create(_sourceFolder);
        IFile sourceFile = IFile.Create("test.txt", sourceFolderInstance);
        sourceFile.RelativePath = string.Empty;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget: true,
            preserveAttributes: false
        );
        using CancellationTokenSource cts = new();

        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await command.ExecuteAsync(cts.Token)
        );
    }

    [TestCase("", "file.txt", TestName = "File in root folder")]
    [TestCase("subdir", "file.txt", TestName = "File in subdirectory")]
    [TestCase("deep/nested/path", "file.txt", TestName = "File in deeply nested path")]
    public async Task ExecuteAsync_Should_Handle_Various_Relative_Paths(
        string relativePath,
        string fileName
    )
    {
        string fullSourcePath = Path.Combine(_sourceFolder, relativePath);
        if (!string.IsNullOrEmpty(relativePath))
        {
            Directory.CreateDirectory(fullSourcePath);
        }

        string sourceFilePath = Path.Combine(fullSourcePath, fileName);
        string testContent = $"Content for {fileName} in {relativePath}";
        await File.WriteAllTextAsync(sourceFilePath, testContent);

        IFolder sourceFolderInstance = IFolder.Create(fullSourcePath);
        IFile sourceFile = IFile.Create(fileName, sourceFolderInstance);
        sourceFile.RelativePath = relativePath;

        CopyCommand command = new(
            _logger,
            sourceFile,
            _targetFolderInstance,
            overWriteTarget: true,
            preserveAttributes: false
        );
        using CancellationTokenSource cts = new();

        await command.ExecuteAsync(cts.Token);

        string expectedTargetPath = Path.Combine(_targetFolder, relativePath, fileName);
        Assert.Multiple(() =>
        {
            Assert.That(command.Result.Success, Is.True, "Command should succeed");
            Assert.That(
                File.Exists(expectedTargetPath),
                Is.True,
                "Target file should exist at correct path"
            );
            Assert.That(
                File.ReadAllText(expectedTargetPath),
                Is.EqualTo(testContent),
                "File content should match"
            );
        });
    }
}
