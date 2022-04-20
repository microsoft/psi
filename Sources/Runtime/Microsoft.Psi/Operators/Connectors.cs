// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Connects a stream producer to a stream consumer.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <typeparam name="TConsumer">The type of the consumer.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="consumer">The consumer (subscriber).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The consumer that was passed as input.</returns>
        public static TConsumer PipeTo<TIn, TConsumer>(this IProducer<TIn> source, TConsumer consumer, DeliveryPolicy<TIn> deliveryPolicy = null)
            where TConsumer : IConsumer<TIn> =>
            PipeTo(source, consumer, false, deliveryPolicy);

        /// <summary>
        /// Creates a connector that exposes the messages it receives as a stream rather than calling a delegate.
        /// This allows the owning component to apply stream operators to this input.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this connector.</typeparam>
        /// <param name="p">The pipeline.</param>
        /// <param name="name">The name of this connector.</param>
        /// <returns>The newly created connector.</returns>
        public static Connector<T> CreateConnector<T>(this Pipeline p, string name) =>
            new (p, name);

        /// <summary>
        /// Creates a stream in a specified target pipeline, based on a given input stream (that may belong in a different pipeline).
        /// </summary>
        /// <typeparam name="T">The type of the messages on the input stream.</typeparam>
        /// <param name="input">The input stream.</param>
        /// <param name="targetPipeline">Pipeline to which to bridge.</param>
        /// <param name="name">An optional name for the connector (defaults to BridgeConnector).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The bridged stream.</returns>
        public static IProducer<T> BridgeTo<T>(this IProducer<T> input, Pipeline targetPipeline, string name = null, DeliveryPolicy<T> deliveryPolicy = null)
        {
            if (input.Out.Pipeline == targetPipeline)
            {
                return input;
            }
            else
            {
                var connector = new Connector<T>(input.Out.Pipeline, targetPipeline, name ?? nameof(BridgeTo));
                return input.PipeTo(connector, deliveryPolicy);
            }
        }

        /// <summary>
        /// Connects a stream producer to a stream consumer.
        /// </summary>
        /// <remarks>
        /// This is an internal-only method which provides the option to allow connections between producers and consumers in running pipelines.
        /// </remarks>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <typeparam name="TConsumer">The type of the consumer.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="consumer">The consumer (subscriber).</param>
        /// <param name="allowWhileRunning">An optional flag to allow connections in running pipelines.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The consumer that was passed as input.</returns>
        internal static TConsumer PipeTo<TIn, TConsumer>(this IProducer<TIn> source, TConsumer consumer, bool allowWhileRunning, DeliveryPolicy<TIn> deliveryPolicy = null)
            where TConsumer : IConsumer<TIn>
        {
            source.Out.Subscribe(consumer.In, allowWhileRunning, deliveryPolicy ?? source.Out.Pipeline.GetDefaultDeliveryPolicy<TIn>());
            return consumer;
        }
    }
}