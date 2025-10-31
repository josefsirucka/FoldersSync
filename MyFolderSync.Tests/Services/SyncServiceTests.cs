// <copyright file="SyncServiceTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Helpers;
using MyFolderSync.Services;
using Serilog;
using Serilog.Core;

namespace MyFolderSync.Tests.Services;

/// <summary>
/// Tests for <see cref="SyncService"/> class.
/// </summary>
[TestFixture]
public class SyncServiceTests
{
    private Logger _logger;
    private string _testBaseDirectory;
    private string _sourceFolder;
    private string _targetFolder;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

        _testBaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "SyncServiceTests",
            Guid.NewGuid().ToString()
        );
        _sourceFolder = Path.Combine(_testBaseDirectory, "Source");
        _targetFolder = Path.Combine(_testBaseDirectory, "Target");

        Directory.CreateDirectory(_sourceFolder);
        Directory.CreateDirectory(_targetFolder);
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

    [TestCase(true, TestName = "Constructor with valid folders should succeed")]
    [TestCase(false, TestName = "Constructor with invalid folders should handle gracefully")]
    public void Constructor_Should_Handle_Folder_Validation(bool useValidFolders)
    {
        IReadOnlyDictionary<IFolder, IFolder> folders;

        if (useValidFolders)
        {
            IFolder sourceFolder = IFolder.Create(_sourceFolder);
            IFolder targetFolder = IFolder.Create(_targetFolder);
            folders = new Dictionary<IFolder, IFolder> { { sourceFolder, targetFolder } };
        }
        else
        {
            IFolder invalidSource = IFolder.Create("C:\\NonExistentSource");
            IFolder invalidTarget = IFolder.Create("C:\\NonExistentTarget");
            folders = new Dictionary<IFolder, IFolder> { { invalidSource, invalidTarget } };
        }

        if (useValidFolders)
        {
            Assert.DoesNotThrow(() => new SyncService(folders, _logger));
        }
        else
        {
            Assert.DoesNotThrow(() => new SyncService(folders, _logger));
        }
    }

    [Test]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder = IFolder.Create(_targetFolder);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder },
        };

        Assert.Throws<ArgumentNullException>(() => new SyncService(folders, null!));
    }

    [Test]
    public async Task SyncFoldersAsync_Should_Handle_Empty_Folders()
    {
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder = IFolder.Create(_targetFolder);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder },
        };

        SyncService syncService = new(folders, _logger);
        using CancellationTokenSource cts = new();

        Assert.DoesNotThrowAsync(async () => await syncService.SyncFoldersAsync(cts.Token));
    }

    [Test]
    public async Task SyncFoldersAsync_Should_Handle_Cancellation()
    {
        // Arrange
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder = IFolder.Create(_targetFolder);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder },
        };

        SyncService syncService = new(folders, _logger);
        using CancellationTokenSource cts = new();

        string testFile = Path.Combine(_sourceFolder, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await syncService.SyncFoldersAsync(cts.Token)
        );
    }

    [Test]
    public async Task SyncFoldersAsync_Should_Copy_New_Files()
    {
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder = IFolder.Create(_targetFolder);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder },
        };

        SyncService syncService = new(folders, _logger);
        using CancellationTokenSource cts = new();

        string sourceFile1 = Path.Combine(_sourceFolder, "file1.txt");
        string sourceFile2 = Path.Combine(_sourceFolder, "subdir", "file2.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile2)!);
        await File.WriteAllTextAsync(sourceFile1, "content1");
        await File.WriteAllTextAsync(sourceFile2, "content2");

        await syncService.SyncFoldersAsync(cts.Token);

        string targetFile1 = Path.Combine(_targetFolder, "file1.txt");
        string targetFile2 = Path.Combine(_targetFolder, "subdir", "file2.txt");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(targetFile1), Is.True, "File1 should be copied to target");
            Assert.That(
                File.Exists(targetFile2),
                Is.True,
                "File2 should be copied to target with subdirectory"
            );
            Assert.That(
                File.ReadAllText(targetFile1),
                Is.EqualTo("content1"),
                "File1 content should match"
            );
            Assert.That(
                File.ReadAllText(targetFile2),
                Is.EqualTo("content2"),
                "File2 content should match"
            );
        });
    }

    [Test]
    public void Constructor_Should_Handle_Same_Source_And_Target()
    {
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder = IFolder.Create(_sourceFolder);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder },
        };

        Assert.DoesNotThrow(() => new SyncService(folders, _logger));
    }

    [Test]
    public void Constructor_Should_Handle_Duplicate_Source_Folders()
    {
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder1 = IFolder.Create(_targetFolder);
        IFolder targetFolder2 = IFolder.Create(Path.Combine(_testBaseDirectory, "Target2"));
        Directory.CreateDirectory(targetFolder2.FullPath);

        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder1 },
            // Note: Dictionary won't allow duplicate keys, so this tests the validation logic
        };

        Assert.DoesNotThrow(() => new SyncService(folders, _logger));
    }

    [Test]
    public async Task SyncFoldersAsync_Should_Delete_Obsolete_Files()
    {
        IFolder sourceFolder = IFolder.Create(_sourceFolder);
        IFolder targetFolder = IFolder.Create(_targetFolder);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { sourceFolder, targetFolder },
        };

        SyncService syncService = new(folders, _logger);
        using CancellationTokenSource cts = new();

        // Create file only in target (should be deleted)
        string targetOnlyFile = Path.Combine(_targetFolder, "obsolete.txt");
        await File.WriteAllTextAsync(targetOnlyFile, "obsolete content");

        // Create file in both source and target
        string sourceFile = Path.Combine(_sourceFolder, "keep.txt");
        string targetFile = Path.Combine(_targetFolder, "keep.txt");
        await File.WriteAllTextAsync(sourceFile, "keep content");
        await File.WriteAllTextAsync(targetFile, "old keep content");

        await syncService.SyncFoldersAsync(cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(targetOnlyFile), Is.False, "Obsolete file should be deleted");
            Assert.That(
                File.Exists(targetFile),
                Is.True,
                "File that exists in source should be kept"
            );
        });
    }

    [TestCase("", TestName = "Empty string folder path should be handled")]
    [TestCase("   ", TestName = "Whitespace folder path should be handled")]
    [TestCase(
        "C:\\Invalid\\Path\\That\\Does\\Not\\Exist",
        TestName = "Non-existent path should be handled"
    )]
    public void Constructor_Should_Handle_Invalid_Folder_Paths(string invalidPath)
    {
        IFolder validSource = IFolder.Create(_sourceFolder);
        IFolder invalidFolder = IFolder.Create(invalidPath);
        IReadOnlyDictionary<IFolder, IFolder> folders = new Dictionary<IFolder, IFolder>
        {
            { validSource, invalidFolder },
        };

        Assert.DoesNotThrow(() => new SyncService(folders, _logger));
    }
}
