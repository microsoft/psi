// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of any type into tuples of originating and message times.
    /// </summary>
    [StreamAdapter]
    public class ObjectToLatencyAdapter : StreamAdapter<object, Tuple<DateTime, DateTime>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectToLatencyAdapter"/> class.
        /// </summary>
        public ObjectToLatencyAdapter()
            : base(Adapter)
        {
        }

        private static Tuple<DateTime, DateTime> Adapter(object value, Envelope env)
        {
            return Tuple.Create(env.OriginatingTime, env.Time);
        }
    }
}
