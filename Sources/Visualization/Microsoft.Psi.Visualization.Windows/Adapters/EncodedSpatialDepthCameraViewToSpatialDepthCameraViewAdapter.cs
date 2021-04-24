// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from encoded spatial depth camera view (shared encoded depth image with intrinsics and position) to spatial depth camera view (shared depth image with intrinsics and position).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialDepthCameraViewToSpatialDepthCameraViewAdapter : StreamAdapter<(Shared<EncodedDepthImage>, ICameraIntrinsics, CoordinateSystem), (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        private readonly EncodedDepthImageToDepthImageAdapter depthImageAdapter = new EncodedDepthImageToDepthImageAdapter();

        /// <inheritdoc/>
        public override (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) GetAdaptedValue((Shared<EncodedDepthImage>, ICameraIntrinsics, CoordinateSystem) source, Envelope envelope)
            => (this.depthImageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2, source.Item3);

        /// <inheritdoc/>
        public override void Dispose((Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) destination)
            => this.depthImageAdapter.Dispose(destination.Item1);
    }
}
