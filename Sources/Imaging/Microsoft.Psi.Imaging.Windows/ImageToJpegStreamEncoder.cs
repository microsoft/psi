// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements an image encoder for JPEG format.
    /// </summary>
    public class ImageToJpegStreamEncoder : IImageToStreamEncoder
    {
        /// <inheritdoc/>
        public string Description => $"Jpeg({this.QualityLevel})";

        /// <summary>
        /// Gets or sets JPEG image quality (0-100).
        /// </summary>
        public int QualityLevel { get; set; } = 100;

        /// <inheritdoc/>
        public void EncodeToStream(Image image, Stream stream)
        {
            // The encoder is created on every call (rather than in a constructor)
            // b/c the encoder has thread affinity
            var encoder = new JpegBitmapEncoder { QualityLevel = this.QualityLevel };
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