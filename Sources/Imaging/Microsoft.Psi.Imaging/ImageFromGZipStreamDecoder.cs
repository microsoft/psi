// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Implements a GZip image decoder.
    /// </summary>
    public class ImageFromGZipStreamDecoder : IImageFromStreamDecoder
    {
        /// <summary>
        /// Determine whether stream has a GZip header.
        /// </summary>
        /// <param name="stream">Stream containing image data.</param>
        /// <returns>A value indicating whether the stream has a GZip header.</returns>
        public bool HasGZipHeader(Stream stream)
        {
            var isGZip = stream.Length >= 2 && stream.ReadByte() == 0x1f && stream.ReadByte() == 0x8b;
            stream.Position = 0;
            return isGZip;
        }

        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, Image image)
        {
            if (!this.HasGZipHeader(stream))
            {
                throw new ArgumentException("Stream does not appear to be GZip-encoded (missing header).");
            }

            // decode GZip
            var size = image.Stride * image.Height;
            using var decompressor = new GZipStream(stream, CompressionMode.Decompress);
            unsafe
            {
                decompressor.CopyTo(new UnmanagedMemoryStream((byte*)image.ImageData.ToPointer(), size, size, FileAccess.ReadWrite));
            }
        }

        /// <inheritdoc/>
        public PixelFormat GetPixelFormat(Stream stream)
        {
            if (!this.HasGZipHeader(stream))
            {
                throw new ArgumentException("Stream does not appear to be GZip-encoded (missing header).");
            }

            return PixelFormat.Undefined; // unknown (only affects images prior to PixelFormat property being introduced anyway)
        }
    }
}
