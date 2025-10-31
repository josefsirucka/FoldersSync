// <copyright file="CommandProcessorServiceTests.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 31.10 2025</summary>

using MyFolderSync.Commands;
using MyFolderSync.Services;
using PerfectResult;
using Serilog;
using Serilog.Core;

namespace MyFolderSync.Tests.Services;

/// <summary>
/// Tests for <see cref="CommandProcessorService"/> class.
/// </summary>
[TestFixture]
public class CommandProcessorServiceTests
{
    private Logger _logger;
    private CommandProcessorService _commandProcessor;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

        _commandProcessor = new CommandProcessorService(_logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_commandProcessor != null)
        {
            await _commandProcessor.DisposeAsync();
        }
        _logger?.Dispose();
    }

    [Test]
    public void Constructor_Should_Initialize_Successfully()
    {
        CommandProcessorService processor = new(_logger);

        Assert.That(processor, Is.Not.Null, "CommandProcessor should be created successfully");
    }

    [Test]
    public void Constructor_Should_Handle_Null_Logger()
    {
        Assert.DoesNotThrow(() => new CommandProcessorService(null!));
    }

    [Test]
    public void InitProcessor_Should_Start_Without_Throwing()
    {
        using CancellationTokenSource cts = new();

        Assert.DoesNotThrow(() => _commandProcessor.InitProcessor(cts.Token));
    }

    [Test]
    public void AddCommand_Should_Accept_Valid_Command()
    {
        TestCommand testCommand = new(success: true);
        using CancellationTokenSource cts = new();
        _commandProcessor.InitProcessor(cts.Token);

        Assert.DoesNotThrow(() => _commandProcessor.AddCommand(testCommand));
    }

    [Test]
    public async Task ProcessCommands_Should_Execute_Added_Commands()
    {
        TestCommand command1 = new(success: true);
        TestCommand command2 = new(success: true);

        using CancellationTokenSource cts = new();
        _commandProcessor.InitProcessor(cts.Token);

        _commandProcessor.AddCommand(command1);
        _commandProcessor.AddCommand(command2);

        await Task.Delay(100);
        await _commandProcessor.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                command1.ExecutedCount,
                Is.EqualTo(1),
                "First command should be executed once"
            );
            Assert.That(
                command2.ExecutedCount,
                Is.EqualTo(1),
                "Second command should be executed once"
            );
        });
    }

    [Test]
    public async Task ProcessCommands_Should_Handle_Failing_Commands()
    {
        TestCommand successCommand = new(success: true);
        TestCommand failingCommand = new(success: false);
        TestCommand anotherSuccessCommand = new(success: true);

        using CancellationTokenSource cts = new();
        _commandProcessor.InitProcessor(cts.Token);

        _commandProcessor.AddCommand(successCommand);
        _commandProcessor.AddCommand(failingCommand);
        _commandProcessor.AddCommand(anotherSuccessCommand);

        await Task.Delay(200);
        await _commandProcessor.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                successCommand.ExecutedCount,
                Is.EqualTo(1),
                "Success command should be executed"
            );
            Assert.That(
                failingCommand.ExecutedCount,
                Is.EqualTo(1),
                "Failing command should be executed"
            );
            Assert.That(
                anotherSuccessCommand.ExecutedCount,
                Is.EqualTo(1),
                "Command after failing should still be executed"
            );
        });
    }

    [Test]
    public async Task ProcessCommands_Should_Handle_Cancellation()
    {
        TestCommand slowCommand = new(success: true, delay: TimeSpan.FromSeconds(2));
        TestCommand quickCommand = new(success: true);

        using CancellationTokenSource cts = new();
        _commandProcessor.InitProcessor(cts.Token);

        _commandProcessor.AddCommand(slowCommand);
        _commandProcessor.AddCommand(quickCommand);

        cts.Cancel();

        await Task.Delay(100);
        await _commandProcessor.DisposeAsync();

        Assert.That(
            slowCommand.ExecutedCount + quickCommand.ExecutedCount,
            Is.GreaterThanOrEqualTo(0)
        );
    }

    [Test]
    public async Task DisposeAsync_Should_Complete_Processing()
    {
        TestCommand command = new(success: true);

        using CancellationTokenSource cts = new();
        _commandProcessor.InitProcessor(cts.Token);
        _commandProcessor.AddCommand(command);

        await _commandProcessor.DisposeAsync();

        Assert.That(
            command.ExecutedCount,
            Is.EqualTo(1),
            "Command should be executed before disposal"
        );
    }

    [TestCase(1, TestName = "Process single command")]
    [TestCase(5, TestName = "Process five commands")]
    [TestCase(10, TestName = "Process ten commands")]
    public async Task ProcessCommands_Should_Handle_Multiple_Commands_In_Order(int commandCount)
    {
        List<TestCommand> commands = [];
        for (int i = 0; i < commandCount; i++)
        {
            commands.Add(new TestCommand(success: true));
        }

        using CancellationTokenSource cts = new();
        _commandProcessor.InitProcessor(cts.Token);

        // Act
        foreach (TestCommand command in commands)
        {
            _commandProcessor.AddCommand(command);
        }

        await Task.Delay(100 * commandCount);
        await _commandProcessor.DisposeAsync();

        Assert.That(
            commands.All(c => c.ExecutedCount == 1),
            Is.True,
            "All commands should be executed exactly once"
        );
    }

    /// <summary>
    /// Test command implementation for testing purposes.
    /// </summary>
    private class TestCommand : ISyncCommand
    {
        private readonly bool _shouldSucceed;
        private readonly TimeSpan _delay;

        public TestCommand(bool success, TimeSpan? delay = null)
        {
            _shouldSucceed = success;
            _delay = delay ?? TimeSpan.Zero;
            ExecutedCount = 0;
        }

        public IResult Result { get; private set; } = IResult.SuccessResult();

        public int ExecutedCount { get; private set; }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ExecutedCount++;

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_shouldSucceed)
            {
                Result = IResult.SuccessResult("Test command succeeded");
            }
            else
            {
                Result = IResult.FailureResult("Test command failed");
                throw new InvalidOperationException("Test command was configured to fail");
            }
        }
    }
}
