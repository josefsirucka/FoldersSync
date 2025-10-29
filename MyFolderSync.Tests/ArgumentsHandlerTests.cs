// <copyright file="UnitTest1.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 26.10 2025</summary>

using MyFolderSync.Arguments;
using PerfectResult;

using Serilog.Events;

namespace MyFolderSync.Tests;

/// <summary>
/// All tests for <see cref="ArgumentsHandler"/> class.
/// </summary>
[TestFixture]
public class ArgumentsHandlerTests
{
    [TestCase("", false, null, TestName = "Empty arguments")]
    [TestCase("-f C:\\Source=>C:\\Target", true, 3600, TestName = "Single folder argument")]
    [TestCase("-f C:\\Source=>C:\\Target someFolder/=>f:\\anotherFolder", true, 3600, TestName = "Single folder argument")]
    public void CommandLineProcessingTest(string commandLineArguments, bool expectSuccess, int? interval = null)
    {
        string[] args = commandLineArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        IResult<ArgumentsModel> result = ArgumentsHandler.ProcessArgsAndGetSettings(args);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(expectSuccess));
            
            if (result.Success)
            {
                Assert.That(result.Value.Interval, Is.EqualTo(interval));
                Assert.That(result.Value.LogLevel, Is.EqualTo(LogEventLevel.Information));
                Assert.That(result.Value.LogPath, Is.EqualTo("log/FolderSync.log"));
            }
        });
    }
}
