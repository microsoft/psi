// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from camera view (shared encoded image with intrinsics) to spatial camera view (shared image with intrinsics) with default position.
    /// </summary>
    [StreamAdapter]
    public class EncodedCameraViewToSpatialCameraViewAdapter : StreamAdapter<(Shared<EncodedImage>, ICameraIntrinsics), (Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        private readonly EncodedImageToImageAdapter imageAdapter = new EncodedImageToImageAdapter();

        /// <inheritdoc/>
        public override (Shared<Image>, ICameraIntrinsics, CoordinateSystem) GetAdaptedValue((Shared<EncodedImage>, ICameraIntrinsics) source, Envelope envelope)
            => (this.imageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2, new CoordinateSystem());

        /// <inheritdoc/>
        public override void Dispose((Shared<Image>, ICameraIntrinsics, CoordinateSystem) destination)
            => this.imageAdapter.Dispose(destination.Item1);
    }
}
