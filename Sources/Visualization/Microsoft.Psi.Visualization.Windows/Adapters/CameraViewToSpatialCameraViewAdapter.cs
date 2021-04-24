// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from camera view to spatial camera view with default position.
    /// </summary>
    [StreamAdapter]
    public class CameraViewToSpatialCameraViewAdapter : StreamAdapter<(Shared<Image>, ICameraIntrinsics), (Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <inheritdoc/>
        public override (Shared<Image>, ICameraIntrinsics, CoordinateSystem) GetAdaptedValue((Shared<Image>, ICameraIntrinsics) source, Envelope envelope)
            => (source.Item1, source.Item2, new CoordinateSystem());
    }
}
