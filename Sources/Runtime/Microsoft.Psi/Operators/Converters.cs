// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Assign name (meta) to the stream.
        /// </summary>
        /// <typeparam name="T">Type of stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="name">Name to give stream.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Name<T>(this IProducer<T> source, string name)
        {
            source.Out.Name = name;
            return source;
        }
    }
}
