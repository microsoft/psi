// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx.Visualization
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="TinyYoloV2DetectionResults"/> to <see cref="Object2DDetectionResults"/>.
    /// </summary>
    [StreamAdapter]
    public class TinyYoloV2DetectionResultsAdapter : StreamAdapter<TinyYoloV2DetectionResults, Object2DDetectionResults>
    {
        /// <inheritdoc />
        public override Object2DDetectionResults GetAdaptedValue(TinyYoloV2DetectionResults source, Envelope envelope)
            => source?.ToObject2DDetectionResults();
    }
}
