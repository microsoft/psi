// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    /// <summary>
    /// Represents an update to a message in a stream.
    /// </summary>
    /// <typeparam name="T">The type of messages in the stream.</typeparam>
    public class StreamUpdate<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamUpdate{T}"/> class.
        /// </summary>
        /// <param name="updateType">The type of update being performed.</param>
        /// <param name="message">The update data, or null if the update type is delete.</param>
        public StreamUpdate(StreamUpdateType updateType, Message<T> message)
        {
            this.UpdateType = updateType;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the type of update being performed.
        /// </summary>
        public StreamUpdateType UpdateType { get; set; }

        /// <summary>
        /// Gets or sets the data for the update.
        /// </summary>
        public Message<T> Message { get; set; }
    }
}
