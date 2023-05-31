// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Provides a pool of shared encoded images.
    /// </summary>
    public static class EncodedImagePool
    {
        private static readonly KeyedSharedPool<EncodedImage, (int, int, PixelFormat)> Instance =
            new KeyedSharedPool<EncodedImage, (int width, int height, PixelFormat pixelFormat)>(key => new EncodedImage(key.width, key.height, key.pixelFormat));

        /// <summary>
        /// Gets or creates an encoded image from the pool.
        /// </summary>
        /// <returns>A shared encoded image from the pool.</returns>
        /// <param name="width">The requested encoded image width.</param>
        /// <param name="height">The requested encoded image height.</param>
        /// <param name="pixelFormat">The requested encoded image pixel format.</param>
        public static Shared<EncodedImage> GetOrCreate(int width, int height, PixelFormat pixelFormat)
        {
            return Instance.GetOrCreate((width, height, pixelFormat));
        }
    }
}
