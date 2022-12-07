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
        private static readonly KeyedSharedPool<Image, int> Instance = new (allocationSize => new Image(allocationSize));
        private static int imageAllocationBlockSize = 1;

        /// <summary>
        /// Resets the pool of shared images.
        /// </summary>
        /// <param name="imageAllocationBlockSize">The image allocation block size to use.</param>
        /// <param name="clearLiveObjects">Indicates whether to clear any live objects.</param>
        /// <remarks>
        /// If the clearLiveObjects flag is false, an exception is thrown if a reset is attempted while the pool
        /// still contains live objects.
        /// </remarks>
        public static void Reset(int imageAllocationBlockSize, bool clearLiveObjects = false)
        {
            Instance.Reset(clearLiveObjects);
            ImagePool.imageAllocationBlockSize = imageAllocationBlockSize;
        }

        /// <summary>
        /// Gets or creates an image from the pool.
        /// </summary>
        /// <param name="width">The requested image width.</param>
        /// <param name="height">The requested image height.</param>
        /// <param name="pixelFormat">The requested image pixel format.</param>
        /// <returns>A shared image from the pool.</returns>
        public static Shared<Image> GetOrCreate(int width, int height, PixelFormat pixelFormat)
        {
            var size = height * 4 * ((width * pixelFormat.GetBytesPerPixel() + 3) / 4);
            var allocationSize = ((size + imageAllocationBlockSize - 1) / imageAllocationBlockSize) * imageAllocationBlockSize;
            var sharedImage = Instance.GetOrCreate(allocationSize);
            sharedImage.Resource.Initialize(width, height, pixelFormat);
            return sharedImage;
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
