// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from depth camera view (shared encoded depth image with intrinsics) to spatial depth camera view (shared depth image with intrinsics) with default position.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthCameraViewToSpatialDepthCameraViewAdapter : StreamAdapter<(Shared<EncodedDepthImage>, ICameraIntrinsics), (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        private readonly EncodedDepthImageToDepthImageAdapter depthImageAdapter = new EncodedDepthImageToDepthImageAdapter();

        /// <inheritdoc/>
        public override (Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) GetAdaptedValue((Shared<EncodedDepthImage>, ICameraIntrinsics) source, Envelope envelope)
            => (this.depthImageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2, new CoordinateSystem());

        /// <inheritdoc/>
        public override void Dispose((Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) destination)
            => this.depthImageAdapter.Dispose(destination.Item1);
    }
}
