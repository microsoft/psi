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
        private static readonly KeyedSharedPool<DepthImage, (int, int, DepthValueSemantics, double)> Instance =
            new KeyedSharedPool<DepthImage, (int width, int height, DepthValueSemantics depthValueSemantics, double depthValueToMetersScaleFactor)>(key =>
            new DepthImage(key.width, key.height, key.depthValueSemantics, key.depthValueToMetersScaleFactor));

        /// <summary>
        /// Gets or creates a depth image from the pool.
        /// </summary>
        /// <param name="width">The requested image width.</param>
        /// <param name="height">The requested image height.</param>
        /// <param name="depthValueSemantics">Optional requested depth value semantics.</param>
        /// <param name="depthValueToMetersScaleFactor">Optional scale factor to convert from depth values to meters.</param>
        /// <returns>A shared depth image from the pool.</returns>
        public static Shared<DepthImage> GetOrCreate(int width, int height, DepthValueSemantics depthValueSemantics = DepthValueSemantics.DistanceToPlane, double depthValueToMetersScaleFactor = 0.001)
        {
            return Instance.GetOrCreate((width, height, depthValueSemantics, depthValueToMetersScaleFactor));
        }

        /// <summary>
        /// Gets or creates a depth image from the pool and initializes it with a managed <see cref="Bitmap"/> object.
        /// </summary>
        /// <param name="bitmap">A bitmap from which to copy the image data.</param>
        /// <param name="depthValueSemantics">Optional depth value semantics.</param>
        /// <param name="depthValueToMetersScaleFactor">Optional scale factor to convert from depth values to meters.</param>
        /// <returns>A shared depth image from the pool containing a copy of the image data from <paramref name="bitmap"/>.</returns>
        public static Shared<DepthImage> GetOrCreateFrom(Bitmap bitmap, DepthValueSemantics depthValueSemantics = DepthValueSemantics.DistanceToPlane, double depthValueToMetersScaleFactor = 0.001)
        {
            BitmapData sourceData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            Shared<DepthImage> sharedDepthImage = null;
            try
            {
                sharedDepthImage = GetOrCreate(bitmap.Width, bitmap.Height, depthValueSemantics, depthValueToMetersScaleFactor);
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
