// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.IO;

    /// <summary>
    /// Represents a log writer that is aware of error log messages.
    /// </summary>
    public class VisualizationLogWriter : StreamWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationLogWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream the writer should write to.</param>
        public VisualizationLogWriter(Stream stream)
            : base(stream)
        {
            this.HasErrors = false;
        }

        /// <summary>
        /// Gets a value indicating whether any of the lines written to the log were error messages.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Writes out a log line that represents an error log line.
        /// </summary>
        /// <param name="value">The text to write.</param>
        public void WriteError(string value)
        {
            this.WriteLine("\tERROR: " + value);
            this.HasErrors = true;
        }

        /// <summary>
        /// Writes out a log line that represents an error log line.
        /// </summary>
        /// <param name="format">A format string.</param>
        /// <param name="args">The arguments to use with the format string.</param>
        public void WriteError(string format, params object[] args)
        {
            this.WriteLine("\tERROR: " + format, args);
            this.HasErrors = true;
        }
    }
}
