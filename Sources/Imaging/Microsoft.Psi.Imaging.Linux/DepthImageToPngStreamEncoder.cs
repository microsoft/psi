// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using SkiaSharp;

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
            depthImage.AsSKImage().Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
        }
    }
}