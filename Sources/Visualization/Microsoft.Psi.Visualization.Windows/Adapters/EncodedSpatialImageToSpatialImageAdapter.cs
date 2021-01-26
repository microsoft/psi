// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of spatial image (encoded) to spatial image (decoded).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialImageToSpatialImageAdapter : StreamAdapter<(Shared<EncodedImage>, CoordinateSystem), (Shared<Image>, CoordinateSystem)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedSpatialImageToSpatialImageAdapter"/> class.
        /// </summary>
        public EncodedSpatialImageToSpatialImageAdapter()
            : base(Adapter)
        {
        }

        private static (Shared<Image>, CoordinateSystem) Adapter((Shared<EncodedImage>, CoordinateSystem) value, Envelope env)
        {
            return (value.Item1?.Decode(), value.Item2);
        }
    }
}
