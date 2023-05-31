// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from an encoded depth image with intrinsics and pose to a depth image camera view.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageIntrinsicsPoseToDepthImageCameraViewAdapter : StreamAdapter<(Shared<EncodedDepthImage>, ICameraIntrinsics, CoordinateSystem), DepthImageCameraView>
    {
        private readonly DepthImageFromStreamDecoder depthImageDecoder = new ();

        /// <inheritdoc/>
        public override DepthImageCameraView GetAdaptedValue((Shared<EncodedDepthImage>, ICameraIntrinsics, CoordinateSystem) source, Envelope envelope)
        {
            if (source.Item1 == null || source.Item1.Resource == null)
            {
                return default;
            }

            var encodedDepthImage = source.Item1.Resource;
            var depthImage = DepthImagePool.GetOrCreate(
                encodedDepthImage.Width,
                encodedDepthImage.Height,
                encodedDepthImage.DepthValueSemantics,
                encodedDepthImage.DepthValueToMetersScaleFactor);
            depthImage.Resource.DecodeFrom(encodedDepthImage, this.depthImageDecoder);
            return new DepthImageCameraView(depthImage, source.Item2, source.Item3);
        }

        /// <inheritdoc/>
        public override void Dispose(DepthImageCameraView destination)
            => destination?.ViewedObject?.Dispose();
    }
}