// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Base class for implementing image camera views.
    /// </summary>
    /// <typeparam name="TImage">Type of image.</typeparam>
    public class ImageCameraView<TImage> : CameraView<Shared<TImage>>, IDisposable
        where TImage : class, IImage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCameraView{T}"/> class.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="cameraIntrinsics">The camera intrinsics.</param>
        /// <param name="cameraPose">The camera pose.</param>
        public ImageCameraView(Shared<TImage> image, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
            : base(image?.AddRef(), cameraIntrinsics, cameraPose)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.ViewedObject != null && this.ViewedObject.Resource != null)
            {
                this.ViewedObject.Dispose();
            }
        }
    }
}
