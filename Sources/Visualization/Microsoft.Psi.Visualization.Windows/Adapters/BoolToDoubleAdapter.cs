// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="bool"/> to <see cref="double"/>.
    /// </summary>
    [StreamAdapter]
    public class BoolToDoubleAdapter : StreamAdapter<bool, double>
    {
        /// <inheritdoc/>
        public override double GetAdaptedValue(bool source, Envelope envelope)
            => source ? 1.0 : 0.0;
    }
}