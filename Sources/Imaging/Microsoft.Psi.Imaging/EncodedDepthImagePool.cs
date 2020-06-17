// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Provides a pool of shared encoded depth images.
    /// </summary>
    public static class EncodedDepthImagePool
    {
        private static readonly KeyedSharedPool<EncodedDepthImage, (int, int)> Instance =
            new KeyedSharedPool<EncodedDepthImage, (int width, int height)>(key => new EncodedDepthImage(key.width, key.height));

        /// <summary>
        /// Gets or creates an encoded depth image from the pool.
        /// </summary>
        /// <returns>A shared encoded depth image from the pool.</returns>
        /// <param name="width">The requested encoded depth image width.</param>
        /// <param name="height">The requested encoded depth image height.</param>
        public static Shared<EncodedDepthImage> GetOrCreate(int width, int height)
        {
            return Instance.GetOrCreate((width, height));
        }
    }
}
