// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from depth image to spatial depth image with default position.
    /// </summary>
    [StreamAdapter]
    public class DepthImageToDepthImageWithDefaultPoseAdapter : StreamAdapter<Shared<DepthImage>, (Shared<DepthImage>, CoordinateSystem)>
    {
        /// <inheritdoc/>
        public override (Shared<DepthImage>, CoordinateSystem) GetAdaptedValue(Shared<DepthImage> source, Envelope envelope)
            => (source, new CoordinateSystem());
    }
}
