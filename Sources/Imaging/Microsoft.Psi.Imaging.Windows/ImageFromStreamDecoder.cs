// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.IO.Compression;
    using System.Windows;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements an image decoder.
    /// </summary>
    public class ImageFromStreamDecoder : IImageFromStreamDecoder
    {
        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, Image image)
        {
            // GZip indentified by 1f8b header (see section 2.3.1 of RFC 1952 https://www.ietf.org/rfc/rfc1952.txt)
            if (stream.Length >= 2 && stream.ReadByte() == 0x1f && stream.ReadByte() == 0x8b)
            {
                // decode GZip
                stream.Position = 0; // advanced by if (... stream.ReadByte() ...) above
                var size = image.Stride * image.Height;
                using var decompressor = new GZipStream(stream, CompressionMode.Decompress);
                unsafe
                {
                    decompressor.CopyTo(new UnmanagedMemoryStream((byte*)image.ImageData.ToPointer(), size, size, FileAccess.ReadWrite));
                }
            }
            else
            {
                // decode JPEG, PNG, ...
                stream.Position = 0; // advanced by if (... stream.ReadByte() ...) above
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bitmapSource = decoder.Frames[0];
                var fmt = bitmapSource.Format.ToPixelFormat();
                if (fmt != image.PixelFormat)
                {
                    using var img = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(image.Width, image.Height, fmt);
                    bitmapSource.CopyPixels(Int32Rect.Empty, img.Resource.ImageData, img.Resource.Stride * img.Resource.Height, img.Resource.Stride);
                    img.Resource.CopyTo(image);
                }
                else
                {
                    bitmapSource.CopyPixels(Int32Rect.Empty, image.ImageData, image.Stride * image.Height, image.Stride);
                }
            }
        }

        /// <inheritdoc/>
        public PixelFormat GetPixelFormat(Stream stream)
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            return bitmapSource.Format.ToPixelFormat();
        }
    }
}