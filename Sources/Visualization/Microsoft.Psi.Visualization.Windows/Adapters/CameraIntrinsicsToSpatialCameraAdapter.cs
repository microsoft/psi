// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of <see cref="ICameraIntrinsics"/> to spatial camera with default position.
    /// </summary>
    [StreamAdapter]
    public class CameraIntrinsicsToSpatialCameraAdapter : StreamAdapter<ICameraIntrinsics, (ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CameraIntrinsicsToSpatialCameraAdapter"/> class.
        /// </summary>
        public CameraIntrinsicsToSpatialCameraAdapter()
            : base(Adapter)
        {
        }

        private static (ICameraIntrinsics, CoordinateSystem) Adapter(ICameraIntrinsics value, Envelope env)
        {
            return (value, new CoordinateSystem());
        }
    }
}
