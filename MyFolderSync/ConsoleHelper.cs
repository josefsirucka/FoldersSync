// <copyright file="ConsoleHelper.cs" company="Josef Širůčka">
// Copyright (c) Josef Širůčka. All rights reserved.
// </copyright>
// <summary>Created on: 23.10 2025</summary>


using System.Text;

namespace MyFolderSync;

/// <summary>
/// Little helper for console output.
/// </summary>
public static class ConsoleHelper
{
    private static readonly object _lock = new();
    private static int _lastLength = 0;
    private const int PREFIX_MAX_LENGTH = 60;
    private const int PROGRESS_BAR_WIDTH = 80;

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

    /// <summary>
    /// Writes a welcome message to the console.
    /// </summary>
    public static void Welcome()
    {
        Console.WriteLine("\n\nMyFolderSync - Folder Synchronization Tool");
        Console.WriteLine("===========================================\n\n");
    }

    /// <summary>
    /// Updates progress bar (thread-safe, non-flickering).
    /// </summary>
    /// <param name="percentage">Progress in percent (0–100).</param>
    /// <param name="prefix">Optional prefix text (e.g., "Reading files")</param>
    public static void WriteProcess(double percentage, string? prefix = null)
    {
        lock (_lock)
        {
            string trimmedPrefix = TrimPrefix(prefix ?? string.Empty);

            int filled = (int)(PROGRESS_BAR_WIDTH * (percentage / 100.0));
            int empty = PROGRESS_BAR_WIDTH - filled;

            StringBuilder sb = new();
            sb.Append(trimmedPrefix);
            sb.Append(" [");
            sb.Append(new string('#', filled));
            sb.Append(new string('-', empty));
            sb.Append($"] {percentage, 6:F2}%");

            string text = sb.ToString();

            int diff = _lastLength - text.Length;
            if (diff > 0)
                text += new string(' ', diff);

            _lastLength = text.Length;

            Console.Write($"\r{text}");
        }
    }

    /// <summary>
    /// Clears progress line (use after completion).
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            Console.Write('\r');
            Console.Write(new string(' ', _lastLength));
            Console.Write("\r");
            _lastLength = 0;
        }
    }

    private static string TrimPrefix(string prefix)
    {
        if (prefix.Length <= PREFIX_MAX_LENGTH)
        {
            return prefix.PadRight(PREFIX_MAX_LENGTH, ' ');
        }
        return prefix.Substring(0, PREFIX_MAX_LENGTH - 3) + "...";
    }
}
