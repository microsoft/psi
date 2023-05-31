// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;

    /// <summary>
    /// Represents a stream reader attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StreamReaderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReaderAttribute"/> class.
        /// </summary>
        /// <param name="name">Name of stream reader source (e.g. "Psi Store", "WAV File", ...).</param>
        /// <param name="extension">File extension of stream reader source (e.g. ".psi", ".wav", ...).</param>
        public StreamReaderAttribute(string name, string extension)
        {
            this.Name = name;
            this.Extension = extension;
        }

        /// <summary>
        /// Gets the name of stream reader source.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the file extension of the stream reader source.
        /// </summary>
        public string Extension { get; private set; }
    }
}