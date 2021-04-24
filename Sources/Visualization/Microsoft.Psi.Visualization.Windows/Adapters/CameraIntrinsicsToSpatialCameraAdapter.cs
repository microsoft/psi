// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="ICameraIntrinsics"/> to spatial camera with default position.
    /// </summary>
    [StreamAdapter]
    public class CameraIntrinsicsToSpatialCameraAdapter : StreamAdapter<ICameraIntrinsics, (ICameraIntrinsics, CoordinateSystem)>
    {
        /// <inheritdoc/>
        public override (ICameraIntrinsics, CoordinateSystem) GetAdaptedValue(ICameraIntrinsics source, Envelope envelope)
            => (source, new CoordinateSystem());
    }
}
