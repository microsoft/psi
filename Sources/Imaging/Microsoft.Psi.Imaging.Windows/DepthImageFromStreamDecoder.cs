// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements a depth image decoder.
    /// </summary>
    public class DepthImageFromStreamDecoder : IDepthImageFromStreamDecoder
    {
        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, DepthImage depthImage)
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            bitmapSource.CopyPixels(Int32Rect.Empty, depthImage.ImageData, depthImage.Stride * depthImage.Height, depthImage.Stride);
        }
    }
}