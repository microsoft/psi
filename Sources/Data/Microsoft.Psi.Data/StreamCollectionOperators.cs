// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    /// <summary>
    /// Implements helper operators for stream collections.
    /// </summary>
    public static class StreamCollectionOperators
    {
        /// <summary>
        /// Writes the specified stream to the specified stream collection.
        /// </summary>
        /// <typeparam name="T">The type of the stream data.</typeparam>
        /// <param name="stream">The stream to write.</param>
        /// <param name="name">The name of the stream.</param>
        /// <param name="streamCollection">The stream collection to write to.</param>
        /// <returns>The source stream.</returns>
        public static IProducer<T> Write<T>(this IProducer<T> stream, string name, StreamCollection streamCollection)
        {
            streamCollection.Add(stream, name);
            return stream;
        }
    }
}
