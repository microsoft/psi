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
        /// Zip one or more streams (T) into a single stream (Message{T}) while ensuring delivery in originating time order (ordered within single tick by stream ID).
        /// </summary>
        /// <remarks>Messages are produced in originating-time order; potentially delayed in wall-clock time.</remarks>
        /// <typeparam name="T">Type of messages.</typeparam>
        /// <param name="inputs">Collection of homogeneous inputs.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of zipped messages.</returns>
        public static IProducer<Message<T>> Zip<T>(IEnumerable<IProducer<T>> inputs, DeliveryPolicy<T> deliveryPolicy = null)
        {
            if (inputs.Count() == 0)
            {
                throw new ArgumentException("Zip requires one or more inputs.");
            }

            var zip = new Zip<T>(inputs.First().Out.Pipeline);
            foreach (var i in inputs)
            {
                i.PipeTo(zip.AddInput($"Receiver{i.Out.Id}"), deliveryPolicy);
            }

            return zip.Out;
        }

        /// <summary>
        /// Zip two streams (T) into a single stream (Message{T}) while ensuring delivery in originating time order (ordered within single tick by stream ID).
        /// </summary>
        /// <remarks>Messages are produced in originating-time order; potentially delayed in wall-clock time.</remarks>
        /// <typeparam name="T">Type of messages.</typeparam>
        /// <param name="input1">First input stream.</param>
        /// <param name="input2">Second input stream with same message type.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of zipped messages.</returns>
        public static IProducer<Message<T>> Zip<T>(this IProducer<T> input1, IProducer<T> input2, DeliveryPolicy<T> deliveryPolicy = null)
        {
            return Zip(new List<IProducer<T>>() { input1, input2 }, deliveryPolicy);
        }
    }
}