// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a specified type to object.
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    public class ObjectAdapter<T> : StreamAdapter<T, object>
    {
        /// <inheritdoc/>
        public override object GetAdaptedValue(T source, Envelope envelope)
            => source;
    }
}
