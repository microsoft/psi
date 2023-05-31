// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Provides a pool of shared encoded depth images.
    /// </summary>
    public static class EncodedDepthImagePool
    {
        private static readonly KeyedSharedPool<EncodedDepthImage, (int, int, DepthValueSemantics, double)> Instance =
            new KeyedSharedPool<EncodedDepthImage, (int width, int height, DepthValueSemantics depthValueSemantics, double depthValueToMetersScaleFactor)>(key =>
            new EncodedDepthImage(key.width, key.height, key.depthValueSemantics, key.depthValueToMetersScaleFactor));

        /// <summary>
        /// Gets or creates an encoded depth image from the pool.
        /// </summary>
        /// <returns>A shared encoded depth image from the pool.</returns>
        /// <param name="width">The requested encoded depth image width.</param>
        /// <param name="height">The requested encoded depth image height.</param>
        /// <param name="depthValueSemantics">Optional requested depth value semantics.</param>
        /// <param name="depthValueToMetersScaleFactor">Optional scale factor to convert from depth values to meters.</param>
        public static Shared<EncodedDepthImage> GetOrCreate(int width, int height, DepthValueSemantics depthValueSemantics = DepthValueSemantics.DistanceToPlane, double depthValueToMetersScaleFactor = 0.001)
        {
            return Instance.GetOrCreate((width, height, depthValueSemantics, depthValueToMetersScaleFactor));
        }
    }
}
