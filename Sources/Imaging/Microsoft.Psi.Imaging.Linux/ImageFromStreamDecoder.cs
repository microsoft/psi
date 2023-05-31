// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;

    /// <summary>
    /// Implements an image decoder.
    /// </summary>
    public class ImageFromStreamDecoder : IImageFromStreamDecoder
    {
        private static readonly ImageFromGZipStreamDecoder GzipDecoder = new ();
        private static readonly ImageFromNV12StreamDecoder Nv12Decoder = new ();
        private static readonly ImageFromBitmapStreamDecoder BitmapDecoder = new ();

        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, Image image)
        {
            if (GzipDecoder.HasGZipHeader(stream))
            {
                GzipDecoder.DecodeFromStream(stream, image);
                return;
            }

            if (Nv12Decoder.HasNV12Header(stream))
            {
                Nv12Decoder.DecodeFromStream(stream, image);
                return;
            }

            // default to decode JPEG, PNG, ...
            BitmapDecoder.DecodeFromStream(stream, image);
        }

        /// <inheritdoc/>
        public PixelFormat GetPixelFormat(Stream stream)
        {
            if (GzipDecoder.HasGZipHeader(stream))
            {
                return GzipDecoder.GetPixelFormat(stream);
            }

            if (Nv12Decoder.HasNV12Header(stream))
            {
                return Nv12Decoder.GetPixelFormat(stream);
            }

            return BitmapDecoder.GetPixelFormat(stream);
        }
    }
}