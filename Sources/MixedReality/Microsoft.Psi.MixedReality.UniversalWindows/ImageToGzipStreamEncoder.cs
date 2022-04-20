// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Implements an image encoder for GZip format.
    /// </summary>
    public class ImageToGZipStreamEncoder : IImageToStreamEncoder
    {
        /// <inheritdoc/>
        public string Description => "GZip";

        /// <inheritdoc/>
        public void EncodeToStream(Image image, Stream stream)
        {
            unsafe
            {
                var size = image.Stride * image.Height;
                var imageData = new UnmanagedMemoryStream((byte*)image.ImageData.ToPointer(), size);
                using var compressor = new GZipStream(stream, CompressionMode.Compress, true);
                imageData.CopyTo(compressor);
            }
        }
    }
}