// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements operators for processing spatial image types.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Encodes an image rectangle using a specified image encoder.
        /// </summary>
        /// <param name="source">The source stream of image rectangles.</param>
        /// <param name="encoder">The image encoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of encoded image rectangles.</returns>
        public static IProducer<EncodedImageRectangle3D> Encode(
            this IProducer<ImageRectangle3D> source,
            IImageToStreamEncoder encoder,
            DeliveryPolicy<ImageRectangle3D> deliveryPolicy = null)
        {
            return source.Process<ImageRectangle3D, EncodedImageRectangle3D>(
                (imageRectangle, envelope, emitter) =>
                {
                    var image = imageRectangle.Image.Resource;
                    using var encodedImage = EncodedImagePool.GetOrCreate(image.Width, image.Height, image.PixelFormat);
                    encodedImage.Resource.EncodeFrom(image, encoder);
                    emitter.Post(new EncodedImageRectangle3D(imageRectangle.Rectangle3D, encodedImage), envelope.OriginatingTime);
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Encodes a depth image rectangle using a specified depth image encoder.
        /// </summary>
        /// <param name="source">The source stream of depth image rectangles.</param>
        /// <param name="encoder">The depth image encoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of encoded depth image rectangles.</returns>
        public static IProducer<EncodedDepthImageRectangle3D> Encode(
            this IProducer<DepthImageRectangle3D> source,
            IDepthImageToStreamEncoder encoder,
            DeliveryPolicy<DepthImageRectangle3D> deliveryPolicy = null)
        {
            return source.Process<DepthImageRectangle3D, EncodedDepthImageRectangle3D>(
                (depthImageRectangle, envelope, emitter) =>
                {
                    var depthImage = depthImageRectangle.DepthImage.Resource;
                    using var encodedDepthImage = EncodedDepthImagePool.GetOrCreate(depthImage.Width, depthImage.Height);
                    encodedDepthImage.Resource.EncodeFrom(depthImage, encoder);
                    emitter.Post(new EncodedDepthImageRectangle3D(depthImageRectangle.Rectangle3D, encodedDepthImage), envelope.OriginatingTime);
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Decodes an encoded image image rectangle using a specified image decoder.
        /// </summary>
        /// <param name="source">The source stream of encoded image rectangles.</param>
        /// <param name="decoder">The image decoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of decoded image rectangles.</returns>
        public static IProducer<ImageRectangle3D> Decode(
            this IProducer<EncodedImageRectangle3D> source,
            IImageFromStreamDecoder decoder,
            DeliveryPolicy<EncodedImageRectangle3D> deliveryPolicy = null)
        {
            return source.Process<EncodedImageRectangle3D, ImageRectangle3D>(
                (encodedImageRectangle, envelope, emitter) =>
                {
                    var encodedImage = encodedImageRectangle.Image.Resource;
                    using var image = ImagePool.GetOrCreate(encodedImage.Width, encodedImage.Height, encodedImage.PixelFormat);
                    image.Resource.DecodeFrom(encodedImage, decoder);
                    emitter.Post(new ImageRectangle3D(encodedImageRectangle.Rectangle3D, image), envelope.OriginatingTime);
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Decodes an encoded depth image image rectangle using a specified depth image decoder.
        /// </summary>
        /// <param name="source">The source stream of encoded depth image rectangles.</param>
        /// <param name="decoder">The depth image decoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of decoded depth image rectangles.</returns>
        public static IProducer<DepthImageRectangle3D> Decode(
            this IProducer<EncodedDepthImageRectangle3D> source,
            IDepthImageFromStreamDecoder decoder,
            DeliveryPolicy<EncodedDepthImageRectangle3D> deliveryPolicy = null)
        {
            return source.Process<EncodedDepthImageRectangle3D, DepthImageRectangle3D>(
                (encodedDepthImageRectangle, envelope, emitter) =>
                {
                    var encodedDepthImage = encodedDepthImageRectangle.DepthImage.Resource;
                    using var depthImage = DepthImagePool.GetOrCreate(encodedDepthImage.Width, encodedDepthImage.Height);
                    depthImage.Resource.DecodeFrom(encodedDepthImage, decoder);
                    emitter.Post(new DepthImageRectangle3D(encodedDepthImageRectangle.Rectangle3D, depthImage), envelope.OriginatingTime);
                },
                deliveryPolicy);
        }
    }
}
