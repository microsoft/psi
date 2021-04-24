// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from encoded spatial depth image (shared encoded depth image with position) to spatial depth image (shared depth image with position).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialDepthImageToSpatialDepthImageAdapter : StreamAdapter<(Shared<EncodedDepthImage>, CoordinateSystem), (Shared<DepthImage>, CoordinateSystem)>
    {
        private readonly EncodedDepthImageToDepthImageAdapter depthImageAdapter = new EncodedDepthImageToDepthImageAdapter();

        /// <inheritdoc/>
        public override (Shared<DepthImage>, CoordinateSystem) GetAdaptedValue((Shared<EncodedDepthImage>, CoordinateSystem) source, Envelope envelope)
            => (this.depthImageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2);

        /// <inheritdoc/>
        public override void Dispose((Shared<DepthImage>, CoordinateSystem) destination)
            => this.depthImageAdapter.Dispose(destination.Item1);
    }
}
