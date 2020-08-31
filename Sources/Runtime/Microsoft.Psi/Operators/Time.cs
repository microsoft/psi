// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Linq;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Map messages to their originating time.
        /// </summary>
        /// <typeparam name="T">Type of source stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of originating times.</returns>
        public static IProducer<DateTime> TimeOf<T>(this IProducer<T> source, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return source.Select((_, e) => e.OriginatingTime, deliveryPolicy);
        }

        /// <summary>
        /// Map messages to their current latency (time since origination).
        /// </summary>
        /// <typeparam name="T">Type of source stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of latency (time span) values.</returns>
        public static IProducer<TimeSpan> Latency<T>(this IProducer<T> source, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return source.Select((_, e) => e.CreationTime - e.OriginatingTime, deliveryPolicy);
        }

        /// <summary>
        /// Delay messages by given time span.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="delay">Time span by which to delay.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Delay<T>(this IProducer<T> source, TimeSpan delay, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return source
                .Process<T, (T, DateTime)>((d, e, s) => s.Post((d, e.OriginatingTime), e.OriginatingTime + delay), deliveryPolicy)
                .Process<(T, DateTime), T>((t, _, s) => s.Post(t.Item1, t.Item2), DeliveryPolicy.SynchronousOrThrottle);
        }
    }
}
