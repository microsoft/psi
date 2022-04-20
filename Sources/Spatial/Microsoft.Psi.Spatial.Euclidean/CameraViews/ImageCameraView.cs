// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a camera view of an image.
    /// </summary>
    public class ImageCameraView : ImageCameraView<Image>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCameraView"/> class.
        /// </summary>
        /// <param name="image">The image viewed by the spatial camera.</param>
        /// <param name="cameraIntrinsics">Intrinsics of the camera.</param>
        /// <param name="cameraPose">Pose of the camera.</param>
        public ImageCameraView(Shared<Image> image, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
            : base(image, cameraIntrinsics, cameraPose)
        {
        }

        /// <summary>
        /// Creates an <see cref="ImageCameraView"/> stream from a stream of images, a stream of camera intrinsics, and a stream of poses.
        /// </summary>
        /// <param name="image">The stream of images.</param>
        /// <param name="cameraIntrinsics">A stream of camera intrinsics.</param>
        /// <param name="cameraPose">A stream of camera poses.</param>
        /// <param name="cameraIntrinsicsInterpolator">The interpolator for the camera intrinsics stream.</param>
        /// <param name="cameraPoseInterpolator">The interpolator for the camera pose stream.</param>
        /// <param name="imageDeliveryPolicy">An optional delivery policy for the image stream.</param>
        /// <param name="cameraIntrinsicsDeliveryPolicy">An optional delivery policy for the camera intrinsics stream.</param>
        /// <param name="cameraPoseDeliveryPolicy">An optional delivery policy for the camera pose stream.</param>
        /// <param name="imageAndCameraIntrinsicsDeliveryPolicy">An optional delivery policy for the tuple of images and camera intrinsics stream.</param>
        /// <returns>Created <see cref="ImageCameraView"/> stream.</returns>
        public static IProducer<ImageCameraView> CreateProducer(
            IProducer<Shared<Image>> image,
            IProducer<ICameraIntrinsics> cameraIntrinsics,
            IProducer<CoordinateSystem> cameraPose,
            Interpolator<ICameraIntrinsics> cameraIntrinsicsInterpolator,
            Interpolator<CoordinateSystem> cameraPoseInterpolator,
            DeliveryPolicy<Shared<Image>> imageDeliveryPolicy = null,
            DeliveryPolicy<ICameraIntrinsics> cameraIntrinsicsDeliveryPolicy = null,
            DeliveryPolicy<CoordinateSystem> cameraPoseDeliveryPolicy = null,
            DeliveryPolicy<(Shared<Image>, ICameraIntrinsics)> imageAndCameraIntrinsicsDeliveryPolicy = null)
        {
            return CreateProducer(
                (image, intrinsics, pose) => new ImageCameraView(image, intrinsics, pose),
                image,
                cameraIntrinsics,
                cameraPose,
                cameraIntrinsicsInterpolator,
                cameraPoseInterpolator,
                imageDeliveryPolicy,
                cameraIntrinsicsDeliveryPolicy,
                cameraPoseDeliveryPolicy,
                imageAndCameraIntrinsicsDeliveryPolicy);
        }
    }
}
