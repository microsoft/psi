// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from shared image to spatial image (shared image with default position).
    /// </summary>
    [StreamAdapter]
    public class ImageToSpatialImageAdapter : StreamAdapter<Shared<Image>, (Shared<Image>, CoordinateSystem)>
    {
        /// <inheritdoc/>
        public override (Shared<Image>, CoordinateSystem) GetAdaptedValue(Shared<Image> source, Envelope envelope)
            => (source, new CoordinateSystem());
    }
}
