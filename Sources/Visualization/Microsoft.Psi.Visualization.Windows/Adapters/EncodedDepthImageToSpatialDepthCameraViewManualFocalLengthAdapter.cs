// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from shared encoded depth image to shared depth image with default position.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageToSpatialDepthCameraViewManualFocalLengthAdapter : StreamAdapter<Shared<EncodedDepthImage>, (Shared<DepthImage>, CoordinateSystem)>
    {
        private readonly EncodedDepthImageToDepthImageAdapter encodedDepthImageAdapter = new EncodedDepthImageToDepthImageAdapter();

        /// <inheritdoc/>
        public override (Shared<DepthImage>, CoordinateSystem) GetAdaptedValue(Shared<EncodedDepthImage> source, Envelope envelope)
            => (this.encodedDepthImageAdapter.GetAdaptedValue(source, envelope), new CoordinateSystem());

        /// <inheritdoc/>
        public override void Dispose((Shared<DepthImage>, CoordinateSystem) destination)
            => this.encodedDepthImageAdapter.Dispose(destination.Item1);
    }
}
