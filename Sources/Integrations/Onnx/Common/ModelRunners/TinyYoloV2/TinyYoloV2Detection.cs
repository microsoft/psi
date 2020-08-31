// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Drawing;

    /// <summary>
    /// Represents an object detection result from the <see cref="TinyYoloV2OnnxModelRunner"/>.
    /// </summary>
    /// <remarks>
    /// Contains a bounding box, a label, and a confidence score.
    /// </remarks>
    public class TinyYoloV2Detection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TinyYoloV2Detection"/> class.
        /// </summary>
        public TinyYoloV2Detection()
        {
        }

        /// <summary>
        /// Gets or sets the bounding box.
        /// </summary>
        public RectangleF BoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the confidence level.
        /// </summary>
        public float Confidence { get; set; }
    }
}
