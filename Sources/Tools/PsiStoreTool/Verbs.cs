// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PsiStoreTool
{
    using System.Collections.Generic;
    using CommandLine;

    /// <summary>
    /// Command-line verbs.
    /// </summary>
    internal class Verbs
    {
        /// <summary>
        /// Base command-line options.
        /// </summary>
        internal abstract class BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets file path to Psi data store.
            /// </summary>
            [Option('p', "path", HelpText = "File path to Psi data store (default=working directory).")]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets name of Psi data store.
            /// </summary>
            [Option('d', "data", Required = true, HelpText = "Name of Psi data store(s).")]
            public string Store { get; set; }
        }

        /// <summary>
        /// Base stream-related command-line options.
        /// </summary>
        internal abstract class BaseStreamCommand : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets name of Psi stream.
            /// </summary>
            [Option('s', "stream", Required = true, HelpText = "Name of Psi stream within data store.")]
            public string Stream { get; set; }
        }

        /// <summary>
        /// Base stream-related command-line options.
        /// </summary>
        internal abstract class BaseTransportStreamCommand : BaseStreamCommand
        {
            /// <summary>
            /// Gets or sets format to which to serialize.
            /// </summary>
            [Option('m', "format", Required = true, HelpText = "Format specifier (msg, json, csv).")]
            public string Format { get; set; }
        }

        /// <summary>
        /// List streams verb.
        /// </summary>
        [Verb("list", HelpText = "List streams within a Psi data store.")]
        internal class ListStreams : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets a value indicating whether to show stream size information.
            /// </summary>
            [Option('s', "showsize", Required = false, HelpText = "Shows stream size information.", Default = false)]
            public bool ShowSize { get; set; }
        }

        /// <summary>
        /// Display stream info verb.
        /// </summary>
        [Verb("info", HelpText = "Display stream information (metadata).")]
        internal class Info : BaseStreamCommand
        {
        }

        /// <summary>
        /// Remove stream verb.
        /// </summary>
        [Verb("removestream", HelpText = "Removes a stream from a store.")]
        internal class RemoveStream : BaseStreamCommand
        {
        }

        /// <summary>
        /// Display messages verb.
        /// </summary>
        [Verb("messages", HelpText = "Display messages in stream.")]
        internal class Messages : BaseStreamCommand
        {
            /// <summary>
            /// Gets or sets number of messages to include.
            /// </summary>
            [Option('n', "number", HelpText = "Include first n messages (optional).", Default = int.MaxValue)]
            public int Number { get; set; }
        }

        /// <summary>
        /// Save messages verb.
        /// </summary>
        [Verb("save", HelpText = "Save messages to file system.")]
        internal class Save : BaseTransportStreamCommand
        {
            /// <summary>
            /// Gets or sets file to which to write.
            /// </summary>
            [Option('f', "file", Required = true, HelpText = "File to which to persist data.")]
            public string File { get; set; }
        }

        /// <summary>
        /// Send messages verb.
        /// </summary>
        [Verb("send", HelpText = "Send messages to message queue (ZeroMQ/NetMQ).")]
        internal class Send : BaseTransportStreamCommand
        {
            /// <summary>
            /// Gets or sets file to which to write.
            /// </summary>
            [Option('t', "topic", HelpText = "Topic name to which to send messages (default='').")]
            public string Topic { get; set; }

            /// <summary>
            /// Gets or sets format to which to serialize.
            /// </summary>
            [Option('a', "address", Required = true, HelpText = "Connection address to which to send messages (e.g. 'tcp://localhost:12345').")]
            public string Address { get; set; }
        }

        /// <summary>
        /// Concatenate stores verb.
        /// </summary>
        [Verb("concat", HelpText = "Concatenate a set of stores, generating a new store.")]
        internal class Concat : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets name of Psi data store.
            /// </summary>
            [Option('o', "output", Required = false, Default = "Concatenated", HelpText = "Name of output Psi data store (default=Concatenated).")]
            public string Output { get; set; }
        }

        /// <summary>
        /// List available tasks.
        /// </summary>
        [Verb("tasks", HelpText = "List available tasks in assemblies given.")]
        internal class ListTasks
        {
            /// <summary>
            /// Gets or sets task name to execute.
            /// </summary>
            [Option('m', "assemblies", Required = false, Separator = ';', HelpText = "Optional assemblies containing task (semicolon-separated).")]
            public IEnumerable<string> Assemblies { get; set; }
        }

        /// <summary>
        /// Execute task verb.
        /// </summary>
        [Verb("exec", HelpText = "Execute task defined in assembly given.")]
        internal class Exec
        {
            /// <summary>
            /// Gets or sets file path to Psi data store.
            /// </summary>
            [Option('p', "path", HelpText = "File path to Psi data store (default=working directory).")]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets name of Psi data store.
            /// </summary>
            [Option('d', "data", HelpText = "Name of Psi data store(s).")]
            public string Store { get; set; }

            /// <summary>
            /// Gets or sets task name to execute.
            /// </summary>
            [Option('t', "task", Required = true, HelpText = "Task name.")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets assemblies containing task.
            /// </summary>
            [Option('m', "assemblies", Required = false, Separator = ';', HelpText = "Optional assemblies containing task (semicolon-separated).")]
            public IEnumerable<string> Assemblies { get; set; }

            /// <summary>
            /// Gets or sets configuration values provided at the command-line.
            /// </summary>
            [Option('a', "arguments", Required = false, Separator = ';', HelpText = "Task arguments provided at the command-line (semicolon-separated).")]
            public IEnumerable<string> Arguments { get; set; }

            /// <summary>
            /// Gets or sets name of optional Psi stream.
            /// </summary>
            [Option('s', "stream", Required = false, HelpText = "Optional name of Psi stream within data store.")]
            public string Stream { get; set; }
        }

        /// <summary>
        /// Crop store verb.
        /// </summary>
        [Verb("crop", HelpText = "Crops a store between the extents of a specified interval, generating a new store.")]
        internal class Crop : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets name of output Psi data store.
            /// </summary>
            [Option('o', "output", Required = false, Default = "Cropped", HelpText = "Name of output Psi data store (default=Cropped).")]
            public string Output { get; set; }

            /// <summary>
            /// Gets or sets start of interval.
            /// </summary>
            [Option('s', "start", Required = false, HelpText = "Start of interval relative to beginning of store (default=0).")]
            public string Start { get; set; }

            /// <summary>
            /// Gets or sets length of interval (relative to start).
            /// </summary>
            [Option('l', "length", Required = false, HelpText = "Length of interval relative to start (default=unbounded).")]
            public string Length { get; set; }
        }

        /// <summary>
        /// Encode image streams verb.
        /// </summary>
        [Verb("encode", HelpText = "Encode image streams to JPEG.")]
        internal class Encode : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets name of output Psi data store.
            /// </summary>
            [Option('o', "output", Required = false, Default = "Encoded", HelpText = "Name of output Psi data store (default=Encoded).")]
            public string Output { get; set; }

            /// <summary>
            /// Gets or sets quality of the JPEG compression.
            /// </summary>
            [Option('q', "quality", Default = 90, HelpText = "Quality of JPEG compression 0-100 (optional, default 90).")]
            public int Quality { get; set; }
        }
    }
}