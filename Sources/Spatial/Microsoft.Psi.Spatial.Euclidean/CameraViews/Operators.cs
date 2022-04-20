// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements a variety of operators and extension methods.
    /// </summary>
    public static partial class Operators
    {
        #region Transformation operators

        /// <summary>
        /// Converts the stream of image camera views to a different pixel format.
        /// </summary>
        /// <param name="source">The source stream of image camera views.</param>
        /// <param name="scaleX">Scale factor for X.</param>
        /// <param name="scaleY">Scale factor for Y.</param>
        /// <param name="samplingMode">Method for sampling pixels when rescaling.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator for creating new shared images.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>The resulting stream.</returns>
        public static IProducer<ImageCameraView> Scale(
            this IProducer<ImageCameraView> source,
            float scaleX,
            float scaleY,
            SamplingMode samplingMode = SamplingMode.Bilinear,
            DeliveryPolicy<ImageCameraView> deliveryPolicy = null,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null,
            string name = nameof(Scale))
        {
            sharedImageAllocator ??= (width, height, pixelFormat) => ImagePool.GetOrCreate(width, height, pixelFormat);
            return source.Process<ImageCameraView, ImageCameraView>(
                (imageCameraView, envelope, emitter) =>
                {
                    // if the image is null, post null
                    if (imageCameraView == null)
                    {
                        emitter.Post(null, envelope.OriginatingTime);
                    }
                    else
                    {
                        int finalWidth = (int)(imageCameraView.ViewedObject.Resource.Width * scaleX);
                        int finalHeight = (int)(imageCameraView.ViewedObject.Resource.Height * scaleY);
                        using var scaledSharedImage = sharedImageAllocator(finalWidth, finalHeight, imageCameraView.ViewedObject.Resource.PixelFormat);
                        imageCameraView.ViewedObject.Resource.Scale(scaledSharedImage.Resource, scaleX, scaleY, samplingMode);
                        using var outputImageCameraView = new ImageCameraView(scaledSharedImage, imageCameraView.CameraIntrinsics, imageCameraView.CameraPose);
                        emitter.Post(outputImageCameraView, envelope.OriginatingTime);
                    }
                },
                deliveryPolicy,
                name);
        }

        #endregion

        #region Convert image camera views to a different format

        /// <summary>
        /// Converts the stream of image camera views to a different pixel format.
        /// </summary>
        /// <param name="source">The source stream of image camera views.</param>
        /// <param name="pixelFormat">The pixel format to convert to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator for creating new shared images.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>The resulting stream.</returns>
        public static IProducer<ImageCameraView> Convert(
            this IProducer<ImageCameraView> source,
            PixelFormat pixelFormat,
            DeliveryPolicy<ImageCameraView> deliveryPolicy = null,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null,
            string name = nameof(Convert))
        {
            sharedImageAllocator ??= (width, height, pixelFormat) => ImagePool.GetOrCreate(width, height, pixelFormat);
            return source.Process<ImageCameraView, ImageCameraView>(
                (imageCameraView, envelope, emitter) =>
                {
                    // if the image is null, post null
                    if (imageCameraView == null)
                    {
                        emitter.Post(null, envelope.OriginatingTime);
                    }
                    else if (pixelFormat == imageCameraView.ViewedObject.Resource.PixelFormat)
                    {
                        // o/w if image is already in the requested format, shortcut the conversion
                        emitter.Post(imageCameraView, envelope.OriginatingTime);
                    }
                    else
                    {
                        using var image = sharedImageAllocator(imageCameraView.ViewedObject.Resource.Width, imageCameraView.ViewedObject.Resource.Height, pixelFormat);
                        imageCameraView.ViewedObject.Resource.CopyTo(image.Resource);
                        using var outputImageCameraView = new ImageCameraView(image, imageCameraView.CameraIntrinsics, imageCameraView.CameraPose);
                        emitter.Post(outputImageCameraView, envelope.OriginatingTime);
                    }
                },
                deliveryPolicy,
                name);
        }

        #endregion

        #region Encoded/Decode image and depth image camera views

        /// <summary>
        /// Encodes an image camera view using a specified image encoder.
        /// </summary>
        /// <param name="source">The source stream of image camera views.</param>
        /// <param name="encoder">The image encoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of encoded image camera views.</returns>
        public static IProducer<EncodedImageCameraView> Encode(
            this IProducer<ImageCameraView> source,
            IImageToStreamEncoder encoder,
            DeliveryPolicy<ImageCameraView> deliveryPolicy = null,
            string name = null)
        {
            return source.Process<ImageCameraView, EncodedImageCameraView>(
                (imageCameraView, envelope, emitter) =>
                {
                    var image = imageCameraView.ViewedObject.Resource;
                    using var encodedImage = EncodedImagePool.GetOrCreate(image.Width, image.Height, image.PixelFormat);
                    encodedImage.Resource.EncodeFrom(image, encoder);
                    using var encodedImageCameraView = new EncodedImageCameraView(encodedImage, imageCameraView.CameraIntrinsics, imageCameraView.CameraPose);
                    emitter.Post(encodedImageCameraView, envelope.OriginatingTime);
                },
                deliveryPolicy,
                name ?? $"{nameof(Encode)}({encoder.Description})");
        }

        /// <summary>
        /// Encodes a depth image camera view using a specified image encoder.
        /// </summary>
        /// <param name="source">The source stream of depth image camera views.</param>
        /// <param name="encoder">The image encoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of encoded depth image camera views.</returns>
        public static IProducer<EncodedDepthImageCameraView> Encode(
            this IProducer<DepthImageCameraView> source,
            IDepthImageToStreamEncoder encoder,
            DeliveryPolicy<DepthImageCameraView> deliveryPolicy = null,
            string name = null)
        {
            return source.Process<DepthImageCameraView, EncodedDepthImageCameraView>(
                (depthImageCameraView, envelope, emitter) =>
                {
                    var depthImage = depthImageCameraView.ViewedObject.Resource;
                    using var encodedDepthImage = EncodedDepthImagePool.GetOrCreate(
                        depthImage.Width,
                        depthImage.Height,
                        depthImage.DepthValueSemantics,
                        depthImage.DepthValueToMetersScaleFactor);
                    encodedDepthImage.Resource.EncodeFrom(depthImage, encoder);
                    using var encodedDepthImageCameraView = new EncodedDepthImageCameraView(encodedDepthImage, depthImageCameraView.CameraIntrinsics, depthImageCameraView.CameraPose);
                    emitter.Post(encodedDepthImageCameraView, envelope.OriginatingTime);
                },
                deliveryPolicy,
                name ?? $"{nameof(Encode)}({encoder.Description})");
        }

        /// <summary>
        /// Decodes an encoded image camera view using a specified image decoder.
        /// </summary>
        /// <param name="source">The source stream of encoded image camera views.</param>
        /// <param name="decoder">The image decoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of decoded image camera views.</returns>
        public static IProducer<ImageCameraView> Decode(
            this IProducer<EncodedImageCameraView> source,
            IImageFromStreamDecoder decoder,
            DeliveryPolicy<EncodedImageCameraView> deliveryPolicy = null,
            string name = nameof(Decode))
        {
            return source.Process<EncodedImageCameraView, ImageCameraView>(
                (encodedImageCameraView, envelope, emitter) =>
                {
                    var encodedImage = encodedImageCameraView.ViewedObject.Resource;
                    using var image = ImagePool.GetOrCreate(encodedImage.Width, encodedImage.Height, encodedImage.PixelFormat);
                    image.Resource.DecodeFrom(encodedImage, decoder);
                    using var imageCameraView = new ImageCameraView(image, encodedImageCameraView.CameraIntrinsics, encodedImageCameraView.CameraPose);
                    emitter.Post(imageCameraView, envelope.OriginatingTime);
                },
                deliveryPolicy,
                name);
        }

        /// <summary>
        /// Decodes an encoded depth image camera view using a specified image decoder.
        /// </summary>
        /// <param name="source">The source stream of encoded depth image camera views.</param>
        /// <param name="decoder">The depth image decoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of decoded depth image camera views.</returns>
        public static IProducer<DepthImageCameraView> Decode(
            this IProducer<EncodedDepthImageCameraView> source,
            IDepthImageFromStreamDecoder decoder,
            DeliveryPolicy<EncodedDepthImageCameraView> deliveryPolicy = null,
            string name = nameof(Decode))
        {
            return source.Process<EncodedDepthImageCameraView, DepthImageCameraView>(
                (encodedDepthImageCameraView, envelope, emitter) =>
                {
                    var encodedDepthImage = encodedDepthImageCameraView.ViewedObject.Resource;
                    using var depthImage = DepthImagePool.GetOrCreate(
                        encodedDepthImage.Width,
                        encodedDepthImage.Height,
                        encodedDepthImage.DepthValueSemantics,
                        encodedDepthImage.DepthValueToMetersScaleFactor);
                    depthImage.Resource.DecodeFrom(encodedDepthImage, decoder);
                    using var depthImageCameraView = new DepthImageCameraView(depthImage, encodedDepthImageCameraView.CameraIntrinsics, encodedDepthImageCameraView.CameraPose);
                    emitter.Post(depthImageCameraView, envelope.OriginatingTime);
                },
                deliveryPolicy,
                name);
        }

        #endregion
    }
}
