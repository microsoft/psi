// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.IO;

    /// <summary>
    /// Implements a helper class for writing messages to the console and a log file.
    /// </summary>
    public static class AppConsole
    {
        /// <summary>
        /// Gets or sets the log file name.
        /// </summary>
        public static string LogFilename { get; set; } = null;

        /// <summary>
        /// Writes the line terminator to the app console.
        /// </summary>
        public static void WriteLine()
        {
            Console.WriteLine();
            if (LogFilename != null)
            {
                File.AppendAllText(LogFilename, Environment.NewLine);
            }
        }

        /// <summary>
        /// Writes the line terminator with timing information to the app console.
        /// </summary>
        public static void TimedWriteLine()
        {
            var dateTimeString = $"[{DateTime.Now:MM/dd/yyyy HH:mm:ss.ffff}]: ";
            Console.WriteLine(dateTimeString);
            if (LogFilename != null)
            {
                File.AppendAllText(LogFilename, $"{dateTimeString}{Environment.NewLine}");
            }
        }

        /// <summary>
        /// Writes a message followed by the line terminator to the app console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
            if (LogFilename != null)
            {
                File.AppendAllText(LogFilename, message + Environment.NewLine);
            }
        }

        /// <summary>
        /// Writes a message followed by the line terminator to the app console, with timing information.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void TimedWriteLine(string message)
        {
            var dateTimeString = $"[{DateTime.Now:MM/dd/yyyy HH:mm:ss.ffff}]: ";
            Console.WriteLine($"{dateTimeString}{message}");
            if (LogFilename != null)
            {
                File.AppendAllText(LogFilename, $"{dateTimeString}{message}{Environment.NewLine}");
            }
        }

        /// <summary>
        /// Writes a message to the app console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void Write(string message)
        {
            Console.Write(message);
            if (LogFilename != null)
            {
                File.AppendAllText(LogFilename, message);
            }
        }

        /// <summary>
        /// Writes a message to the app console, with timing information.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void TimedWrite(string message)
        {
            var dateTimeString = $"[{DateTime.Now:MM/dd/yyyy HH:mm:ss.ffff}]: ";
            Console.Write($"{dateTimeString}{message}");
            if (LogFilename != null)
            {
                File.AppendAllText(LogFilename, $"{dateTimeString}{message}");
            }
        }
    }
}
