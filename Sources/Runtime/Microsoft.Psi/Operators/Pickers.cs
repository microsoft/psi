// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Filter messages to those where a given condition is met.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="condition">Predicate function by which to filter messages.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Where<T>(this IProducer<T> source, Func<T, Envelope, bool> condition, DeliveryPolicy policy = null)
        {
            var p = Process<T, T>(
                source,
                (d, e, s) =>
                {
                    if (condition(d, e))
                    {
                        s.Post(d, e.OriginatingTime);
                    }
                },
                policy);

            return p;
        }

        /// <summary>
        /// Filter messages to those where a given condition is met.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="condition">Predicate function by which to filter messages.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Where<T>(this IProducer<T> source, Predicate<T> condition, DeliveryPolicy policy = null)
        {
            return Where(source, (d, e) => condition(d), policy);
        }

        /// <summary>
        /// Filter stream to the first message (single-message stream).
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> First<T>(this IProducer<T> source)
        {
            bool first = true;
            return source.Where(v =>
            {
                if (first)
                {
                    first = false;
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }
    }
}
