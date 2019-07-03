// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of time spans into doubles.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimeSpanAdapter : StreamAdapter<TimeSpan, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanAdapter"/> class.
        /// </summary>
        public TimeSpanAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(TimeSpan value, Envelope env)
        {
            return value.TotalMilliseconds;
        }
    }
}