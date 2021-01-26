// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of camera view (encoded image) to spatial camera view (decoded image) with default position.
    /// </summary>
    [StreamAdapter]
    public class EncodedCameraViewToSpatialCameraViewAdapter : StreamAdapter<(Shared<EncodedImage>, ICameraIntrinsics), (Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedCameraViewToSpatialCameraViewAdapter"/> class.
        /// </summary>
        public EncodedCameraViewToSpatialCameraViewAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<Image>, ICameraIntrinsics, CoordinateSystem) Adapter((Shared<EncodedImage>, ICameraIntrinsics) value, Envelope env)
        {
            return (value.Item1?.Decode(), value.Item2, new CoordinateSystem());
        }
    }
}
