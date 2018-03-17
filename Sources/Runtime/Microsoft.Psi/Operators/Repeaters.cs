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
        public static IProducer<T> Repeat<T, TClock>(this IProducer<T> source, IProducer<TClock> clock, bool useInitialValue = false, T initialValue = default(T), DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Throttled;
            var repeater = new Repeater<T, TClock>(source.Out.Pipeline, useInitialValue, initialValue);
            clock.PipeTo(repeater.ClockIn, policy);
            source.PipeTo(repeater.In, policy);
            return repeater;
        }

        public static IProducer<T> Delay<T>(this IProducer<T> source, TimeSpan delay, DeliveryPolicy policy = null)
        {
            var p = source
                .Process<T, (T, DateTime)>((d, e, s) => s.Post((d, e.OriginatingTime), e.OriginatingTime + delay), policy)
                .Process<(T, DateTime), T>((t, _, s) => s.Post(t.Item1, t.Item2), policy);
            return p;
        }
    }
}