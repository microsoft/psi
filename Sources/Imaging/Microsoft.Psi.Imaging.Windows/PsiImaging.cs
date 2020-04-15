// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Implements stream operator methods for Imaging.
    /// </summary>
    public static partial class ImagingOperators
    {
        /// <summary>
        /// Converts from an Image to a compressed (encoded) image.
        /// </summary>
        /// <param name="source">Source image to encode.</param>
        /// <param name="encoderFn">Method to perform encoding.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates the encoded images.</returns>
        public static IProducer<Shared<EncodedImage>> Encode(this IProducer<Shared<Image>> source, Func<BitmapEncoder> encoderFn, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return source.PipeTo(new ImageEncoder(source.Out.Pipeline, encoderFn), deliveryPolicy);
        }

        /// <summary>
        /// Converts from an Image to a compressed JPEG image.
        /// </summary>
        /// <param name="source">Source image to compress.</param>
        /// <param name="quality">JPEG quality to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates the JPEG images.</returns>
        public static IProducer<Shared<EncodedImage>> EncodeJpeg(this IProducer<Shared<Image>> source, int quality = 90, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return Encode(source, () => new JpegBitmapEncoder { QualityLevel = quality }, deliveryPolicy);
        }

        /// <summary>
        /// Converts from an Image to a compressed PNG image.
        /// </summary>
        /// <param name="source">Source image to compress.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates the PNG images.</returns>
        public static IProducer<Shared<EncodedImage>> EncodePng(this IProducer<Shared<Image>> source, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return Encode(source, () => new PngBitmapEncoder(), deliveryPolicy);
        }

        /// <summary>
        /// Decodes an image that was previously encoded.
        /// </summary>
        /// <param name="source">Source image to compress.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates the decoded images.</returns>
        public static IProducer<Shared<Image>> Decode(this IProducer<Shared<EncodedImage>> source, DeliveryPolicy<Shared<EncodedImage>> deliveryPolicy = null)
        {
            return source.PipeTo(new ImageDecoder(source.Out.Pipeline), deliveryPolicy);
        }
    }
}