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
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Stream of originating times.</returns>
        public static IProducer<DateTime> TimeOf<T>(this IProducer<T> source, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(TimeOf))
            => source.Select((_, e) => e.OriginatingTime, deliveryPolicy, name);

        /// <summary>
        /// Map messages to their current latency (time since origination).
        /// </summary>
        /// <typeparam name="T">Type of source stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Stream of latency (time span) values.</returns>
        public static IProducer<TimeSpan> Latency<T>(this IProducer<T> source, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(Latency))
            => source.Select((_, e) => e.CreationTime - e.OriginatingTime, deliveryPolicy, name);

        /// <summary>
        /// Delays the delivery of messages by a given time span.
        /// </summary>
        /// <typeparam name="T">The type of the source/output messages.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="delay">The time span by which to delay the messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The output stream.</returns>
        /// <remarks>
        /// This operator delays the delivery of messages on the source stream by a fixed amount of time
        /// ahead of the creation time of the source messages. This ensures that the messages are not
        /// delivered to the downstream receiver(s) until the pipeline clock has advanced to at least
        /// the delayed time. The observed delay may be slightly larger than the specified time span to
        /// account for latencies at the emitters and receivers. The originating times of the source
        /// messages are preserved.
        /// </remarks>
        public static IProducer<T> Delay<T>(this IProducer<T> source, TimeSpan delay, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return source
                .Process<T, (T, DateTime)>((d, e, s) => s.Post((d, e.OriginatingTime), e.CreationTime + delay), deliveryPolicy)
                .Process<(T, DateTime), T>((t, _, s) => s.Post(t.Item1, t.Item2), DeliveryPolicy.SynchronousOrThrottle);
        }
    }
}
