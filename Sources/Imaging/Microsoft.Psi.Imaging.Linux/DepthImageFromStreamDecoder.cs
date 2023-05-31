// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;
    using System.Runtime.InteropServices;
    using SkiaSharp;

    /// <summary>
    /// Implements a depth image decoder.
    /// </summary>
    public class DepthImageFromStreamDecoder : IDepthImageFromStreamDecoder
    {
        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, DepthImage depthImage)
        {
            var decoded = SKBitmap.Decode(stream);
            Marshal.Copy(decoded.Bytes, 0, depthImage.ImageData, decoded.ByteCount);
        }
    }
}