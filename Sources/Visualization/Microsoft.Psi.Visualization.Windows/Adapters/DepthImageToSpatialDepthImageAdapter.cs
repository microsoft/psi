// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of depth image to spatial depth image with default position.
    /// </summary>
    [StreamAdapter]
    public class DepthImageToSpatialDepthImageAdapter : StreamAdapter<Shared<DepthImage>, (Shared<DepthImage>, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageToSpatialDepthImageAdapter"/> class.
        /// </summary>
        public DepthImageToSpatialDepthImageAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<DepthImage>, CoordinateSystem) Adapter(Shared<DepthImage> value, Envelope env)
        {
            return (value, new CoordinateSystem());
        }
    }
}
