// <copyright file="ArgumentsHandler.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 23.10 2025</summary>

using MyFolderSync.Helpers;
using PerfectResult;

namespace MyFolderSync.Arguments;

/// <summary>
/// Arguments Handler class.
/// </summary>
public class ArgumentsHandler
{
    private ArgumentsModel _argumentsModel;
    private IResult _validationResult;

    /// <summary>
    /// Processes the arguments and gets the settings.
    /// </summary>
    /// <param name="args">Arguments passed from program.cs.</param>
    /// <returns>Processed model.</returns>
    public static IResult<ArgumentsModel> ProcessArgsAndGetSettings(string[] args)
    {
        ArgumentsHandler handler = new(args);

        if (!handler._validationResult.Success)
        {
            return IResult.FailureResult<ArgumentsModel>(handler._validationResult.Message);
        }

        return IResult.SuccessResult(handler._argumentsModel);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentsHandler"/> class.
    /// </summary>
    /// <param name="args">Arguments passed from program.cs.</param>
    private ArgumentsHandler(string[] args)
    {
        Parser
            .Default.ParseArguments<ArgumentsModel>(args)
            .WithParsed(model =>
            {
                _argumentsModel = model;
            });

        _validationResult = FinalChecks();
        _argumentsModel ??= new ArgumentsModel();

        _argumentsModel.ResolvedFolders = ParseFolderMappings(_argumentsModel.Folders);
    }

    private IResult FinalChecks()
    {
        if (_argumentsModel == null)
        {
            return IResult.FailureResult("Failed to parse command line arguments!");
        }

        if (_argumentsModel.Folders.Count() < 1)
        {
            return IResult.FailureResult(
                "At least one folder must be specified for synchronization!"
            );
        }

        if (_argumentsModel.Interval < 1)
        {
            return IResult.FailureResult("The synchronization interval must be at least 1 second!");
        }

        return IResult.SuccessResult();
    }

    private IReadOnlyDictionary<IFolder, IFolder> ParseFolderMappings(
        IEnumerable<string> allFolders
    )
    {
        Dictionary<IFolder, IFolder> mappings = [];

        foreach (string pair in allFolders)
        {
            string[] pairs = pair.Split("=>", StringSplitOptions.RemoveEmptyEntries);
            if (pairs.Length == 2)
            {
                IFolder source = IFolder.Create(pairs[0]);
                IFolder target = IFolder.Create(pairs[1]);
                mappings[source] = target;
            }
        }

        return mappings;
    }
}
