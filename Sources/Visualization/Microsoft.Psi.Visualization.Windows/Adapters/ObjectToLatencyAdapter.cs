// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a specified type to tuples of originating and creation times.
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    public class ObjectToLatencyAdapter<T> : StreamAdapter<T, Tuple<DateTime, DateTime>>
    {
        /// <inheritdoc/>
        public override Tuple<DateTime, DateTime> GetAdaptedValue(T source, Envelope envelope)
            => Tuple.Create(envelope.OriginatingTime, envelope.CreationTime);
    }
}
