// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements a general bitmap image decoder.
    /// </summary>
    /// <remarks>Internally uses System.Windows.Media.Imaging.BitmapDecoder.</remarks>
    public class ImageFromBitmapStreamDecoder : IImageFromStreamDecoder
    {
        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, Image image)
        {
            // decode JPEG, PNG, ...
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            var fmt = bitmapSource.Format.ToPixelFormat();
            if (fmt != image.PixelFormat)
            {
                using var img = ImagePool.GetOrCreate(image.Width, image.Height, fmt);
                bitmapSource.CopyPixels(Int32Rect.Empty, img.Resource.ImageData, img.Resource.Stride * img.Resource.Height, img.Resource.Stride);
                img.Resource.CopyTo(image);
            }
            else
            {
                bitmapSource.CopyPixels(Int32Rect.Empty, image.ImageData, image.Stride * image.Height, image.Stride);
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