// <copyright file="FileInfoBaseTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Helpers;

namespace MyFolderSync.Tests.Helpers;

/// <summary>
/// Tests for <see cref="FileInfoBase"/> class.
/// </summary>
[TestFixture]
public class FileInfoBaseTests
{
    [TestCase(0L, TestName = "Zero file size")]
    [TestCase(1L, TestName = "One byte file size")]
    [TestCase(1024L, TestName = "1KB file size")]
    [TestCase(1048576L, TestName = "1MB file size")]
    [TestCase(1073741824L, TestName = "1GB file size")]
    [TestCase(long.MaxValue, TestName = "Maximum file size")]
    public void Constructor_Should_Initialize_With_Various_File_Sizes(long fileSize)
    {
        DateTime lastModified = DateTime.UtcNow;

        FileInfoBase fileInfo = new(fileSize, lastModified);

        Assert.Multiple(() =>
        {
            Assert.That(
                fileInfo.FileSize,
                Is.EqualTo(fileSize),
                "FileSize should match constructor parameter"
            );
            Assert.That(
                fileInfo.LastModified,
                Is.EqualTo(lastModified),
                "LastModified should match constructor parameter"
            );
        });
    }

    [Test]
    public void Constructor_Should_Handle_Negative_File_Size()
    {
        long negativeSize = -1L;
        DateTime lastModified = DateTime.UtcNow;

        FileInfoBase fileInfo = new(negativeSize, lastModified);

        Assert.That(
            fileInfo.FileSize,
            Is.EqualTo(negativeSize),
            "Should handle negative file size (validation is caller's responsibility)"
        );
    }

    [TestCase("2025-01-01 00:00:00", TestName = "New Year 2025")]
    [TestCase("2024-12-31 23:59:59", TestName = "End of 2024")]
    [TestCase("1970-01-01 00:00:00", TestName = "Unix epoch")]
    [TestCase("2025-10-31 12:30:45", TestName = "Current date test")]
    public void Constructor_Should_Initialize_With_Various_Dates(string dateTimeString)
    {
        DateTime lastModified = DateTime.Parse(dateTimeString);
        long fileSize = 1024L;

        FileInfoBase fileInfo = new(fileSize, lastModified);

        Assert.That(
            fileInfo.LastModified,
            Is.EqualTo(lastModified),
            "LastModified should match constructor parameter"
        );
    }

    [Test]
    public void Constructor_Should_Handle_DateTime_MinValue()
    {
        DateTime minDateTime = DateTime.MinValue;
        long fileSize = 100L;

        FileInfoBase fileInfo = new(fileSize, minDateTime);

        Assert.That(
            fileInfo.LastModified,
            Is.EqualTo(minDateTime),
            "Should handle DateTime.MinValue"
        );
    }

    [Test]
    public void Constructor_Should_Handle_DateTime_MaxValue()
    {
        DateTime maxDateTime = DateTime.MaxValue;

        FileInfoBase fileInfo = new(maxDateTime.Ticks, maxDateTime);

        Assert.That(
            fileInfo.LastModified,
            Is.EqualTo(maxDateTime),
            "Should handle DateTime.MaxValue"
        );
    }

    [Test]
    public void Properties_Should_Be_Immutable_After_Construction()
    {
        long originalSize = 2048L;
        DateTime originalDate = DateTime.UtcNow;
        FileInfoBase fileInfo = new(originalSize, originalDate);

        Assert.Multiple(() =>
        {
            Assert.That(fileInfo.FileSize, Is.EqualTo(originalSize), "FileSize should not change");
            Assert.That(
                fileInfo.LastModified,
                Is.EqualTo(originalDate),
                "LastModified should not change"
            );
        });
    }

    [Test]
    public void Multiple_FileInfo_Instances_Should_Be_Independent()
    {
        long size1 = 1000L;
        long size2 = 2000L;
        DateTime date1 = DateTime.UtcNow;
        DateTime date2 = DateTime.UtcNow.AddDays(-1);

        FileInfoBase fileInfo1 = new(size1, date1);
        FileInfoBase fileInfo2 = new(size2, date2);

        Assert.Multiple(() =>
        {
            Assert.That(
                fileInfo1.FileSize,
                Is.EqualTo(size1),
                "First instance should have its own size"
            );
            Assert.That(
                fileInfo2.FileSize,
                Is.EqualTo(size2),
                "Second instance should have its own size"
            );
            Assert.That(
                fileInfo1.LastModified,
                Is.EqualTo(date1),
                "First instance should have its own date"
            );
            Assert.That(
                fileInfo2.LastModified,
                Is.EqualTo(date2),
                "Second instance should have its own date"
            );
        });
    }

    [TestCase(1024L, 1024L, true, TestName = "Same file sizes should be equal")]
    [TestCase(1000L, 2000L, false, TestName = "Different file sizes should not be equal")]
    [TestCase(0L, 0L, true, TestName = "Zero sizes should be equal")]
    public void FileSize_Comparison_Should_Work_Correctly(
        long size1,
        long size2,
        bool shouldBeEqual
    )
    {
        DateTime sameDate = DateTime.UtcNow;
        FileInfoBase fileInfo1 = new(size1, sameDate);
        FileInfoBase fileInfo2 = new(size2, sameDate);

        bool sizesEqual = fileInfo1.FileSize == fileInfo2.FileSize;

        Assert.That(
            sizesEqual,
            Is.EqualTo(shouldBeEqual),
            $"FileSize comparison result should be {shouldBeEqual}"
        );
    }

    [Test]
    public void LastModified_Comparison_Should_Work_Correctly()
    {
        DateTime baseDate = DateTime.UtcNow;
        DateTime sameDate = new(baseDate.Ticks);
        DateTime differentDate = baseDate.AddMinutes(1);

        FileInfoBase fileInfo1 = new(100L, baseDate);
        FileInfoBase fileInfo2 = new(100L, sameDate);
        FileInfoBase fileInfo3 = new(100L, differentDate);

        Assert.Multiple(() =>
        {
            Assert.That(
                fileInfo1.LastModified,
                Is.EqualTo(fileInfo2.LastModified),
                "Same dates should be equal"
            );
            Assert.That(
                fileInfo1.LastModified,
                Is.Not.EqualTo(fileInfo3.LastModified),
                "Different dates should not be equal"
            );
            Assert.That(
                fileInfo1.LastModified,
                Is.LessThan(fileInfo3.LastModified),
                "Earlier date should be less than later date"
            );
        });
    }

    [Test]
    public void Constructor_Should_Work_With_UTC_And_Local_Times()
    {
        DateTime utcTime = DateTime.UtcNow;
        DateTime localTime = DateTime.Now;
        long fileSize = 512L;

        FileInfoBase utcFileInfo = new(fileSize, utcTime);
        FileInfoBase localFileInfo = new(fileSize, localTime);

        Assert.Multiple(() =>
        {
            Assert.That(utcFileInfo.LastModified, Is.EqualTo(utcTime), "Should preserve UTC time");
            Assert.That(
                localFileInfo.LastModified,
                Is.EqualTo(localTime),
                "Should preserve local time"
            );
            Assert.That(
                utcFileInfo.LastModified.Kind,
                Is.EqualTo(utcTime.Kind),
                "Should preserve DateTime kind"
            );
            Assert.That(
                localFileInfo.LastModified.Kind,
                Is.EqualTo(localTime.Kind),
                "Should preserve DateTime kind"
            );
        });
    }
}
