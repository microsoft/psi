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
    [StreamAdapter]
    public class TimeSpanToDoubleAdapter : StreamAdapter<TimeSpan, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanToDoubleAdapter"/> class.
        /// </summary>
        public TimeSpanToDoubleAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(TimeSpan value, Envelope env)
        {
            return value.TotalMilliseconds;
        }
    }
}