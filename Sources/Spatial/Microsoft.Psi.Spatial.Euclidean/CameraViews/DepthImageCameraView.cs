// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a camera view of an depth image.
    /// </summary>
    public class DepthImageCameraView : ImageCameraView<DepthImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageCameraView"/> class.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="cameraIntrinsics">The camera intrinsics.</param>
        /// <param name="cameraPose">The camera pose.</param>
        public DepthImageCameraView(Shared<DepthImage> depthImage, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
            : base(depthImage, cameraIntrinsics, cameraPose)
        {
        }

        /// <summary>
        /// Creates an <see cref="DepthImageCameraView"/> stream from a stream of depth images, a stream of camera intrinsics, and a stream of poses.
        /// </summary>
        /// <param name="depthImage">The stream of depth images.</param>
        /// <param name="cameraIntrinsics">A stream of camera intrinsics.</param>
        /// <param name="cameraPose">A stream of camera poses.</param>
        /// <param name="cameraIntrinsicsInterpolator">The interpolator for the camera intrinsics stream.</param>
        /// <param name="cameraPoseInterpolator">The interpolator for the camera pose stream.</param>
        /// <param name="depthImageDeliveryPolicy">An optional delivery policy for the image stream.</param>
        /// <param name="cameraIntrinsicsDeliveryPolicy">An optional delivery policy for the camera intrinsics stream.</param>
        /// <param name="cameraPoseDeliveryPolicy">An optional delivery policy for the camera pose stream.</param>
        /// <param name="depthImageAndCameraIntrinsicsDeliveryPolicy">An optional delivery policy for the tuple of image and camera intrinsics stream.</param>
        /// <returns>Created <see cref="DepthImageCameraView"/> stream.</returns>
        public static IProducer<DepthImageCameraView> CreateProducer(
            IProducer<Shared<DepthImage>> depthImage,
            IProducer<ICameraIntrinsics> cameraIntrinsics,
            IProducer<CoordinateSystem> cameraPose,
            Interpolator<ICameraIntrinsics> cameraIntrinsicsInterpolator,
            Interpolator<CoordinateSystem> cameraPoseInterpolator,
            DeliveryPolicy<Shared<DepthImage>> depthImageDeliveryPolicy = null,
            DeliveryPolicy<ICameraIntrinsics> cameraIntrinsicsDeliveryPolicy = null,
            DeliveryPolicy<CoordinateSystem> cameraPoseDeliveryPolicy = null,
            DeliveryPolicy<(Shared<DepthImage>, ICameraIntrinsics)> depthImageAndCameraIntrinsicsDeliveryPolicy = null)
        {
            return CreateProducer(
                (image, intrinsics, pose) => new DepthImageCameraView(image, intrinsics, pose),
                depthImage,
                cameraIntrinsics,
                cameraPose,
                cameraIntrinsicsInterpolator,
                cameraPoseInterpolator,
                depthImageDeliveryPolicy,
                cameraIntrinsicsDeliveryPolicy,
                cameraPoseDeliveryPolicy,
                depthImageAndCameraIntrinsicsDeliveryPolicy);
        }
    }
}
