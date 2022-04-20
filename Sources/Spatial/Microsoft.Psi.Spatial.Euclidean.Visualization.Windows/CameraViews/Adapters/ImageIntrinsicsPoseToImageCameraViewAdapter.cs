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
    /// Implements a stream adapter from an image with intrinsics and pose to an image camera view.
    /// </summary>
    [StreamAdapter]
    public class ImageIntrinsicsPoseToImageCameraViewAdapter : StreamAdapter<(Shared<Image>, ICameraIntrinsics, CoordinateSystem), ImageCameraView>
    {
        /// <inheritdoc/>
        public override ImageCameraView GetAdaptedValue((Shared<Image>, ICameraIntrinsics, CoordinateSystem) source, Envelope envelope)
        {
            if (source.Item1 == null || source.Item1.Resource == null)
            {
                return default;
            }

            return new ImageCameraView(source.Item1, source.Item2, source.Item3);
        }

        /// <inheritdoc/>
        public override void Dispose(ImageCameraView destination)
            => destination?.ViewedObject?.Dispose();
    }
}