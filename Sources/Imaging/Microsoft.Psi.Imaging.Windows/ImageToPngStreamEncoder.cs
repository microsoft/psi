// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements an image encoder for PNG format.
    /// </summary>
    public class ImageToPngStreamEncoder : IImageToStreamEncoder
    {
        /// <inheritdoc/>
        public string Description => $"Png";

        /// <inheritdoc/>
        public void EncodeToStream(Image image, Stream stream)
        {
            // The encoder is created on every call (rather than in a constructor)
            // b/c the encoder has thread affinity
            var encoder = new PngBitmapEncoder();
            var bitmapSource = BitmapSource.Create(
                image.Width,
                image.Height,
                96,
                96,
                image.PixelFormat.ToWindowsMediaPixelFormat(),
                null,
                image.ImageData,
                image.Stride * image.Height,
                image.Stride);
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);
        }
    }
}