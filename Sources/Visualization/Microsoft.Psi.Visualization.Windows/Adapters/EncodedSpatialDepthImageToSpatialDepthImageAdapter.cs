// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of spatial depth image (encoded) to spatial depth image (decoded).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialDepthImageToSpatialDepthImageAdapter : StreamAdapter<(Shared<EncodedDepthImage>, CoordinateSystem), (Shared<DepthImage>, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedSpatialDepthImageToSpatialDepthImageAdapter"/> class.
        /// </summary>
        public EncodedSpatialDepthImageToSpatialDepthImageAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<DepthImage>, CoordinateSystem) Adapter((Shared<EncodedDepthImage>, CoordinateSystem) value, Envelope env)
        {
            return (value.Item1?.Decode(), value.Item2);
        }
    }
}
