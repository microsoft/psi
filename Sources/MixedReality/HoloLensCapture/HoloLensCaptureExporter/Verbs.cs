// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
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
        internal class ExportCommand
        {
            /// <summary>
            /// Gets or sets the file path of the input Psi data store.
            /// </summary>
            [Option('n', "name", Required = false, HelpText = "Name of the input Psi data store (default: HoloLensCapture).")]
            public string StoreName { get; set; } = "HoloLensCapture";

            /// <summary>
            /// Gets or sets the file path of the input Psi data store.
            /// </summary>
            [Option('p', "path", Required = true, HelpText = "Path to the input Psi data store.")]
            public string StorePath { get; set; }

            /// <summary>
            /// Gets or sets the output path to export data to.
            /// </summary>
            [Option('o', "output", Required = true, HelpText = "Output path to export data to.")]
            public string OutputPath { get; set; }
        }
    }
}