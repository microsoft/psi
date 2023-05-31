// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from an encoded depth image to a depth image with default position.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageToDepthImageWithDefaultPoseAdapter : StreamAdapter<Shared<EncodedDepthImage>, (Shared<DepthImage>, CoordinateSystem)>
    {
        private readonly EncodedDepthImageToDepthImageAdapter encodedDepthImageAdapter = new ();

        /// <inheritdoc/>
        public override (Shared<DepthImage>, CoordinateSystem) GetAdaptedValue(Shared<EncodedDepthImage> source, Envelope envelope)
            => (this.encodedDepthImageAdapter.GetAdaptedValue(source, envelope), new CoordinateSystem());

        /// <inheritdoc/>
        public override void Dispose((Shared<DepthImage>, CoordinateSystem) destination)
            => this.encodedDepthImageAdapter.Dispose(destination.Item1);
    }
}
