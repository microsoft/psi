// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of any type into tuples of originating and message times.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class LatencyAdapter<T> : StreamAdapter<T, Tuple<DateTime, DateTime>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LatencyAdapter{T}"/> class.
        /// </summary>
        public LatencyAdapter()
            : base(Adapter)
        {
        }

        private static Tuple<DateTime, DateTime> Adapter(T value, Envelope env)
        {
            return Tuple.Create(env.OriginatingTime, env.Time);
        }
    }
}
