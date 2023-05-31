// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from an encoded depth image with intrinsics to a depth image camera view.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageIntrinsicsToDepthImageCameraViewAdapter : StreamAdapter<(Shared<EncodedDepthImage>, ICameraIntrinsics), DepthImageCameraView>
    {
        private readonly EncodedDepthImageToDepthImageAdapter depthImageAdapter = new ();

        /// <inheritdoc/>
        public override DepthImageCameraView GetAdaptedValue((Shared<EncodedDepthImage>, ICameraIntrinsics) source, Envelope envelope)
            => new (this.depthImageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2, new CoordinateSystem());

        /// <inheritdoc/>
        public override void Dispose(DepthImageCameraView destination)
            => this.depthImageAdapter.Dispose(destination.ViewedObject);
    }
}
