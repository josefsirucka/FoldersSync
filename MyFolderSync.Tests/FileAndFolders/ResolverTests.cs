// <copyright file="ResolverTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 27.10 2025</summary>

using MyFolderSync.Helpers;

using PerfectResult;

namespace MyFolderSync.Tests.FileAndFolders;

/// <summary>
/// All tests for <see cref="Resolver"/> class.
/// </summary>
[TestFixture]
public class FileResolverTests
{
    private Resolver _resolver;
    private static string _basePath = AppContext.BaseDirectory;

    // Use the current drive letter for absolute paths in tests (WIP)
    private static string _drive = Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:\\";

    [SetUp]
    public void SetUp()
    {
        //Directory.CreateDirectory("C:\\temp");
        _resolver = new Resolver();
    }

    // Basic test cases:
    [TestCase("", "default.log", false, "", false, TestName = "Empty folder should fail")]
    [TestCase(":c:", "default.log", false, "", false, TestName = "Invalid path should fail")]

    // No default file name provided:
    [TestCase("c:/", null, false, "", false, TestName = "Path to existing folder without default file name should fail")]
    [TestCase("C:\\", null, false, "", false, TestName = "Path to existing folder without default file name should fail")]

    // Default file name provided && existing folder:
    [TestCase("c:", "default.log", true, "C:\\", false, TestName = "Path to drive only with default file name should succeed")]
    [TestCase("c:/", "default.log", true, "c:\\", false, TestName = "Path to existing folder with default file name should succeed")]
    [TestCase("c:\\", "default.log", true, "c:\\", false, TestName = "Path to existing folder with default file name should succeed")]
    [TestCase("c:/default.log", null, true, "c:\\", false, TestName = "Path to non existing file, but with extension and no default file name should succeed")]

    // Default file name provided && relative folder provided:
    [TestCase("some/relative/folder", "default.log", true, "some\\relative\\folder", true, TestName = "Relative folder with default file name should succeed")]
    [TestCase("some\\relative\\folder/default.log", null, true, "some\\relative\\folder", true, TestName = "Relative folder with file and no default file name should succeed")]

    // File name with extension only:
    [TestCase("some/.onlyextension", null, true, "some", true, TestName = "Relative folder with file with only extension and no default file name should succeed")]
    [TestCase("some/", ".onlyextension", true, "some", true, TestName = "Relative folder with file with only extension and default file name should succeed")]

    // Absolute path with file name only as extension:
    [TestCase("c:/temp", ".onlyextension", true, "c:\\temp", true, TestName = "Absolute folder and default file as extension only name should succeed")]
    [TestCase("c:/temp/.onlyextension", null, true, "c:\\temp", true, TestName = "Absolute folder with extension only and no default file name should succeed")]
    [TestCase("c:/temp/", ".onlyextension", true, "c:\\temp", true, TestName = "Absolute folder and file with only extension and default file name should succeed (With slash)")]

    [TestCase("/temp", "log.txt", true, "temp", false, TestName = "Unix style absolute folder with default file name should succeed")]
    [TestCase("\\temp", "log.txt", true, "temp", false, TestName = "Unix style absolute folder with default file name should succeed")]

    public void ResolveFileName_Should_Handle_Various_Paths(string folder, string? defaultFileName, bool expectSuccess, string? expectedFolder, bool relativePath)
    {
        IResult<IFile> folderResult = _resolver.ResolveFileName(
                folder,
                defaultFileName
        );

        Assert.That(folderResult.Success, Is.EqualTo(expectSuccess));
        if (folderResult.Success)
        {
            if (relativePath && !string.IsNullOrEmpty(expectedFolder))
            {
                expectedFolder = Path.Combine(_basePath, expectedFolder);
            }

            Assert.That(folderResult.Value.Folder.FullPath, Is.EqualTo(expectedFolder));
        }
    }
}


