// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from time span to double.
    /// </summary>
    [StreamAdapter]
    public class TimeSpanToDoubleAdapter : StreamAdapter<TimeSpan, double>
    {
        /// <inheritdoc/>
        public override double GetAdaptedValue(TimeSpan source, Envelope envelope)
            => source.TotalMilliseconds;
    }
}