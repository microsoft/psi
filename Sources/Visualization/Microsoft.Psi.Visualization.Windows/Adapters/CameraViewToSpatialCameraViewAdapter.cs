// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of camera view to spatial camera view with default position.
    /// </summary>
    [StreamAdapter]
    public class CameraViewToSpatialCameraViewAdapter : StreamAdapter<(Shared<Image>, ICameraIntrinsics), (Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CameraViewToSpatialCameraViewAdapter"/> class.
        /// </summary>
        public CameraViewToSpatialCameraViewAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<Image>, ICameraIntrinsics, CoordinateSystem) Adapter((Shared<Image>, ICameraIntrinsics) value, Envelope env)
        {
            return (value.Item1, value.Item2, new CoordinateSystem());
        }
    }
}
