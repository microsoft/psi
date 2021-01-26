// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of image to spatial image with default position.
    /// </summary>
    [StreamAdapter]
    public class ImageToSpatialImageAdapter : StreamAdapter<Shared<Image>, (Shared<Image>, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageToSpatialImageAdapter"/> class.
        /// </summary>
        public ImageToSpatialImageAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<Image>, CoordinateSystem) Adapter(Shared<Image> value, Envelope env)
        {
            return (value, new CoordinateSystem());
        }
    }
}
