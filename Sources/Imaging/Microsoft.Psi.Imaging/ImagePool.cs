// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing.Imaging;

    /// <summary>
    /// Defines a pool of images
    /// </summary>
    public class ImagePool
    {
        /// <summary>
        /// Retrieves an image from the pool
        /// </summary>
        /// <param name="width">Width of image requested</param>
        /// <param name="height">Height of image requested</param>
        /// <param name="pixelFormat">Pixel format for requested image</param>
        /// <returns>Returns an image from the pool</returns>
        public static Shared<Image> GetOrCreate(int width, int height, PixelFormat pixelFormat)
        {
            return KeyedSharedPool<Image, (int, int, PixelFormat)>.GetOrCreate((width, height, pixelFormat), () => Image.Create(width, height, pixelFormat));
        }

        /// <summary>
        /// Creates a shared image from a managed Bitmap object by retrieving an existing
        /// image from the pool or allocating a new image.
        /// </summary>
        /// <param name="image">Bitmap from which to copy the image data</param>
        /// <returns>Returns an shared image from the pool containing a copy of the image data from "imageData"</returns>
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
