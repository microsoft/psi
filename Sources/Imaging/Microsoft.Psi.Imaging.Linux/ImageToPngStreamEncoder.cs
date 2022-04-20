// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using SkiaSharp;

    /// <summary>
    /// Implements an image encoder for PNG format.
    /// </summary>
    public class ImageToPngStreamEncoder : IImageToStreamEncoder
    {
        /// <inheritdoc/>
        public string Description => "Png";

        /// <inheritdoc/>
        public void EncodeToStream(Image image, Stream stream)
        {
            image.AsSKImage().Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
        }
    }
}