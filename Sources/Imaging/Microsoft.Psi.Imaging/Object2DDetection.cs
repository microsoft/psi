// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.Drawing;

    /// <summary>
    /// Represents a detection result for an 2D object.
    /// </summary>
    public class Object2DDetection
    {
        /// <summary>
        /// Gets or sets the object class.
        /// </summary>
        public string Class { get; set; }

        /// <summary>
        /// Gets or sets the instance id.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the detection score.
        /// </summary>
        public float DetectionScore { get; set; }

        /// <summary>
        /// Gets or sets the detection bounding box.
        /// </summary>
        public RectangleF BoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the detection mask.
        /// </summary>
        public float[][] Mask { get; set; }
    }
}
