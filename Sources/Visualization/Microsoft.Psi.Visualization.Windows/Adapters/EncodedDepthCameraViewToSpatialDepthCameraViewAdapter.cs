// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of depth camera view (encoded depth image) to spatial depth camera view (decoded depth image) with default position.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthCameraViewToSpatialDepthCameraViewAdapter : StreamAdapter<(Shared<EncodedDepthImage>, ICameraIntrinsics), (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthCameraViewToSpatialDepthCameraViewAdapter"/> class.
        /// </summary>
        public EncodedDepthCameraViewToSpatialDepthCameraViewAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) Adapter((Shared<EncodedDepthImage>, ICameraIntrinsics) value, Envelope env)
        {
            return (value.Item1?.Decode(), value.Item2, new CoordinateSystem());
        }
    }
}
