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
    /// Implements a stream adapter from an encoded image with intrinsics and pose to an image camera view.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageIntrinsicsPoseToImageCameraViewAdapter : StreamAdapter<(Shared<EncodedImage>, ICameraIntrinsics, CoordinateSystem), ImageCameraView>
    {
        private readonly ImageFromStreamDecoder imageDecoder = new ();

        /// <inheritdoc/>
        public override ImageCameraView GetAdaptedValue((Shared<EncodedImage>, ICameraIntrinsics, CoordinateSystem) source, Envelope envelope)
        {
            if (source.Item1 == null || source.Item1.Resource == null)
            {
                return default;
            }

            var encodedImage = source.Item1.Resource;
            var image = ImagePool.GetOrCreate(encodedImage.Width, encodedImage.Height, encodedImage.PixelFormat);
            image.Resource.DecodeFrom(encodedImage, this.imageDecoder);
            return new ImageCameraView(image, source.Item2, source.Item3);
        }

        /// <inheritdoc/>
        public override void Dispose(ImageCameraView destination)
            => destination?.ViewedObject?.Dispose();
    }
}