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
    /// Implements a stream adapter from a depth image with intrinsics and pose to a depth image camera view.
    /// </summary>
    [StreamAdapter]
    public class DepthImageIntrinsicsPoseToDepthImageCameraViewAdapter : StreamAdapter<(Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem), DepthImageCameraView>
    {
        /// <inheritdoc/>
        public override DepthImageCameraView GetAdaptedValue((Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem) source, Envelope envelope)
        {
            if (source.Item1 == null || source.Item1.Resource == null)
            {
                return default;
            }

            return new DepthImageCameraView(source.Item1, source.Item2, source.Item3);
        }

        /// <inheritdoc/>
        public override void Dispose(DepthImageCameraView destination)
            => destination?.ViewedObject?.Dispose();
    }
}