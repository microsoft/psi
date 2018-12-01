// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PsiStoreTool
{
    using CommandLine;

    /// <summary>
    /// Command-line verbs.
    /// </summary>
    internal class Verbs
    {
        /// <summary>
        /// Base command-line options.
        /// </summary>
        internal abstract class Base
        {
            /// <summary>
            /// Gets or sets file path to Psi data store.
            /// </summary>
            [Option('p', "path", HelpText = "File path to Psi data store (default=working directory).")]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets name of Psi data store.
            /// </summary>
            [Option('d', "data", Required = true, HelpText = "Name of Psi data store.")]
            public string Store { get; set; }
        }

        /// <summary>
        /// Base stream-related command-line options.
        /// </summary>
        internal abstract class BaseStream : Base
        {
            /// <summary>
            /// Gets or sets name of Psi stream.
            /// </summary>
            [Option('s', "stream", Required = true, HelpText = "Name Psi stream within data store.")]
            public string Stream { get; set; }
        }

        /// <summary>
        /// Base stream-related command-line options.
        /// </summary>
        internal abstract class BaseTransportStream : BaseStream
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
        internal class List : Base
        {
        }

        /// <summary>
        /// Display stream info verb.
        /// </summary>
        [Verb("info", HelpText = "Display stream information (metadata).")]
        internal class Info : BaseStream
        {
        }

        /// <summary>
        /// Display messages verb.
        /// </summary>
        [Verb("messages", HelpText = "Display messages in stream.")]
        internal class Messages : BaseStream
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
        internal class Save : BaseTransportStream
        {
            /// <summary>
            /// Gets or sets file to which to write.
            /// </summary>
            [Option('f', "file", Required = true, HelpText = "File to which to persist data.")]
            public string File { get; set; }
        }

        /// <summary>
        /// Save messages verb.
        /// </summary>
        [Verb("send", HelpText = "Send messages to message queue (ZeroMQ/NetMQ).")]
        internal class Send : BaseTransportStream
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
    }
}