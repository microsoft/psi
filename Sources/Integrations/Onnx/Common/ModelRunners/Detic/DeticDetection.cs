// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Drawing;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a detection result for the Detic model.
    /// </summary>
    public class DeticDetection
    {
        /// <summary>
        /// Gets or sets the detection class.
        /// </summary>
        public string Class { get; set; }

        /// <summary>
        /// Gets or sets the detection score.
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Gets or sets the detection bounding box.
        /// </summary>
        public RectangleF Rectangle { get; set; }

        /// <summary>
        /// Gets or sets the detection mask.
        /// </summary>
        public bool[][] Mask { get; set; }

        /// <summary>
        /// Converts the <see cref="DeticDetection"/> to <see cref="Object2DDetection"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="Object2DDetection"/>.</returns>
        public Object2DDetection ToObject2DDetection()
        {
            var height = (int)this.Rectangle.Height;
            var width = (int)this.Rectangle.Width;
            var mask = new float[height][];
            for (var y = 0; y < height; y++)
            {
                mask[y] = new float[width];
                for (var x = 0; x < width; x++)
                {
                    mask[y][x] = this.Mask[y][x] ? 1f : 0f;
                }
            }

            return new ()
            {
                Class = this.Class,
                InstanceId = null,
                DetectionScore = this.Score,
                BoundingBox = this.Rectangle,
                Mask = mask,
            };
        }
    }
}
