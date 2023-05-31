// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from float to double.
    /// </summary>
    [StreamAdapter]
    public class FloatToDoubleAdapter : StreamAdapter<float, double>
    {
        /// <inheritdoc/>
        public override double GetAdaptedValue(float source, Envelope envelope)
            => source;
    }
}