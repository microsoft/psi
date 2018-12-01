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
        /// Process windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="indexInterval">The relative index interval over which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> Window<T, U>(this IProducer<T> source, IntInterval indexInterval, Func<IEnumerable<Message<T>>, U> selector, DeliveryPolicy deliveryPolicy = null)
        {
            var window = new RelativeIndexWindow<T, U>(source.Out.Pipeline, indexInterval, selector);
            return PipeTo(source, window, deliveryPolicy);
        }

        /// <summary>
        /// Process windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="fromIndex">The relative index from which to gather messages.</param>
        /// <param name="toIndex">The relative index to which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> Window<T, U>(this IProducer<T> source, int fromIndex, int toIndex, Func<IEnumerable<Message<T>>, U> selector, DeliveryPolicy deliveryPolicy = null)
        {
            return Window(source, new IntInterval(fromIndex, toIndex), selector, deliveryPolicy);
        }

        /// <summary>
        /// Get windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="indexInterval">The relative index interval over which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> Window<T>(this IProducer<T> source, IntInterval indexInterval, DeliveryPolicy deliveryPolicy = null)
        {
            return Window(source, indexInterval, GetMessageData, deliveryPolicy);
        }

        /// <summary>
        /// Get windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="fromIndex">The relative index from which to gather messages.</param>
        /// <param name="toIndex">The relative index to which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> Window<T>(this IProducer<T> source, int fromIndex, int toIndex, DeliveryPolicy deliveryPolicy = null)
        {
            return Window(source, fromIndex, toIndex, GetMessageData, deliveryPolicy);
        }

        /// <summary>
        /// Process windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="relativeTimeInterval">The relative time interval over which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> Window<T, U>(this IProducer<T> source, RelativeTimeInterval relativeTimeInterval, Func<IEnumerable<Message<T>>, U> selector, DeliveryPolicy deliveryPolicy = null)
        {
            var window = new RelativeTimeWindow<T, U>(source.Out.Pipeline, relativeTimeInterval, selector);
            return PipeTo(source, window, deliveryPolicy);
        }

        /// <summary>
        /// Process windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="U">Type of output messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="fromTime">The relative timespan from which to gather messages.</param>
        /// <param name="toTime">The relative timespan to which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<U> Window<T, U>(this IProducer<T> source, TimeSpan fromTime, TimeSpan toTime, Func<IEnumerable<Message<T>>, U> selector, DeliveryPolicy deliveryPolicy = null)
        {
            return Window(source, new RelativeTimeInterval(fromTime, toTime), selector, deliveryPolicy);
        }

        /// <summary>
        /// Get windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="relativeTimeInterval">The relative time interval over which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> Window<T>(this IProducer<T> source, RelativeTimeInterval relativeTimeInterval, DeliveryPolicy deliveryPolicy = null)
        {
            return Window(source, relativeTimeInterval, GetMessageData, deliveryPolicy);
        }

        /// <summary>
        /// Get windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="fromTime">The relative timespan from which to gather messages.</param>
        /// <param name="toTime">The relative timespan to which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<IEnumerable<T>> Window<T>(this IProducer<T> source, TimeSpan fromTime, TimeSpan toTime, DeliveryPolicy deliveryPolicy = null)
        {
            return Window(source, new RelativeTimeInterval(fromTime, toTime), deliveryPolicy);
        }

        private static IEnumerable<T> GetMessageData<T>(IEnumerable<Message<T>> messages)
        {
            return messages.Select(m => m.Data);
        }
    }
}
