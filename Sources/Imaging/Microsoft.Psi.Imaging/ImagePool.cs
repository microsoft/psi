// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing.Imaging;

    /// <summary>
    /// Provides a pool of shared images.
    /// </summary>
    public static class ImagePool
    {
        private static readonly KeyedSharedPool<Image, (int, int, PixelFormat)> Instance =
            new KeyedSharedPool<Image, (int width, int height, PixelFormat format)>(key => Image.Create(key.width, key.height, key.format));

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
        /// <param name="image">A bitmap from which to copy the image data.</param>
        /// <returns>A shared image from the pool containing a copy of the image data from <paramref name="image"/>.</returns>
        public static Shared<Image> GetOrCreate(System.Drawing.Bitmap image)
        {
            BitmapData sourceData = image.LockBits(
                new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                image.PixelFormat);
            Shared<Image> sharedImg = null;
            try
            {
                sharedImg = GetOrCreate(image.Width, image.Height, PixelFormatHelper.FromSystemPixelFormat(image.PixelFormat));
                unsafe
                {
                    Buffer.MemoryCopy(sourceData.Scan0.ToPointer(), sharedImg.Resource.ImageData.ToPointer(), sourceData.Stride * sourceData.Height, sourceData.Stride * sourceData.Height);
                }
            }
            finally
            {
                image.UnlockBits(sourceData);
            }

            return sharedImg;
        }
    }
}
