// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents Detic object detection results over an image.
    /// </summary>
    public class DeticDetectionResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeticDetectionResults"/> class.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        public DeticDetectionResults(int width, int height)
        {
            this.ImageSize = new Size(width, height);
        }

        /// <summary>
        /// Gets the image size.
        /// </summary>
        public Size ImageSize { get; }

        /// <summary>
        /// Gets the list of detection results.
        /// </summary>
        public List<DeticDetection> Detections { get; } = new ();

        /// <summary>
        /// Converts the <see cref="DeticDetectionResults"/> to <see cref="Object2DDetectionResults"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="Object2DDetectionResults"/>.</returns>
        public Object2DDetectionResults ToObject2DDetectionResults()
            => new (
                this.ImageSize,
                this.Detections.Select(d => d.ToObject2DDetection()).ToList());
    }
}
