// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a specified type to an interface.
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    public class InterfaceAdapter<T, TInterface> : StreamAdapter<T, TInterface>
        where T : TInterface
    {
        /// <inheritdoc/>
        public override TInterface GetAdaptedValue(T source, Envelope envelope)
            => source;
    }
}
