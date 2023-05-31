// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Runtime.InteropServices;
    using SkiaSharp;

    /// <summary>
    /// Implements a general bitmap image decoder.
    /// </summary>
    /// <remarks>Internally uses SkiaSharp.SKBitmap.Decode().</remarks>
    public class ImageFromBitmapStreamDecoder : IImageFromStreamDecoder
    {
        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, Image image)
        {
            // decode JPEG, PNG, ...
            var decoded = SKBitmap.Decode(stream);
            Marshal.Copy(decoded.Bytes, 0, image.ImageData, decoded.ByteCount);
        }

        /// <inheritdoc/>
        public PixelFormat GetPixelFormat(Stream stream)
        {
            var decoded = SKBitmap.Decode(stream);
            return decoded.ColorType switch
            {
                SKColorType.Bgra8888 => PixelFormat.BGRA_32bpp,
                SKColorType.Gray8 => PixelFormat.Gray_8bpp,
                _ => PixelFormat.Undefined,
            };
        }
    }
}