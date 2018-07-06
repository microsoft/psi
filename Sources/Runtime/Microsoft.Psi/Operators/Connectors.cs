// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Connnects a stream producer to a stream consumer. As a result, all messages in the stream will be routed to the consumer for processing.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <typeparam name="TC">The type of consumer</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="consumer">The consumer (subscriber)</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>The consumer (subscriber).</returns>
        public static TC PipeTo<TIn, TC>(this IProducer<TIn> source, TC consumer, DeliveryPolicy policy = null)
            where TC : IConsumer<TIn>
        {
            source.Out.Subscribe(consumer.In, policy ?? DeliveryPolicy.Immediate);
            return consumer;
        }

        /// <summary>
        /// Creates a connector that exposes the messages it receives as a stream rather than calling a delegate.
        /// This allows the owning component to apply stream operators to this input.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this connector</typeparam>
        /// <param name="p">The pipeline</param>
        /// <param name="owner">The owning component</param>
        /// <param name="name">The name of this connector</param>
        /// <returns>The newly created connector.</returns>
        public static Connector<T> CreateConnector<T>(this Pipeline p, object owner, string name)
        {
            return new Connector<T>(p, owner, name);
        }

        /// <summary>
        /// Creates a connector that exposes the messages it receives as a stream rather than calling a delegate.
        /// This allows the owning component to apply stream operators to this input.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this connector</typeparam>
        /// <param name="parent">The pipeline</param>
        /// <param name="subpipeline">The pipeline to which to connect.</param>
        /// <param name="owner">The owning component</param>
        /// <param name="name">The name of this connector</param>
        /// <returns>The newly created connector.</returns>
        public static Connector<T> CreateInputConnector<T>(this Pipeline parent, Subpipeline subpipeline, object owner, string name)
        {
            if (parent == subpipeline)
            {
                throw new ArgumentException("Input connections should be from be from a parent pipeline to a subpipeline.");
            }

            return new Connector<T>(parent, subpipeline, owner, name);
        }

        /// <summary>
        /// Creates a connector that exposes the messages it receives as a stream rather than calling a delegate.
        /// This allows the owning component to apply stream operators to this input.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this connector</typeparam>
        /// <param name="self">The subpipeline</param>
        /// <param name="pipeline">The pipeline to which to connect.</param>
        /// <param name="owner">The owning component</param>
        /// <param name="name">The name of this connector</param>
        /// <returns>The newly created connector.</returns>
        public static Connector<T> CreateOutputConnector<T>(this Subpipeline self, Pipeline pipeline, object owner, string name)
        {
            if (self == pipeline)
            {
                throw new ArgumentException("Output connections should be from be from a subpipeline to a parent pipeline.");
            }

            return new Connector<T>(self, pipeline, owner, name);
        }
    }
}