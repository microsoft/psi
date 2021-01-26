// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of depth camera view to spatial depth camera view with default position.
    /// </summary>
    [StreamAdapter]
    public class DepthCameraViewToSpatialDepthCameraViewAdapter : StreamAdapter<(Shared<DepthImage>, ICameraIntrinsics), (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthCameraViewToSpatialDepthCameraViewAdapter"/> class.
        /// </summary>
        public DepthCameraViewToSpatialDepthCameraViewAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) Adapter((Shared<DepthImage>, ICameraIntrinsics) value, Envelope env)
        {
            return (value.Item1, value.Item2, new CoordinateSystem());
        }
    }
}
