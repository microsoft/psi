// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Represents a tracked 3D object.
    /// </summary>
    public class TrackedObject3D
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
        /// Gets or sets the tracking score.
        /// </summary>
        public float TrackingScore { get; set; }

        /// <summary>
        /// Gets or sets the object bounding box.
        /// </summary>
        public Box3D BoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the object point cloud.
        /// </summary>
        public PointCloud3D PointCloud { get; set; }
    }
}
