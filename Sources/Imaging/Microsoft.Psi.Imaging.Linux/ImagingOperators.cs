// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using SkiaSharp;

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
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A producer that generates the JPEG images.</returns>
        public static IProducer<Shared<EncodedImage>> EncodeJpeg(
            this IProducer<Shared<Image>> source,
            int quality = 90,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = null)
            => source.Encode(new ImageToJpegStreamEncoder { QualityLevel = quality }, deliveryPolicy, name ?? $"{nameof(EncodeJpeg)}({quality})");

        /// <summary>
        /// Encodes an image to a PNG format.
        /// </summary>
        /// <param name="source">A producer of images to encoder.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A producer that generates the PNG images.</returns>
        public static IProducer<Shared<EncodedImage>> EncodePng(
            this IProducer<Shared<Image>> source,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(EncodePng))
            => source.Encode(new ImageToPngStreamEncoder(), deliveryPolicy, name);

        /// <summary>
        /// Decodes an encoded image.
        /// </summary>
        /// <param name="source">A producer of encoded images to decode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A producer that generates the decoded images.</returns>
        public static IProducer<Shared<Image>> Decode(
            this IProducer<Shared<EncodedImage>> source,
            DeliveryPolicy<Shared<EncodedImage>> deliveryPolicy = null,
            string name = nameof(Decode))
            => source.Decode(new ImageFromStreamDecoder(), deliveryPolicy, name);

        /// <summary>
        /// Encodes a depth image to a PNG format.
        /// </summary>
        /// <param name="source">A producer of depth images to encode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A producer that generates the PNG-encoded depth images.</returns>
        public static IProducer<Shared<EncodedDepthImage>> EncodePng(
            this IProducer<Shared<DepthImage>> source,
            DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null,
            string name = nameof(EncodePng))
            => source.Encode(new DepthImageToPngStreamEncoder(), deliveryPolicy, name);

        /// <summary>
        /// Decodes an encoded depth image.
        /// </summary>
        /// <param name="source">A producer of encoded depth images to decode.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A producer that generates the decoded depth images.</returns>
        public static IProducer<Shared<DepthImage>> Decode(
            this IProducer<Shared<EncodedDepthImage>> source,
            DeliveryPolicy<Shared<EncodedDepthImage>> deliveryPolicy = null,
            string name = nameof(Decode))
            => source.Decode(new DepthImageFromStreamDecoder(), deliveryPolicy, name);

        /// <summary>
        /// Converts an image to a SkiaSharp SKImage.
        /// </summary>
        /// <param name="image">Image to convert to SKImage type.</param>
        /// <returns>SKImage.</returns>
        internal static SKImage AsSKImage(this ImageBase image)
        {
            var data = SKData.Create(image.ImageData, image.Size);
            var colorType = image.PixelFormat switch
            {
                // These are unsupported by SkiaSharp: BGRX_32bpp, BGR_24bpp, Gray_16bpp, RGBA_64bpp
                PixelFormat.BGRA_32bpp => SKColorType.Bgra8888,
                PixelFormat.Gray_8bpp => SKColorType.Gray8,
                PixelFormat.Undefined => SKColorType.Unknown,
                PixelFormat.Gray_16bpp => throw new ArgumentException($"Unsupported pixel format: {image.PixelFormat} (e.g. DepthImage)"),
                _ => throw new ArgumentException($"Unsupported pixel format: {image.PixelFormat}"),
            };
            var info = new SKImageInfo(image.Width, image.Height, colorType);
            return SKImage.FromPixels(info, data, image.Stride);
        }
    }
}