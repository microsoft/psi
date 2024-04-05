// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents object 3D tracking results.
    /// </summary>
    public class Object3DTrackingResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Object3DTrackingResults"/> class.
        /// </summary>
        /// <param name="trackedObject3Ds">The set of tracked objects.</param>
        public Object3DTrackingResults(List<TrackedObject3D> trackedObject3Ds = null)
        {
            this.Detections = trackedObject3Ds ?? new ();
        }

        /// <summary>
        /// Gets the set of tracked 3d objects.
        /// </summary>
        public List<TrackedObject3D> Detections { get; } = new ();
    }
}
