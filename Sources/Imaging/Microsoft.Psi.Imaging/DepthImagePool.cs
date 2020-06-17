// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Provides a pool of shared depth images.
    /// </summary>
    public static class DepthImagePool
    {
        private static readonly KeyedSharedPool<DepthImage, (int, int)> Instance =
            new KeyedSharedPool<DepthImage, (int width, int height)>(key => new DepthImage(key.width, key.height));

        /// <summary>
        /// Gets or creates a depth image from the pool.
        /// </summary>
        /// <param name="width">The requested image width.</param>
        /// <param name="height">The requested image height.</param>
        /// <returns>A shared depth image from the pool.</returns>
        public static Shared<DepthImage> GetOrCreate(int width, int height)
        {
            return Instance.GetOrCreate((width, height));
        }

        /// <summary>
        /// Gets or creates a depth image from the pool and initializes it with a managed <see cref="Bitmap"/> object.
        /// </summary>
        /// <param name="bitmap">A bitmap from which to copy the image data.</param>
        /// <returns>A shared depth image from the pool containing a copy of the image data from <paramref name="bitmap"/>.</returns>
        public static Shared<DepthImage> GetOrCreateFrom(Bitmap bitmap)
        {
            BitmapData sourceData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            Shared<DepthImage> sharedDepthImage = null;
            try
            {
                sharedDepthImage = GetOrCreate(bitmap.Width, bitmap.Height);
                sharedDepthImage.Resource.CopyFrom(sourceData);
            }
            finally
            {
                bitmap.UnlockBits(sourceData);
            }

            return sharedDepthImage;
        }
    }
}
