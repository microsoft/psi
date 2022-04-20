// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from an encoded image with intrinsics to an image camera view.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageIntrinsicsToImageCameraViewAdapter : StreamAdapter<(Shared<EncodedImage>, ICameraIntrinsics), ImageCameraView>
    {
        private readonly EncodedImageToImageAdapter imageAdapter = new ();

        /// <inheritdoc/>
        public override ImageCameraView GetAdaptedValue((Shared<EncodedImage>, ICameraIntrinsics) source, Envelope envelope)
            => new (this.imageAdapter.GetAdaptedValue(source.Item1, envelope), source.Item2, new CoordinateSystem());

        /// <inheritdoc/>
        public override void Dispose(ImageCameraView destination)
            => this.imageAdapter.Dispose(destination.ViewedObject);
    }
}
