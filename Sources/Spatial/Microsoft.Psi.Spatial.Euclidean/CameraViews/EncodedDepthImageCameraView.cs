// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a camera view of an encoded depth image.
    /// </summary>
    public class EncodedDepthImageCameraView : ImageCameraView<EncodedDepthImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImageCameraView"/> class.
        /// </summary>
        /// <param name="encodedDepthImage">The viewed encoded depth image.</param>
        /// <param name="cameraIntrinsics">The camera intrinsics.</param>
        /// <param name="cameraPose">The camera pose.</param>
        public EncodedDepthImageCameraView(Shared<EncodedDepthImage> encodedDepthImage, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
            : base(encodedDepthImage, cameraIntrinsics, cameraPose)
        {
        }

        /// <summary>
        /// Creates an <see cref="EncodedDepthImageCameraView"/> stream from a stream of encoded depth images, a stream of camera intrinsics, and a stream of poses.
        /// </summary>
        /// <param name="encodedDepthImage">The stream of encoded images.</param>
        /// <param name="cameraIntrinsics">A stream of camera intrinsics.</param>
        /// <param name="cameraPose">A stream of camera poses.</param>
        /// <param name="cameraIntrinsicsInterpolator">The interpolator for the camera intrinsics stream.</param>
        /// <param name="cameraPoseInterpolator">The interpolator for the camera pose stream.</param>
        /// <param name="encodedDepthImageDeliveryPolicy">An optional delivery policy for the encoded image stream.</param>
        /// <param name="cameraIntrinsicsDeliveryPolicy">An optional delivery policy for the camera intrinsics stream.</param>
        /// <param name="cameraPoseDeliveryPolicy">An optional delivery policy for the camera pose stream.</param>
        /// <param name="encodedDepthImageAndCameraIntrinsicsDeliveryPolicy">An optional delivery policy for the tuple of encoded image and camera intrinsics stream.</param>
        /// <returns>Created <see cref="EncodedDepthImageCameraView"/> stream.</returns>
        public static IProducer<EncodedDepthImageCameraView> CreateProducer(
            IProducer<Shared<EncodedDepthImage>> encodedDepthImage,
            IProducer<ICameraIntrinsics> cameraIntrinsics,
            IProducer<CoordinateSystem> cameraPose,
            Interpolator<ICameraIntrinsics> cameraIntrinsicsInterpolator,
            Interpolator<CoordinateSystem> cameraPoseInterpolator,
            DeliveryPolicy<Shared<EncodedDepthImage>> encodedDepthImageDeliveryPolicy = null,
            DeliveryPolicy<ICameraIntrinsics> cameraIntrinsicsDeliveryPolicy = null,
            DeliveryPolicy<CoordinateSystem> cameraPoseDeliveryPolicy = null,
            DeliveryPolicy<(Shared<EncodedDepthImage>, ICameraIntrinsics)> encodedDepthImageAndCameraIntrinsicsDeliveryPolicy = null)
        {
            return CreateProducer(
                (image, intrinsics, pose) => new EncodedDepthImageCameraView(image, intrinsics, pose),
                encodedDepthImage,
                cameraIntrinsics,
                cameraPose,
                cameraIntrinsicsInterpolator,
                cameraPoseInterpolator,
                encodedDepthImageDeliveryPolicy,
                cameraIntrinsicsDeliveryPolicy,
                cameraPoseDeliveryPolicy,
                encodedDepthImageAndCameraIntrinsicsDeliveryPolicy);
        }
    }
}
