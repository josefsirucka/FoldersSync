// <copyright file="ExtensionTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using System.Text;
using MyFolderSync.Helpers;
using PerfectResult;

namespace MyFolderSync.Tests.Extensions;

/// <summary>
/// Tests for extension methods.
/// </summary>
[TestFixture]
public class ExtensionTests
{
    private string _testBaseDirectory;
    private IFolder _testFolder;

    [SetUp]
    public void SetUp()
    {
        _testBaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "ExtensionTests",
            Guid.NewGuid().ToString()
        );
        Directory.CreateDirectory(_testBaseDirectory);
        _testFolder = IFolder.Create(_testBaseDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testBaseDirectory))
        {
            try
            {
                Directory.Delete(_testBaseDirectory, recursive: true);
            }
            catch { }
        }
    }

    [TestCase(@"C:\Folder", "file.txt", @"C:\Folder\file.txt", TestName = "Windows absolute path")]
    [TestCase(
        @"C:\Folder\SubFolder",
        "document.pdf",
        @"C:\Folder\SubFolder\document.pdf",
        TestName = "Windows nested path"
    )]
    [TestCase(@"C:\", "root.txt", @"C:\root.txt", TestName = "Root drive path")]
    [TestCase(
        "RelativeFolder",
        "file.txt",
        "RelativeFolder\\file.txt",
        TestName = "Relative folder path"
    )]
    public void GetFullPath_Should_Combine_Folder_And_File_Paths(
        string folderPath,
        string fileName,
        string expectedPath
    )
    {
        IFolder folder = IFolder.Create(folderPath);
        IFile file = IFile.Create(fileName, folder);

        string fullPath = file.GetFullPath();

        Assert.That(
            fullPath,
            Is.EqualTo(expectedPath),
            "GetFullPath should combine folder and file paths correctly"
        );
    }

    [Test]
    public void GetFullPath_Should_Handle_Empty_File_Name()
    {
        IFile file = IFile.Create(string.Empty, _testFolder);

        string fullPath = file.GetFullPath();

        string expectedPath = Path.Combine(_testFolder.FullPath, string.Empty);
        Assert.That(fullPath, Is.EqualTo(expectedPath), "Should handle empty file name gracefully");
    }

    [Test]
    public void GetFullPath_Should_Handle_Empty_Folder_Path()
    {
        IFolder emptyFolder = IFolder.Create(string.Empty);
        IFile file = IFile.Create("test.txt", emptyFolder);

        string fullPath = file.GetFullPath();

        Assert.That(fullPath, Is.EqualTo("test.txt"), "Should handle empty folder path gracefully");
    }

    [Test]
    public async Task CalculateMD5Hash_Should_Return_Correct_Hash_For_Existing_File()
    {
        string fileName = "test.txt";
        string testContent = "Hello, World!";
        string filePath = Path.Combine(_testBaseDirectory, fileName);
        await File.WriteAllTextAsync(filePath, testContent);

        IFile file = IFile.Create(fileName, _testFolder);

        IResult<string> result = file.CalculateMD5Hash();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "Hash calculation should succeed");
            Assert.That(result.Value, Is.Not.Null, "Hash value should not be null");
            Assert.That(
                result.Value,
                Has.Length.EqualTo(32),
                "MD5 hash should be 32 characters long"
            );
            Assert.That(
                result.Value,
                Does.Match("^[0-9a-f]{32}$"),
                "Hash should be lowercase hexadecimal"
            );
        });
    }

    [Test]
    public void CalculateMD5Hash_Should_Return_Failure_For_Non_Existing_File()
    {
        IFile file = IFile.Create("nonexistent.txt", _testFolder);

        IResult<string> result = file.CalculateMD5Hash();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Success,
                Is.False,
                "Hash calculation should fail for non-existing file"
            );
            Assert.That(
                result.Message,
                Does.Contain("Error computing MD5 hash"),
                "Error message should indicate hash computation failure"
            );
        });
    }

    [Test]
    public async Task CalculateMD5Hash_Should_Return_Same_Hash_For_Same_Content()
    {
        string testContent = "Identical content for hash testing";

        string file1Path = Path.Combine(_testBaseDirectory, "file1.txt");
        string file2Path = Path.Combine(_testBaseDirectory, "file2.txt");

        await File.WriteAllTextAsync(file1Path, testContent);
        await File.WriteAllTextAsync(file2Path, testContent);

        IFile file1 = IFile.Create("file1.txt", _testFolder);
        IFile file2 = IFile.Create("file2.txt", _testFolder);

        IResult<string> hash1 = file1.CalculateMD5Hash();
        IResult<string> hash2 = file2.CalculateMD5Hash();

        Assert.Multiple(() =>
        {
            Assert.That(hash1.Success, Is.True, "First hash calculation should succeed");
            Assert.That(hash2.Success, Is.True, "Second hash calculation should succeed");
            Assert.That(
                hash1.Value,
                Is.EqualTo(hash2.Value),
                "Same content should produce same hash"
            );
        });
    }

    [Test]
    public async Task CalculateMD5Hash_Should_Return_Different_Hash_For_Different_Content()
    {
        string content1 = "Content for first file";
        string content2 = "Different content for second file";

        string file1Path = Path.Combine(_testBaseDirectory, "file1.txt");
        string file2Path = Path.Combine(_testBaseDirectory, "file2.txt");

        await File.WriteAllTextAsync(file1Path, content1);
        await File.WriteAllTextAsync(file2Path, content2);

        IFile file1 = IFile.Create("file1.txt", _testFolder);
        IFile file2 = IFile.Create("file2.txt", _testFolder);

        IResult<string> hash1 = file1.CalculateMD5Hash();
        IResult<string> hash2 = file2.CalculateMD5Hash();

        Assert.Multiple(() =>
        {
            Assert.That(hash1.Success, Is.True, "First hash calculation should succeed");
            Assert.That(hash2.Success, Is.True, "Second hash calculation should succeed");
            Assert.That(
                hash1.Value,
                Is.Not.EqualTo(hash2.Value),
                "Different content should produce different hashes"
            );
        });
    }

    [Test]
    public async Task CalculateMD5Hash_Should_Handle_Empty_File()
    {
        string fileName = "empty.txt";
        string filePath = Path.Combine(_testBaseDirectory, fileName);
        await File.WriteAllTextAsync(filePath, string.Empty);

        IFile file = IFile.Create(fileName, _testFolder);

        IResult<string> result = file.CalculateMD5Hash();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "Hash calculation should succeed for empty file");
            Assert.That(
                result.Value,
                Is.EqualTo("d41d8cd98f00b204e9800998ecf8427e"),
                "Empty file should have known MD5 hash"
            );
        });
    }

    [TestCase("single-byte.txt", "A", TestName = "Single byte file")]
    [TestCase("multi-byte.txt", "ABC123", TestName = "Multi-byte file")]
    [TestCase("unicode.txt", "🙂👍", TestName = "Unicode content file")]
    public async Task CalculateMD5Hash_Should_Return_Consistent_Hash_For_Known_Content(
        string fileName,
        string content
    )
    {
        string filePath = Path.Combine(_testBaseDirectory, fileName);
        await File.WriteAllTextAsync(filePath, content);

        IFile file = IFile.Create(fileName, _testFolder);

        IResult<string> result = file.CalculateMD5Hash();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "Hash calculation should succeed");
            Assert.That(result.Value, Is.Not.Null, $"Hash for '{content}' should not be null");
            Assert.That(
                result.Value,
                Has.Length.EqualTo(32),
                "MD5 hash should be 32 characters long"
            );
        });
    }

    [Test]
    public async Task CalculateMD5Hash_Should_Handle_Large_File()
    {
        // Arrange
        string fileName = "large.txt";
        string filePath = Path.Combine(_testBaseDirectory, fileName);

        // Create a file with some content (not too large for CI/test environments)
        StringBuilder largeContent = new();
        for (int i = 0; i < 1000; i++)
        {
            largeContent.AppendLine($"Line {i} with some content to make the file larger");
        }

        await File.WriteAllTextAsync(filePath, largeContent.ToString());

        IFile file = IFile.Create(fileName, _testFolder);

        // Act
        IResult<string> result = file.CalculateMD5Hash();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "Hash calculation should succeed for large file");
            Assert.That(result.Value, Is.Not.Null, "Hash value should not be null");
            Assert.That(
                result.Value,
                Has.Length.EqualTo(32),
                "MD5 hash should be 32 characters long"
            );
        });
    }
}
