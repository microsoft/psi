// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Provides a pool of shared encoded images.
    /// </summary>
    public static class EncodedImagePool
    {
        private static readonly SharedPool<EncodedImage> Instance = new SharedPool<EncodedImage>(() => new EncodedImage(), 10);

        /// <summary>
        /// Gets or creates an encoded image from the pool.
        /// </summary>
        /// <returns>A shared encoded image from the pool.</returns>
        public static Shared<EncodedImage> GetOrCreate()
        {
            return Instance.GetOrCreate();
        }
    }
}
