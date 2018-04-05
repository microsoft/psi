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
        /// Repeat last seen source message (or default or initial value) upon clock signal.
        /// </summary>
        /// <typeparam name="T">Type of stream messages.</typeparam>
        /// <typeparam name="TClock">Type of clock signal.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock signal stream.</param>
        /// <param name="useInitialValue">Whether to seed with an initial value (before any messages seen).</param>
        /// <param name="initialValue">Initial value (repeated before any messages seen).</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Repeat<T, TClock>(this IProducer<T> source, IProducer<TClock> clock, bool useInitialValue = false, T initialValue = default(T), DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Throttled;
            var repeater = new Repeater<T, TClock>(source.Out.Pipeline, useInitialValue, initialValue);
            clock.PipeTo(repeater.ClockIn, policy);
            source.PipeTo(repeater.In, policy);
            return repeater;
        }

        /// <summary>
        /// Delay messages by given time span.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="delay">Time span by which to delay.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Delay<T>(this IProducer<T> source, TimeSpan delay, DeliveryPolicy policy = null)
        {
            var p = source
                .Process<T, (T, DateTime)>((d, e, s) => s.Post((d, e.OriginatingTime), e.OriginatingTime + delay), policy)
                .Process<(T, DateTime), T>((t, _, s) => s.Post(t.Item1, t.Item2), policy);
            return p;
        }
    }
}