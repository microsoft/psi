// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from int into double.
    /// </summary>
    [StreamAdapter]
    public class IntToDoubleAdapter : StreamAdapter<int, double>
    {
        /// <inheritdoc/>
        public override double GetAdaptedValue(int source, Envelope envelope)
            => source;
    }
}