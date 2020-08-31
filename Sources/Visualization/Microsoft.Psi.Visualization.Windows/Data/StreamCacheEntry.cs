// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Class describing a cached indexed reader thunk.
    /// </summary>
    public class StreamCacheEntry
    {
        private dynamic indexThunk;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamCacheEntry"/> class.
        /// </summary>
        /// <param name="indexThunk">Thunk used to read the indexed message.</param>
        /// <param name="creationTime">The largest creation time value seen up to the position specified by this entry.</param>
        /// <param name="originatingTime">The largest originating time value seen up to the position specified by this entry.</param>
        public StreamCacheEntry(dynamic indexThunk, DateTime creationTime, DateTime originatingTime)
        {
            this.indexThunk = indexThunk;
            this.CreationTime = creationTime;
            this.OriginatingTime = originatingTime;
        }

        /// <summary>
        /// Gets the largest creation time value seen up to the position specified by this entry.
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Gets the largest originating time value seen up to the position specified by this entry.
        /// </summary>
        public DateTime OriginatingTime { get; private set; }

        /// <summary>
        /// Reads the message at the cached index entry.
        /// </summary>
        /// <typeparam name="T">Type of the message to read.</typeparam>
        /// <param name="streamReader">The stream reader that will read the data.</param>
        /// <returns>The message read from the store.</returns>
        public T Read<T>(IStreamReader streamReader)
        {
            return ((Func<IStreamReader, T>)this.indexThunk)(streamReader);
        }
    }
}
