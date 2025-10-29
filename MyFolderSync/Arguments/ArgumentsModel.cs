// <copyright file="ArgumentsModel.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 23.10 2025</summary>

using MyFolderSync.Helpers;
using Serilog.Events;

namespace MyFolderSync.Arguments;

/// <summary>
/// Arguments Model class for settings.
/// </summary>
public class ArgumentsModel
{
    /// <summary>
    /// Gets the log level.
    /// </summary>
    [Option(
        'l',
        "level",
        Required = false,
        HelpText = "Overrides the log level (default level is Information, other values are Debug, Warning, Error, Fatal)."
    )]
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Gets the log path.
    /// </summary>
    [Option(
        'p',
        "path",
        Required = false,
        HelpText = "Overrides the default log path and file name (default is log/FolderSync.log)."
    )]
    public string LogPath { get; set; } = "log/FolderSync.log";

    /// <summary>
    /// Gets the synchronisation interval.
    /// </summary>
    [Option(
        'i',
        "interval",
        Required = false,
        HelpText = "Overrides the synchronisation interval setting (in seconds). Default value is one hour (3600 seconds)."
    )]
    public int Interval { get; set; } = 3600;

    /// <summary>
    /// Gets the list of folders to process.
    /// </summary>
    [Option(
        'f',
        "folders",
        Required = true,
        HelpText = "List of folder(s) for sync. Format: -f source=>target -f source2=>target2 ..."
    )]
    public IEnumerable<string> Folders { get; set; } = [];

    /// <summary>
    /// Gets or sets the resolved folders dictionary.
    /// </summary>
    public IReadOnlyDictionary<IFolder, IFolder> ResolvedFolders { get; set; } =
        new Dictionary<IFolder, IFolder>();
}
