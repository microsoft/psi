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
    /// Implements a stream adapter from image and camera intrinsics to an image camera view.
    /// </summary>
    [StreamAdapter]
    public class ImageIntrisicsToImageCameraViewAdapter : StreamAdapter<(Shared<Image>, ICameraIntrinsics), ImageCameraView>
    {
        /// <inheritdoc/>
        public override ImageCameraView GetAdaptedValue((Shared<Image>, ICameraIntrinsics) source, Envelope envelope)
            => new (source.Item1, source.Item2, new CoordinateSystem());
    }
}
