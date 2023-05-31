// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the event arguments passed by the stream read error event of <see cref="StreamDataProvider{T}"/>.
    /// </summary>
    public class StreamReadErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the name of the store containing the stream that was unable to be read.
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// Gets or sets the path to the store containing the stream that was unable to be read.
        /// </summary>
        public string StorePath { get; set; }

        /// <summary>
        /// Gets or sets the name of the stream that was unable to be read.
        /// </summary>
        public string StreamName { get; set; }

        /// <summary>
        /// Gets or sets the serialization exception that occurred.
        /// </summary>
        public SerializationException Exception { get; set; }
    }
}
