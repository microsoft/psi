// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Linq;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        public static IProducer<DateTime> TimeOf<T>(this IProducer<T> source)
        {
            return source.Select((_, e) => e.OriginatingTime);
        }

        public static IProducer<TimeSpan> Latency<T>(this IProducer<T> source)
        {
            return source.Select((_, e) => e.Time - e.OriginatingTime);
        }
    }
}
