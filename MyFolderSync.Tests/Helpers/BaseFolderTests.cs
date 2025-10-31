// <copyright file="BaseFolderTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Helpers;

namespace MyFolderSync.Tests.Helpers;

/// <summary>
/// Tests for <see cref="BaseFolder"/> class.
/// </summary>
[TestFixture]
public class BaseFolderTests
{
    [TestCase(@"C:\TestFolder", TestName = "Windows absolute path")]
    [TestCase(@"D:\Program Files\MyApp", TestName = "Windows path with spaces")]
    [TestCase(@"C:\", TestName = "Root drive path")]
    [TestCase(@"\\server\share\folder", TestName = "UNC network path")]
    [TestCase("RelativeFolder", TestName = "Relative folder path")]
    [TestCase(@"C:\Folder-With-Hyphens", TestName = "Folder with hyphens")]
    [TestCase(@"C:\Folder_With_Underscores", TestName = "Folder with underscores")]
    public void Constructor_Should_Initialize_With_Various_Paths(string folderPath)
    {
        BaseFolder folder = new(folderPath);

        Assert.That(
            folder.FullPath,
            Is.EqualTo(folderPath),
            "FullPath should match constructor parameter"
        );
    }

    [Test]
    public void Constructor_Should_Handle_Empty_Path()
    {
        BaseFolder folder = new(string.Empty);

        Assert.That(folder.FullPath, Is.EqualTo(string.Empty), "Empty path should be handled");
    }

    [Test]
    public void Constructor_Should_Handle_Null_Path()
    {
        BaseFolder folder = new(null!);

        Assert.That(folder.FullPath, Is.Null, "Null path should be handled");
    }

    [Test]
    public void IFolder_Create_Should_Return_BaseFolder_Instance()
    {
        string folderPath = @"C:\CreatedFolder";

        IFolder folder = IFolder.Create(folderPath);

        Assert.Multiple(() =>
        {
            Assert.That(folder, Is.TypeOf<BaseFolder>(), "Should return BaseFolder instance");
            Assert.That(folder.FullPath, Is.EqualTo(folderPath), "FullPath should match parameter");
        });
    }

    [Test]
    public void FullPath_Property_Should_Be_Immutable()
    {
        string originalPath = @"C:\ImmutableFolder";
        BaseFolder folder = new(originalPath);

        Assert.That(
            folder.FullPath,
            Is.EqualTo(originalPath),
            "FullPath should not change after construction"
        );
    }

    [Test]
    public void Multiple_Folders_Should_Be_Independent()
    {
        string path1 = @"C:\Folder1";
        string path2 = @"C:\Folder2";

        BaseFolder folder1 = new(path1);
        BaseFolder folder2 = new(path2);

        Assert.Multiple(() =>
        {
            Assert.That(folder1.FullPath, Is.EqualTo(path1), "Folder1 should have its own path");
            Assert.That(folder2.FullPath, Is.EqualTo(path2), "Folder2 should have its own path");
            Assert.That(
                folder1.FullPath,
                Is.Not.EqualTo(folder2.FullPath),
                "Folders should be independent"
            );
        });
    }

    [TestCase(@"C:\TestFolder", @"C:\TestFolder", true, TestName = "Same paths should be equal")]
    [TestCase(
        @"C:\Folder1",
        @"C:\Folder2",
        false,
        TestName = "Different paths should not be equal"
    )]
    [TestCase("", "", true, TestName = "Empty paths should be equal")]
    public void Folders_Equality_Should_Be_Based_On_Path_Content(
        string path1,
        string path2,
        bool shouldBeEqual
    )
    {
        BaseFolder folder1 = new(path1);
        BaseFolder folder2 = new(path2);

        bool pathsEqual = folder1.FullPath == folder2.FullPath;

        Assert.That(
            pathsEqual,
            Is.EqualTo(shouldBeEqual),
            $"Paths '{path1}' and '{path2}' equality should be {shouldBeEqual}"
        );
    }

    [TestCase(
        @"C:\TestFolder\",
        @"C:\TestFolder",
        TestName = "Path with trailing slash vs without"
    )]
    [TestCase(@"C:\TESTFOLDER", @"C:\testfolder", TestName = "Case differences in paths")]
    public void Folders_Should_Preserve_Exact_Path_Format(
        string originalPath,
        string comparisonPath
    )
    {
        BaseFolder folder = new(originalPath);

        Assert.That(folder.FullPath, Is.EqualTo(originalPath), "Should preserve exact path format");
        Assert.That(
            folder.FullPath,
            Is.Not.EqualTo(comparisonPath),
            "Should not normalize path automatically"
        );
    }

    [Test]
    public void Folder_Should_Work_As_Dictionary_Key()
    {
        BaseFolder folder1 = new(@"C:\Key1");
        BaseFolder folder2 = new(@"C:\Key2");
        BaseFolder folder3 = new(@"C:\Key1");

        Dictionary<IFolder, string> folderDictionary = new()
        {
            { folder1, "Value1" },
            { folder2, "Value2" },
        };

        Assert.Multiple(() =>
        {
            Assert.That(folderDictionary.ContainsKey(folder1), Is.True, "Should contain folder1");
            Assert.That(folderDictionary.ContainsKey(folder2), Is.True, "Should contain folder2");
            Assert.That(
                folderDictionary[folder1],
                Is.EqualTo("Value1"),
                "Should retrieve correct value for folder1"
            );
            Assert.That(
                folderDictionary[folder2],
                Is.EqualTo("Value2"),
                "Should retrieve correct value for folder2"
            );
        });
    }
}
