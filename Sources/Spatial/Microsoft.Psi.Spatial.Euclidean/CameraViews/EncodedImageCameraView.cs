// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a camera view of an encoded image.
    /// </summary>
    public class EncodedImageCameraView : ImageCameraView<EncodedImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageCameraView"/> class.
        /// </summary>
        /// <param name="encodedImage">The encoded image.</param>
        /// <param name="cameraIntrinsics">The camera intrinsics.</param>
        /// <param name="cameraPose">The camera pose.</param>
        public EncodedImageCameraView(Shared<EncodedImage> encodedImage, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
            : base(encodedImage, cameraIntrinsics, cameraPose)
        {
        }

        /// <summary>
        /// Creates an <see cref="EncodedImageCameraView"/> stream from a stream of encoded images, a stream of camera intrinsics, and a stream of poses.
        /// </summary>
        /// <param name="encodedImage">The stream of encoded images.</param>
        /// <param name="cameraIntrinsics">A stream of camera intrinsics.</param>
        /// <param name="cameraPose">A stream of camera poses.</param>
        /// <param name="cameraIntrinsicsInterpolator">The interpolator for the camera intrinsics stream.</param>
        /// <param name="cameraPoseInterpolator">The interpolator for the camera pose stream.</param>
        /// <param name="encodedImageDeliveryPolicy">An optional delivery policy for the encoded image stream.</param>
        /// <param name="cameraIntrinsicsDeliveryPolicy">An optional delivery policy for the camera intrinsics stream.</param>
        /// <param name="cameraPoseDeliveryPolicy">An optional delivery policy for the camera pose stream.</param>
        /// <param name="encodedImageAndCameraIntrinsicsDeliveryPolicy">An optional delivery policy for the tuple of encoded image and camera intrinsics stream.</param>
        /// <returns>Created <see cref="EncodedImageCameraView"/> stream.</returns>
        public static IProducer<EncodedImageCameraView> CreateProducer(
            IProducer<Shared<EncodedImage>> encodedImage,
            IProducer<ICameraIntrinsics> cameraIntrinsics,
            IProducer<CoordinateSystem> cameraPose,
            Interpolator<ICameraIntrinsics> cameraIntrinsicsInterpolator,
            Interpolator<CoordinateSystem> cameraPoseInterpolator,
            DeliveryPolicy<Shared<EncodedImage>> encodedImageDeliveryPolicy = null,
            DeliveryPolicy<ICameraIntrinsics> cameraIntrinsicsDeliveryPolicy = null,
            DeliveryPolicy<CoordinateSystem> cameraPoseDeliveryPolicy = null,
            DeliveryPolicy<(Shared<EncodedImage>, ICameraIntrinsics)> encodedImageAndCameraIntrinsicsDeliveryPolicy = null)
        {
            return CreateProducer(
                (image, intrinsics, pose) => new EncodedImageCameraView(image, intrinsics, pose),
                encodedImage,
                cameraIntrinsics,
                cameraPose,
                cameraIntrinsicsInterpolator,
                cameraPoseInterpolator,
                encodedImageDeliveryPolicy,
                cameraIntrinsicsDeliveryPolicy,
                cameraPoseDeliveryPolicy,
                encodedImageAndCameraIntrinsicsDeliveryPolicy);
        }
    }
}
