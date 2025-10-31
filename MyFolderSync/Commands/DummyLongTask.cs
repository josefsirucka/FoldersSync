// <copyright file="DummyLongTask.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 29.10 2025</summary>


using Serilog;

namespace MyFolderSync.Commands;

/// <summary>
/// A dummy long task command for testing purposes.
/// </summary>
public sealed class DummyLongTaskCommand : SyncCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DummyLongTaskCommand"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public DummyLongTaskCommand(ILogger logger)
        : base(logger) { }

    /// <inheritdoc/>
    public override Task CommandExecutionImplementation(CancellationToken cancellationToken)
    {
        return Task.Delay(5000, cancellationToken);
    }
}
