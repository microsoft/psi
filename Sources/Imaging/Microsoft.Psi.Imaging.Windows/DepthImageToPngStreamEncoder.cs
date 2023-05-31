// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements a depth image encoder for PNG format.
    /// </summary>
    public class DepthImageToPngStreamEncoder : IDepthImageToStreamEncoder
    {
        /// <inheritdoc/>
        public string Description => "Png";

        /// <inheritdoc/>
        public void EncodeToStream(DepthImage depthImage, Stream stream)
        {
            var encoder = new PngBitmapEncoder();
            var bitmapSource = BitmapSource.Create(
                depthImage.Width,
                depthImage.Height,
                96,
                96,
                depthImage.PixelFormat.ToWindowsMediaPixelFormat(),
                null,
                depthImage.ImageData,
                depthImage.Stride * depthImage.Height,
                depthImage.Stride);
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);
        }
    }
}