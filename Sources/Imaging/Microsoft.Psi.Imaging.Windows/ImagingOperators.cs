﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Implements stream operator methods for Imaging.
    /// </summary>
    public static partial class ImagingOperators
    {
        /// <summary>
        /// Encodes an image to a JPEG format.
        /// </summary>
        /// <param name="source">A producer of images to encode.</param>
        /// <param name="quality">JPEG quality to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the JPEG images.</returns>
        public static IProducer<Shared<EncodedImage>> EncodeJpeg(this IProducer<Shared<Image>> source, int quality = 90, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return source.Encode(new ImageToJpegStreamEncoder { QualityLevel = quality }, deliveryPolicy);
        }

        /// <summary>
        /// Encodes an image to a PNG format.
        /// </summary>
        /// <param name="source">A producer of images to encoder.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the PNG images.</returns>
        public static IProducer<Shared<EncodedImage>> EncodePng(this IProducer<Shared<Image>> source, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return source.Encode(new ImageToPngStreamEncoder(), deliveryPolicy);
        }

        /// <summary>
        /// Decodes an encoded image.
        /// </summary>
        /// <param name="source">A producer of encoded images to decode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the decoded images.</returns>
        public static IProducer<Shared<Image>> Decode(this IProducer<Shared<EncodedImage>> source, DeliveryPolicy<Shared<EncodedImage>> deliveryPolicy = null)
        {
            return source.Decode(new ImageFromStreamDecoder(), deliveryPolicy);
        }

        /// <summary>
        /// Encodes a depth image to a PNG format.
        /// </summary>
        /// <param name="source">A producer of depth images to encode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the PNG-encoded depth images.</returns>
        public static IProducer<Shared<EncodedDepthImage>> EncodePng(this IProducer<Shared<DepthImage>> source, DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null)
        {
            return source.Encode(new DepthImageToPngStreamEncoder(), deliveryPolicy);
        }

        /// <summary>
        /// Encodes a depth image to a TIFF format.
        /// </summary>
        /// <param name="source">A producer of depth images to encode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the TIFF-encoded depth images.</returns>
        public static IProducer<Shared<EncodedDepthImage>> EncodeTiff(this IProducer<Shared<DepthImage>> source, DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null)
        {
            return source.Encode(new DepthImageToTiffStreamEncoder(), deliveryPolicy);
        }

        /// <summary>
        /// Decodes an encoded depth image.
        /// </summary>
        /// <param name="source">A producer of encoded depth images to decode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the decoded depth images.</returns>
        public static IProducer<Shared<DepthImage>> Decode(this IProducer<Shared<EncodedDepthImage>> source, DeliveryPolicy<Shared<EncodedDepthImage>> deliveryPolicy = null)
        {
            return source.Decode(new DepthImageFromStreamDecoder(), deliveryPolicy);
        }
    }
}