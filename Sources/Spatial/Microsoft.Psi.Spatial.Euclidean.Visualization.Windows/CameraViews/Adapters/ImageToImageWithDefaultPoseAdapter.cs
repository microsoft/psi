// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from an image to an image with a default pose.
    /// </summary>
    [StreamAdapter]
    public class ImageToImageWithDefaultPoseAdapter : StreamAdapter<Shared<Image>, (Shared<Image>, CoordinateSystem)>
    {
        /// <inheritdoc/>
        public override (Shared<Image>, CoordinateSystem) GetAdaptedValue(Shared<Image> source, Envelope envelope)
            => (source, new CoordinateSystem());
    }
}
