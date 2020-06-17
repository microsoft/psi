// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Provides a pool of shared images.
    /// </summary>
    public static class ImagePool
    {
        private static readonly KeyedSharedPool<Image, (int, int, PixelFormat)> Instance =
            new KeyedSharedPool<Image, (int width, int height, PixelFormat format)>(key => new Image(key.width, key.height, key.format));

        /// <summary>
        /// Gets or creates an image from the pool.
        /// </summary>
        /// <param name="width">The requested image width.</param>
        /// <param name="height">The requested image height.</param>
        /// <param name="pixelFormat">The requested image pixel format.</param>
        /// <returns>A shared image from the pool.</returns>
        public static Shared<Image> GetOrCreate(int width, int height, PixelFormat pixelFormat)
        {
            return Instance.GetOrCreate((width, height, pixelFormat));
        }

        /// <summary>
        /// Gets or creates an image from the pool and initializes it with a managed Bitmap object.
        /// </summary>
        /// <param name="bitmap">A bitmap from which to copy the image data.</param>
        /// <returns>A shared image from the pool containing a copy of the image data from <paramref name="bitmap"/>.</returns>
        public static Shared<Image> GetOrCreateFromBitmap(Bitmap bitmap)
        {
            BitmapData sourceData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            Shared<Image> sharedImage = null;
            try
            {
                sharedImage = GetOrCreate(bitmap.Width, bitmap.Height, PixelFormatHelper.FromSystemPixelFormat(bitmap.PixelFormat));
                sharedImage.Resource.CopyFrom(sourceData);
            }
            finally
            {
                bitmap.UnlockBits(sourceData);
            }

            return sharedImage;
        }
    }
}
