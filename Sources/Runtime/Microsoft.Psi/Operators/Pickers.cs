// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        public static IProducer<T> Where<T>(this IProducer<T> source, Func<T, Envelope, bool> condition, DeliveryPolicy policy = null)
        {
            var p = Process<T, T>(
                source,
                (d, e, s) =>
                {
                    if (condition(d, e))
                    {
                        s.Post(d, e.OriginatingTime);
                    }
                },
                policy);

            return p;
        }

        public static IProducer<T> Where<T>(this IProducer<T> source, Predicate<T> condition, DeliveryPolicy policy = null)
        {
            return Where(source, (d, e) => condition(d), policy);
        }

        public static IProducer<T> First<T>(this IProducer<T> source)
        {
            bool first = true;
            return source.Where(v =>
            {
                if (first)
                {
                    first = false;
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }
    }
}
