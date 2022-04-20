// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="EncodedImageCameraView"/> to <see cref="ImageCameraView"/>.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageCameraViewToImageCameraViewAdapter : StreamAdapter<EncodedImageCameraView, ImageCameraView>
    {
        private readonly ImageFromStreamDecoder imageDecoder = new ();

        /// <inheritdoc/>
        public override ImageCameraView GetAdaptedValue(EncodedImageCameraView source, Envelope envelope)
        {
            if (source == null)
            {
                return default;
            }

            var encodedImage = source.ViewedObject.Resource;
            var image = ImagePool.GetOrCreate(encodedImage.Width, encodedImage.Height, encodedImage.PixelFormat);
            image.Resource.DecodeFrom(encodedImage, this.imageDecoder);
            return new ImageCameraView(image, source.CameraIntrinsics, source.CameraPose);
        }

        /// <inheritdoc/>
        public override void Dispose(ImageCameraView destination)
            => destination?.ViewedObject?.Dispose();
    }
}