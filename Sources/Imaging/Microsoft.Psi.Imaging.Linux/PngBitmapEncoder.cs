// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using SkiaSharp;

    /// <summary>
    /// PNG bitmap encoder.
    /// </summary>
    public class PngBitmapEncoder : IBitmapEncoder
    {
        /// <summary>
        /// Encode image to stream.
        /// </summary>
        /// <param name="image">Image to be encoded.</param>
        /// <param name="stream">Stream to which to encode.</param>
        public void Encode(Image image, Stream stream)
        {
            var data = SKData.Create(image.ImageData, image.Size);
            var img = SKImage.FromPixelData(SKImageInfo.Empty, data, image.Stride);
            var png = img.Encode(SKEncodedImageFormat.Png, 100);
            png.SaveTo(stream);
        }
    }
}