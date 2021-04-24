// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from encoded spatial camera view (shared encoded image with intrinsics and position) to spatial camera view (shared image with intrinsics and position).
    /// </summary>
    [StreamAdapter]
    public class EncodedSpatialCameraViewToSpatialCameraViewAdapter : StreamAdapter<(Shared<EncodedImage>, ICameraIntrinsics, CoordinateSystem), (Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        private readonly EncodedImageToImageAdapter imageAdapter = new EncodedImageToImageAdapter();

        /// <inheritdoc/>
        public override (Shared<Image>, ICameraIntrinsics, CoordinateSystem) GetAdaptedValue((Shared<EncodedImage>, ICameraIntrinsics, CoordinateSystem) source, Envelope envelope)
            => (this.imageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2, source.Item3);

        /// <inheritdoc/>
        public override void Dispose((Shared<Image>, ICameraIntrinsics, CoordinateSystem) destination)
            => this.imageAdapter.Dispose(destination.Item1);
    }
}
