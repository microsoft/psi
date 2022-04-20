// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;

    /// <summary>
    /// Represents a camera view of a 3D point cloud.
    /// </summary>
    public class PointCloud3DCameraView : CameraView<PointCloud3D>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointCloud3DCameraView"/> class.
        /// </summary>
        /// <param name="pointCloud3D">The 3D point cloud.</param>
        /// <param name="cameraIntrinsics">Intrinsics of the camera.</param>
        /// <param name="cameraPose">Pose of the camera.</param>
        public PointCloud3DCameraView(PointCloud3D pointCloud3D, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
            : base(pointCloud3D, cameraIntrinsics, cameraPose)
        {
        }
    }
}
