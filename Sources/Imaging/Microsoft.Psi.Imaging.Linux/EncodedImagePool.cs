// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Defines a pool of encoded images
    /// </summary>
    public class EncodedImagePool
    {
        private static readonly SharedPool<EncodedImage> Recycler = new SharedPool<EncodedImage>(10);

        /// <summary>
        /// Retrieves an image from the pool
        /// </summary>
        /// <returns>Image retrieved from the pool</returns>
        public static Shared<EncodedImage> Get()
        {
            return EncodedImagePool.Recycler.GetOrCreate(() => new EncodedImage());
        }
    }
}
