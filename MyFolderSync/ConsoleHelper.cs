// <copyright file="ConsoleHelper.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 23.10 2025</summary>


namespace MyFolderSync;

/// <summary>
/// Little helper for console output.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Writes an info message to the console.
    /// </summary>
    /// <param name="message">Error message to be written to the console.</param>
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    internal static void WriteHeader()
    {
        Console.WriteLine("MyFolderSync - Folder Synchronization Tool");
        Console.WriteLine("===========================================");
    }
}
