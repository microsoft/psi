// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx.Visualization
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Implements a stream adapter from <see cref="List{MaskRCNNDetection}"/> to <see cref="Shared{Image}"/> with containing masks, bounding boxes and labels.
    /// </summary>
    [StreamAdapter]
    public class MaskRCNNDetectionAdapter : StreamAdapter<MaskRCNNDetectionResults, Shared<Image>>
    {
        /// <inheritdoc />
        public override Shared<Image> GetAdaptedValue(MaskRCNNDetectionResults source, Envelope envelope)
        {
            if (source == null)
            {
                return null;
            }

            return Onnx.Operators.Render(source);
        }

        /// <inheritdoc/>
        public override void Dispose(Shared<Image> destination) => destination?.Dispose();
    }
}
