// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of spatial camera view (encoded image) to spatial camera view (decoded image).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialCameraViewToSpatialCameraViewAdapter : StreamAdapter<(Shared<EncodedImage>, ICameraIntrinsics, CoordinateSystem), (Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedSpatialCameraViewToSpatialCameraViewAdapter"/> class.
        /// </summary>
        public EncodedSpatialCameraViewToSpatialCameraViewAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<Image>, ICameraIntrinsics, CoordinateSystem) Adapter((Shared<EncodedImage>, ICameraIntrinsics, CoordinateSystem) value, Envelope env)
        {
            return (value.Item1?.Decode(), value.Item2, value.Item3);
        }
    }
}
