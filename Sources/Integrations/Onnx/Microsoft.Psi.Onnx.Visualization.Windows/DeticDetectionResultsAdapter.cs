// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx.Visualization
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="DeticDetectionResults"/> to <see cref="Object2DDetectionResults"/>.
    /// </summary>
    [StreamAdapter]
    public class DeticDetectionResultsAdapter : StreamAdapter<DeticDetectionResults, Object2DDetectionResults>
    {
        /// <inheritdoc />
        public override Object2DDetectionResults GetAdaptedValue(DeticDetectionResults source, Envelope envelope)
            => source?.ToObject2DDetectionResults();
    }
}
