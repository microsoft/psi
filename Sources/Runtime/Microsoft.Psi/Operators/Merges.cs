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
        /// Merge one or more streams (T) into a single stream (Message{T}) interleaved in wall-clock time.
        /// </summary>
        /// <remarks>Messages are produced in the order they arrive, in wall-clock time; potentially out of originating-time order.</remarks>
        /// <typeparam name="T">Type of messages.</typeparam>
        /// <param name="inputs">Collection of homogeneous inputs.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Stream of merged messages.</returns>
        public static IProducer<Message<T>> Merge<T>(IEnumerable<IProducer<T>> inputs, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(Merge))
        {
            if (inputs.Count() == 0)
            {
                throw new ArgumentException("Merge requires one or more inputs.");
            }

            var merge = new Merge<T>(inputs.First().Out.Pipeline, name);
            foreach (var i in inputs)
            {
                i.PipeTo(merge.AddInput($"Receiver{i.Out.Id}"), deliveryPolicy);
            }

            return merge.Out;
        }

        /// <summary>
        /// Merge two streams (T) into a single stream (Message{T}) interleaved in wall-clock time.
        /// </summary>
        /// <remarks>Messages are produced in the order they arrive, in wall-clock time; potentially out of originating-time order.</remarks>
        /// <typeparam name="T">Type of messages.</typeparam>
        /// <param name="input1">First input stream.</param>
        /// <param name="input2">Second input stream with same message type.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Stream of merged messages.</returns>
        public static IProducer<Message<T>> Merge<T>(this IProducer<T> input1, IProducer<T> input2, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(Merge))
            => Merge(new List<IProducer<T>>() { input1, input2 }, deliveryPolicy, name);
    }
}