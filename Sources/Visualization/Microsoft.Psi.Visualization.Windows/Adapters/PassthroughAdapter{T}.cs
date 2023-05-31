// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter that passes its input through to its output unchanged.
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    public class PassthroughAdapter<T> : StreamAdapter<T, T>
    {
        /// <inheritdoc/>
        public override T GetAdaptedValue(T source, Envelope envelope)
            => source;
    }
}
