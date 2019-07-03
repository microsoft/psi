// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Connnects a stream producer to a stream consumer. As a result, all messages in the stream will be routed to the consumer for processing.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <typeparam name="TC">The type of consumer.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="consumer">The consumer (subscriber).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Consumer (subscriber).</returns>
        public static TC PipeTo<TIn, TC>(this IProducer<TIn> source, TC consumer, DeliveryPolicy deliveryPolicy = null)
            where TC : IConsumer<TIn>
        {
            return PipeTo(source, consumer, false, deliveryPolicy);
        }

        /// <summary>
        /// Creates a connector that exposes the messages it receives as a stream rather than calling a delegate.
        /// This allows the owning component to apply stream operators to this input.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this connector.</typeparam>
        /// <param name="p">The pipeline.</param>
        /// <param name="name">The name of this connector.</param>
        /// <returns>The newly created connector.</returns>
        public static Connector<T> CreateConnector<T>(this Pipeline p, string name)
        {
            return new Connector<T>(p, name);
        }

        /// <summary>
        /// Connnects a stream producer to a stream consumer. As a result, all messages in the stream will be routed to the consumer for processing.
        /// </summary>
        /// <remarks>
        /// This is an internal-only method which provides the option to allow connections between producers and consumers in running pipelines.
        /// </remarks>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <typeparam name="TC">The type of consumer.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="consumer">The consumer (subscriber).</param>
        /// <param name="allowWhileRunning">An optional flag to allow connections in running pipelines.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Consumer.</returns>
        internal static TC PipeTo<TIn, TC>(this IProducer<TIn> source, TC consumer, bool allowWhileRunning, DeliveryPolicy deliveryPolicy = null)
            where TC : IConsumer<TIn>
        {
            source.Out.Subscribe(consumer.In, allowWhileRunning, deliveryPolicy ?? source.Out.Pipeline.DeliveryPolicy);
            return consumer;
        }
    }
}