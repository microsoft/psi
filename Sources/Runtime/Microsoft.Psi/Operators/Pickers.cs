// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Filter messages to those where a given condition is met.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="condition">Predicate function by which to filter messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Where<T>(this IProducer<T> source, Func<T, Envelope, bool> condition, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return Process<T, T>(
                source,
                (d, e, s) =>
                {
                    if (condition(d, e))
                    {
                        s.Post(d, e.OriginatingTime);
                    }
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Filter messages to those where a given condition is met.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="condition">Predicate function by which to filter messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Where<T>(this IProducer<T> source, Predicate<T> condition, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return Where(source, (d, e) => condition(d), deliveryPolicy);
        }

        /// <summary>
        /// Filter stream to the first n messages.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="number">Number of messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> First<T>(this IProducer<T> source, int number, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return source.Where(v => number-- > 0, deliveryPolicy);
        }

        /// <summary>
        /// Filter stream to the first message (single-message stream).
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> First<T>(this IProducer<T> source, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return First(source, 1, deliveryPolicy);
        }
    }
}
