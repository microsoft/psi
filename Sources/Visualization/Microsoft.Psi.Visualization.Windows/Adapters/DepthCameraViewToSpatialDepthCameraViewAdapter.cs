// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from depth camera view to spatial depth camera view with default position.
    /// </summary>
    [StreamAdapter]
    public class DepthCameraViewToSpatialDepthCameraViewAdapter : StreamAdapter<(Shared<DepthImage>, ICameraIntrinsics), (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        /// <inheritdoc/>
        public override (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) GetAdaptedValue((Shared<DepthImage>, ICameraIntrinsics) source, Envelope envelope)
            => (source.Item1, source.Item2, new CoordinateSystem());
    }
}
