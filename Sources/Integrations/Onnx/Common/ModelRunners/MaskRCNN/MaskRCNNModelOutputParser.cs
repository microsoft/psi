// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Internal static class that parses the outputs from the Mask R-CNN model into
    /// a set of masked object detection results.
    /// </summary>
    internal static class MaskRCNNModelOutputParser
    {
        /// <summary>
        /// Parses the model outputs into a list of object masks and detection results.
        /// </summary>
        /// <param name="scores">Confidence level scores.</param>
        /// <param name="boxes">Bounding boxes.</param>
        /// <param name="scaleWidth">Scale factor applied to bounding boxes (to original image width).</param>
        /// <param name="scaleHeight">Scale factor applied to bounding boxes (to original image height).</param>
        /// <param name="labels">Classification labels.</param>
        /// <param name="masks">Masks within bounding box.</param>
        /// <param name="classes">Classes corresponding to label indexes.</param>
        /// <param name="confidenceThreshold">The confidence threshold to use in filtering results.</param>
        /// <returns>The list of detection results.</returns>
        internal static IEnumerable<MaskRCNNDetection> Extract(float[] scores, float[] boxes, float scaleWidth, float scaleHeight, long[] labels, float[] masks, string[] classes, float confidenceThreshold)
        {
            for (var i = 0; i < scores.Length; i++)
            {
                var confidence = scores[i];
                if (confidence > confidenceThreshold)
                {
                    var label = classes[(int)labels[i]];

                    var box = i * 4;
                    var x0 = boxes[box] * scaleWidth;
                    var y0 = boxes[box + 1] * scaleHeight;
                    var x1 = boxes[box + 2] * scaleWidth;
                    var y1 = boxes[box + 3] * scaleHeight;
                    var bounds = new RectangleF(x0, y0, x1 - x0, y1 - y0);

                    const int MASK_SIZE = 28 * 28;
                    var mask = new float[MASK_SIZE];
                    Array.Copy(masks, i * MASK_SIZE, mask, 0, MASK_SIZE);

                    yield return new MaskRCNNDetection(label, confidence, bounds, mask);
                }
            }
        }
    }
}
