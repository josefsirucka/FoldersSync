// <copyright file="BaseFileTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Helpers;

namespace MyFolderSync.Tests.Helpers;

/// <summary>
/// Tests for <see cref="FileBase"/> class.
/// </summary>
[TestFixture]
public class BaseFileTests
{
    private IFolder _testFolder;

    [SetUp]
    public void SetUp()
    {
        _testFolder = IFolder.Create(@"C:\TestFolder");
    }

    [TestCase("test.txt", TestName = "Simple file name")]
    [TestCase("document.pdf", TestName = "File with different extension")]
    [TestCase("file-with-hyphens.txt", TestName = "File name with hyphens")]
    [TestCase("file_with_underscores.log", TestName = "File name with underscores")]
    [TestCase("file with spaces.doc", TestName = "File name with spaces")]
    [TestCase(".hidden", TestName = "Hidden file starting with dot")]
    [TestCase("config.json", TestName = "Configuration file")]
    public void Constructor_Should_Initialize_With_Various_File_Names(string fileName)
    {
        FileBase file = new(fileName, _testFolder);

        Assert.Multiple(() =>
        {
            Assert.That(
                file.Name,
                Is.EqualTo(fileName),
                "File name should match constructor parameter"
            );
            Assert.That(
                file.Folder,
                Is.EqualTo(_testFolder),
                "Folder should match constructor parameter"
            );
            Assert.That(file.FileInfo, Is.Null, "FileInfo should be null initially");
            Assert.That(file.RelativePath, Is.Null, "RelativePath should be null initially");
        });
    }

    [Test]
    public void Constructor_Should_Handle_Empty_File_Name()
    {
        FileBase file = new(string.Empty, _testFolder);

        Assert.That(file.Name, Is.EqualTo(string.Empty), "Empty file name should be handled");
    }

    [Test]
    public void Constructor_Should_Handle_Null_Folder()
    {
        Assert.DoesNotThrow(() => new FileBase("test.txt", null!));
    }

    [Test]
    public void FileInfo_Property_Should_Be_Settable()
    {
        FileBase file = new("test.txt", _testFolder);
        FileInfoBase fileInfo = new(1024, DateTime.UtcNow);

        file.FileInfo = fileInfo;

        Assert.That(file.FileInfo, Is.EqualTo(fileInfo), "FileInfo should be settable");
    }

    [Test]
    public void RelativePath_Property_Should_Be_Settable()
    {
        FileBase file = new("test.txt", _testFolder);
        string relativePath = "subfolder\\nested";

        file.RelativePath = relativePath;

        Assert.That(file.RelativePath, Is.EqualTo(relativePath), "RelativePath should be settable");
    }

    [TestCase(@"C:\Folder", "file.txt", @"C:\Folder\file.txt", TestName = "Windows path")]
    [TestCase(
        @"C:\Folder\SubFolder",
        "document.pdf",
        @"C:\Folder\SubFolder\document.pdf",
        TestName = "Windows nested path"
    )]
    [TestCase(@"D:\", "root.txt", @"D:\root.txt", TestName = "Root drive path")]
    public void ToString_Should_Return_Combined_Path(
        string folderPath,
        string fileName,
        string expectedResult
    )
    {
        IFolder folder = IFolder.Create(folderPath);
        FileBase file = new(fileName, folder);

        string result = file.ToString();

        Assert.That(
            result,
            Is.EqualTo(expectedResult),
            "ToString should return combined folder and file path"
        );
    }

    [Test]
    public void IFile_Create_Should_Return_FileBase_Instance()
    {
        string fileName = "created.txt";

        IFile file = IFile.Create(fileName, _testFolder);

        Assert.Multiple(() =>
        {
            Assert.That(file, Is.TypeOf<FileBase>(), "Should return FileBase instance");
            Assert.That(file.Name, Is.EqualTo(fileName), "Name should match parameter");
            Assert.That(file.Folder, Is.EqualTo(_testFolder), "Folder should match parameter");
        });
    }

    [Test]
    public void Properties_Should_Be_Immutable_After_Construction()
    {
        string fileName = "immutable.txt";
        FileBase file = new(fileName, _testFolder);

        Assert.Multiple(() =>
        {
            Assert.That(file.Name, Is.EqualTo(fileName), "Name should not change");
            Assert.That(file.Folder, Is.EqualTo(_testFolder), "Folder should not change");
        });
    }

    [Test]
    public void Multiple_Files_In_Same_Folder_Should_Be_Independent()
    {
        FileBase file1 = new("file1.txt", _testFolder);
        FileBase file2 = new("file2.txt", _testFolder);
        FileInfoBase fileInfo1 = new(100, DateTime.UtcNow);
        FileInfoBase fileInfo2 = new(200, DateTime.UtcNow.AddDays(-1));

        file1.FileInfo = fileInfo1;
        file2.FileInfo = fileInfo2;
        file1.RelativePath = "path1";
        file2.RelativePath = "path2";

        Assert.Multiple(() =>
        {
            Assert.That(
                file1.FileInfo,
                Is.EqualTo(fileInfo1),
                "File1 should have its own FileInfo"
            );
            Assert.That(
                file2.FileInfo,
                Is.EqualTo(fileInfo2),
                "File2 should have its own FileInfo"
            );
            Assert.That(
                file1.RelativePath,
                Is.EqualTo("path1"),
                "File1 should have its own RelativePath"
            );
            Assert.That(
                file2.RelativePath,
                Is.EqualTo("path2"),
                "File2 should have its own RelativePath"
            );
            Assert.That(
                file1.Folder,
                Is.EqualTo(file2.Folder),
                "Both files should share the same folder"
            );
        });
    }
}
