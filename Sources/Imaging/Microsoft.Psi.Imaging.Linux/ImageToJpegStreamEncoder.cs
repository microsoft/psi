// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using SkiaSharp;

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
            image.AsSKImage().Encode(SKEncodedImageFormat.Jpeg, this.QualityLevel).SaveTo(stream);
        }
    }
}