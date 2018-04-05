// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Linq;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Map messages to their originating time.
        /// </summary>
        /// <typeparam name="T">Type of source stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <returns>Stream of originating times.</returns>
        public static IProducer<DateTime> TimeOf<T>(this IProducer<T> source)
        {
            return source.Select((_, e) => e.OriginatingTime);
        }

        /// <summary>
        /// Map messages to their current latency (time since origination).
        /// </summary>
        /// <typeparam name="T">Type of source stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <returns>Stream of latency (time span) values.</returns>
        public static IProducer<TimeSpan> Latency<T>(this IProducer<T> source)
        {
            return source.Select((_, e) => e.Time - e.OriginatingTime);
        }
    }
}
