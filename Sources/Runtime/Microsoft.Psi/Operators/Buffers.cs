// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Buffer messages by window size.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="size">Window size (message count).</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> Buffer<T, U>(this IProducer<T> source, int size, Func<IEnumerable<Message<T>>, ValueTuple<U, DateTime>> selector, DeliveryPolicy policy = null)
        {
            return BufferSelectInternal(source, size, selector, policy);
        }

        /// <summary>
        /// Buffer messages by window size.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="size">Window size (message count).</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> Buffer<T>(this IProducer<T> source, int size, DeliveryPolicy policy = null)
        {
            return Buffer(source, size, FirstTimestamp, policy);
        }

        /// <summary>
        /// Buffer messages by window size.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="size">Window size (message count).</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> History<T, U>(this IProducer<T> source, int size, Func<IEnumerable<Message<T>>, ValueTuple<U, DateTime>> selector, DeliveryPolicy policy = null)
        {
            return BufferSelectInternal(source, size, selector, policy);
        }

        /// <summary>
        /// Buffer messages by window size.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="size">Window size (message count).</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> History<T>(this IProducer<T> source, int size, DeliveryPolicy policy = null)
        {
            return History(source, size, LastTimestamp, policy);
        }

        /// <summary>
        /// Historical messages by time span.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="timeSpan">Time span over which to gather historical messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> History<T, U>(this IProducer<T> source, TimeSpan timeSpan, Func<IEnumerable<Message<T>>, ValueTuple<U, DateTime>> selector, DeliveryPolicy policy = null)
        {
            if (timeSpan.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("timeSpan", timeSpan, "The timeSpan should be positive (and non-zero).");
            }

            Func<Message<T>, DateTime, bool> bufferRemoveCondition =
                (b, ct) => b.OriginatingTime < ct - timeSpan;

            var buffer = new BufferSelect<T, U>(source.Out.Pipeline, bufferRemoveCondition, selector);
            source.PipeTo(buffer.In, policy ?? DeliveryPolicy.Immediate);
            return buffer.Out;
        }

        /// <summary>
        /// Historical messages by time span.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="timeSpan">Time span over which to gather historical messages.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> History<T>(this IProducer<T> source, TimeSpan timeSpan, DeliveryPolicy policy = null)
        {
            return History(source, timeSpan, LastTimestamp, policy);
        }

        /// <summary>
        /// Previous message nth back.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="index">Index of previous message (nth back).</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Previous<T>(this IProducer<T> source, int index, DeliveryPolicy policy = null)
        {
            return source.History(index + 1, policy).Where(b => b.Count() == index + 1, policy).Select(b => b.ElementAt(0), policy);
        }

        private static IProducer<U> BufferSelectInternal<T, U>(this IProducer<T> source, int size, Func<IEnumerable<Message<T>>, ValueTuple<U, DateTime>> selector, DeliveryPolicy policy)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size", size, "The size should be positive (and non-zero).");
            }

            var buffer = new BufferSelect<T, U>(source.Out.Pipeline, selector, size);
            return PipeTo(source, buffer, policy);
        }

        private static ValueTuple<IEnumerable<T>, DateTime> FirstTimestamp<T>(IEnumerable<Message<T>> messages)
        {
            return ValueTuple.Create(messages.Select(x => x.Data), messages.First().OriginatingTime);
        }

        private static ValueTuple<IEnumerable<T>, DateTime> LastTimestamp<T>(IEnumerable<Message<T>> messages)
        {
            return ValueTuple.Create(messages.Select(x => x.Data), messages.Last().OriginatingTime);
        }
    }
}