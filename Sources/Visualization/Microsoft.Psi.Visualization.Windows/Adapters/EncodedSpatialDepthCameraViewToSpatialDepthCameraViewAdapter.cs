// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of spatial depth camera view (encoded depth image) to spatial depth camera view (decoded depth image).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialDepthCameraViewToSpatialDepthCameraViewAdapter : StreamAdapter<(Shared<EncodedDepthImage>, ICameraIntrinsics, CoordinateSystem), (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedSpatialDepthCameraViewToSpatialDepthCameraViewAdapter"/> class.
        /// </summary>
        public EncodedSpatialDepthCameraViewToSpatialDepthCameraViewAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) Adapter((Shared<EncodedDepthImage>, ICameraIntrinsics, CoordinateSystem) value, Envelope env)
        {
            return (value.Item1?.Decode(), value.Item2, value.Item3);
        }
    }
}
