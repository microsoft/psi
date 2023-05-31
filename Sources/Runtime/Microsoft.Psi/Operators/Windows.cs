// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Process windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <typeparam name="TOutput">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="indexInterval">The relative index interval over which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOutput> Window<TSource, TOutput>(
            this IProducer<TSource> source,
            IntInterval indexInterval,
            Func<IEnumerable<Message<TSource>>, TOutput> selector,
            DeliveryPolicy<TSource> deliveryPolicy = null,
            string name = nameof(Window))
        {
            var window = new RelativeIndexWindow<TSource, TOutput>(source.Out.Pipeline, indexInterval, selector, name);
            return PipeTo(source, window, deliveryPolicy);
        }

        /// <summary>
        /// Process windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <typeparam name="TOutput">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="fromIndex">The relative index from which to gather messages.</param>
        /// <param name="toIndex">The relative index to which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOutput> Window<TSource, TOutput>(
            this IProducer<TSource> source,
            int fromIndex,
            int toIndex,
            Func<IEnumerable<Message<TSource>>, TOutput> selector,
            DeliveryPolicy<TSource> deliveryPolicy = null,
            string name = nameof(Window))
            => Window(source, new IntInterval(fromIndex, toIndex), selector, deliveryPolicy, name);

        /// <summary>
        /// Get windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="indexInterval">The relative index interval over which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TSource[]> Window<TSource>(this IProducer<TSource> source, IntInterval indexInterval, DeliveryPolicy<TSource> deliveryPolicy = null, string name = nameof(Window))
            => Window(source, indexInterval, GetMessageData, deliveryPolicy, name);

        /// <summary>
        /// Get windows of messages by relative index interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="fromIndex">The relative index from which to gather messages.</param>
        /// <param name="toIndex">The relative index to which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TSource[]> Window<TSource>(this IProducer<TSource> source, int fromIndex, int toIndex, DeliveryPolicy<TSource> deliveryPolicy = null, string name = nameof(Window))
            => Window(source, fromIndex, toIndex, GetMessageData, deliveryPolicy, name);

        /// <summary>
        /// Process windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <typeparam name="TOutput">Type of output messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="relativeTimeInterval">The relative time interval over which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOutput> Window<TSource, TOutput>(
            this IProducer<TSource> source,
            RelativeTimeInterval relativeTimeInterval,
            Func<IEnumerable<Message<TSource>>, TOutput> selector,
            DeliveryPolicy<TSource> deliveryPolicy = null,
            string name = nameof(Window))
        {
            var window = new RelativeTimeWindow<TSource, TOutput>(source.Out.Pipeline, relativeTimeInterval, selector, name);
            return PipeTo(source, window, deliveryPolicy);
        }

        /// <summary>
        /// Process windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <typeparam name="TOutput">Type of output messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="fromTime">The relative timespan from which to gather messages.</param>
        /// <param name="toTime">The relative timespan to which to gather messages.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOutput> Window<TSource, TOutput>(
            this IProducer<TSource> source,
            TimeSpan fromTime,
            TimeSpan toTime,
            Func<IEnumerable<Message<TSource>>, TOutput> selector,
            DeliveryPolicy<TSource> deliveryPolicy = null,
            string name = nameof(Window))
            => Window(source, new RelativeTimeInterval(fromTime, toTime), selector, deliveryPolicy, name);

        /// <summary>
        /// Get windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="relativeTimeInterval">The relative time interval over which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TSource[]> Window<TSource>(
            this IProducer<TSource> source,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy<TSource> deliveryPolicy = null,
            string name = nameof(Window))
            => Window(source, relativeTimeInterval, GetMessageData, deliveryPolicy, name);

        /// <summary>
        /// Get windows of messages by relative time interval.
        /// </summary>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <param name="source">Source stream of messages.</param>
        /// <param name="fromTime">The relative timespan from which to gather messages.</param>
        /// <param name="toTime">The relative timespan to which to gather messages.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TSource[]> Window<TSource>(
            this IProducer<TSource> source,
            TimeSpan fromTime,
            TimeSpan toTime,
            DeliveryPolicy<TSource> deliveryPolicy = null,
            string name = nameof(Window))
            => Window(source, new RelativeTimeInterval(fromTime, toTime), deliveryPolicy, name);

        /// <summary>
        /// Get windows of messages specified via data from an additional window-defining stream.
        /// </summary>
        /// <remarks>
        /// The operator implements dynamic windowing over a stream of data. Messages on the incoming window stream
        /// are used to compute a relative time interval in the source stream. The output is created by a function
        /// that has access to the window message and the computed buffer of messages on the source stream.
        /// </remarks>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <typeparam name="TWindow">Type of messages on the additional window stream.</typeparam>
        /// <typeparam name="TOutput">Type of messages on the output stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="window">The window-defining stream.</param>
        /// <param name="windowCreator">The function that creates the actual window to use at every point.</param>
        /// <param name="outputCreator">A function that creates output messages given a message on the window-defining stream and a buffer of messages on the source stream.</param>
        /// <param name="sourceDeliveryPolicy">An optional delivery policy for the source stream.</param>
        /// <param name="windowDeliveryPolicy">An optional delivery policy for the window-defining stream.</param>
        /// <param name="name">An optional name for this operator.</param>
        /// <returns>A stream of computed outputs.</returns>
        public static IProducer<TOutput> Window<TSource, TWindow, TOutput>(
            this IProducer<TSource> source,
            IProducer<TWindow> window,
            Func<Message<TWindow>, (TimeInterval, DateTime)> windowCreator,
            Func<Message<TWindow>, IEnumerable<Message<TSource>>, TOutput> outputCreator,
            DeliveryPolicy<TSource> sourceDeliveryPolicy = null,
            DeliveryPolicy<TWindow> windowDeliveryPolicy = null,
            string name = nameof(Window))
        {
            var dynamicWindow = new DynamicWindow<TWindow, TSource, TOutput>(source.Out.Pipeline, windowCreator, outputCreator, name);
            window.PipeTo(dynamicWindow.WindowIn, windowDeliveryPolicy);
            source.PipeTo(dynamicWindow.In, sourceDeliveryPolicy);
            return dynamicWindow;
        }

        /// <summary>
        /// Get windows of messages specified via data from an additional window-defining stream.
        /// </summary>
        /// <remarks>
        /// The operator implements dynamic windowing over a stream of data. Messages on the incoming window stream
        /// are used to compute a relative time interval in the source stream. The output is created by a function
        /// that has access to the window message and the computed buffer of messages on the source stream.
        /// </remarks>
        /// <typeparam name="TSource">Type of source messages.</typeparam>
        /// <typeparam name="TWindow">Type of messages on the additional window stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="window">The window-defining stream.</param>
        /// <param name="windowCreator">The function that creates the actual window to use at every point.</param>
        /// <param name="sourceDeliveryPolicy">An optional delivery policy for the source stream.</param>
        /// <param name="windowDeliveryPolicy">An optional delivery policy for the window-defining stream.</param>
        /// <param name="name">An optional name for this operator.</param>
        /// <returns>A stream of computed outputs.</returns>
        public static IProducer<Message<TSource>[]> Window<TSource, TWindow>(
            this IProducer<TSource> source,
            IProducer<TWindow> window,
            Func<Message<TWindow>, (TimeInterval, DateTime)> windowCreator,
            DeliveryPolicy<TSource> sourceDeliveryPolicy = null,
            DeliveryPolicy<TWindow> windowDeliveryPolicy = null,
            string name = nameof(Window))
            => source.Window(
                window,
                windowCreator,
                (_, messages) => messages.ToArray(),
                sourceDeliveryPolicy,
                windowDeliveryPolicy,
                name);

        private static T[] GetMessageData<T>(IEnumerable<Message<T>> messages)
            => messages.Select(m => m.Data).ToArray();
    }
}
