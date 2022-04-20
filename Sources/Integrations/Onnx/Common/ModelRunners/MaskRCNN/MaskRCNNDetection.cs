// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Drawing;

    /// <summary>
    /// Represents a masked object detection result from the <see cref="MaskRCNNModelRunner"/>.
    /// </summary>
    public class MaskRCNNDetection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaskRCNNDetection"/> class.
        /// </summary>
        /// <param name="label">Classification label.</param>
        /// <param name="confidence">Confidence level.</param>
        /// <param name="bounds">Bounding box.</param>
        /// <param name="mask">Mask within bounding box.</param>
        public MaskRCNNDetection(string label, float confidence, RectangleF bounds, float[] mask)
        {
            this.Label = label;
            this.Confidence = confidence;
            this.Bounds = bounds;
            this.Mask = mask;
        }

        /// <summary>
        /// Gets the label.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets the confidence level.
        /// </summary>
        public float Confidence { get; }

        /// <summary>
        /// Gets bounding box.
        /// </summary>
        public RectangleF Bounds { get; }

        /// <summary>
        /// Gets the mask.
        /// </summary>
        public float[] Mask { get; }
    }
}
