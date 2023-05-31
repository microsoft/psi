// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Defines how depth values should be interpreted.
    /// </summary>
    public enum DepthValueSemantics
    {
        /// <summary>
        /// The depth value indicates the distance to a plane perpendicular
        /// to the camera's pointing direction.
        /// </summary>
        DistanceToPlane = 0,

        /// <summary>
        /// The depth value indicates the euclidean distance directly to
        /// the point in space.
        /// </summary>
        DistanceToPoint = 1,
    }
}
