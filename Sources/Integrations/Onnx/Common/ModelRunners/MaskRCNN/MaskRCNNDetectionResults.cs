// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a set of detection results from the <see cref="MaskRCNNModelRunner"/>.
    /// </summary>
    public class MaskRCNNDetectionResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaskRCNNDetectionResults"/> class.
        /// </summary>
        /// <param name="detections">Set of detections.</param>
        /// <param name="imageWidth">Original image width.</param>
        /// <param name="imageHeight">Original image height.</param>
        public MaskRCNNDetectionResults(IEnumerable<MaskRCNNDetection> detections, int imageWidth, int imageHeight)
        {
            this.Detections = detections.ToList();
            this.ImageWidth = imageWidth;
            this.ImageHeight = imageHeight;
        }

        /// <summary>
        /// Gets the set of detections.
        /// </summary>
        public List<MaskRCNNDetection> Detections { get; }

        /// <summary>
        /// Gets the original image width.
        /// </summary>
        public int ImageWidth { get; }

        /// <summary>
        /// Gets the original image height.
        /// </summary>
        public int ImageHeight { get; }
    }
}
