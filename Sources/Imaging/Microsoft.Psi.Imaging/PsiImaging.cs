// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// Implements stream operator methods for Imaging.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts the source image to a different pixel format.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="pixelFormat">The pixel format to convert to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The resulting stream.</returns>
        public static IProducer<Shared<Image>> Convert(this IProducer<Shared<Image>> source, PixelFormat pixelFormat, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new ToPixelFormat(source.Out.Pipeline, pixelFormat), deliveryPolicy);
        }

        /// <summary>
        /// Converts an image to a different pixel format using the specified transformer.
        /// </summary>
        /// <param name="source">Source image to compress.</param>
        /// <param name="transformer">Method for converting an image sample.</param>
        /// <param name="pixelFormat">Pixel format to use for converted image.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates the transformed images.</returns>
        public static IProducer<Shared<Image>> Transform(this IProducer<Shared<Image>> source, TransformDelegate transformer, PixelFormat pixelFormat, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new ImageTransformer(source.Out.Pipeline, transformer, pixelFormat), deliveryPolicy);
        }

        /// <summary>
        /// Crops an image using the specified rectangle.
        /// </summary>
        /// <param name="source">Source of image and rectangle samples.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer generating new cropped image samples.</returns>
        public static IProducer<Shared<Image>> Crop(this IProducer<(Shared<Image>, Rectangle)> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Process<(Shared<Image>, Rectangle), Shared<Image>>(
                (rectWithImage, env, e) =>
                {
                    using (var croppedImage = rectWithImage.Item1.Resource.Crop(
                            rectWithImage.Item2.Left,
                            rectWithImage.Item2.Top,
                            rectWithImage.Item2.Width,
                            rectWithImage.Item2.Height))
                    {
                        e.Post(croppedImage, env.OriginatingTime);
                    }
                }, deliveryPolicy);
        }

        /// <summary>
        /// Converts an image to grayscale.
        /// </summary>
        /// <param name="source">Image producer to use as source images.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Producers of grayscale images.</returns>
        public static IProducer<Shared<Image>> ToGray(this IProducer<Shared<Image>> source, DeliveryPolicy deliveryPolicy = null)
        {
            return Convert(source, PixelFormat.Gray_8bpp, deliveryPolicy);
        }

        /// <summary>
        /// Resizes an image.
        /// </summary>
        /// <param name="source">Image to scale.</param>
        /// <param name="finalWidth">Final width of desired output.</param>
        /// <param name="finalHeight">Final height of desired output.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates resized images.</returns>
        public static IProducer<Shared<Image>> Resize(this IProducer<Shared<Image>> source, float finalWidth, float finalHeight, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Process<Shared<Image>, Shared<Image>>(
                (image, env, emitter) =>
                {
                    float scaleX = finalWidth / image.Resource.Width;
                    float scaleY = finalHeight / image.Resource.Height;
                    using (var resizedImage = image.Resource.Scale(scaleX, scaleY, SamplingMode.Bilinear))
                    {
                        emitter.Post(resizedImage, env.OriginatingTime);
                    }
                }, deliveryPolicy);
        }

        /// <summary>
        /// Flips an image about the horizontal or vertical axis.
        /// </summary>
        /// <param name="source">Image to flip.</param>
        /// <param name="mode">Axis about which to flip.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates flip images.</returns>
        public static IProducer<Shared<Image>> Flip(this IProducer<Shared<Image>> source, FlipMode mode, DeliveryPolicy deliveryPolicy = null)
        {
            if (mode == FlipMode.None)
            {
                // just post original image in the case of a no-op
                return source.Process<Shared<Image>, Shared<Image>>(
                    (image, env, emitter) => emitter.Post(image, env.OriginatingTime), deliveryPolicy);
            }
            else
            {
                return source.Process<Shared<Image>, Shared<Image>>(
                    (image, env, emitter) =>
                    {
                        using (var flippedImage = image.Resource.Flip(mode))
                        {
                            emitter.Post(flippedImage, env.OriginatingTime);
                        }
                    }, deliveryPolicy);
            }
        }

        /// <summary>
        /// Computes the absolute difference between two images.
        /// </summary>
        /// <param name="sources">Images to diff.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Producer that returns the difference image.</returns>
        public static IProducer<Shared<Image>> AbsDiff(this IProducer<(Shared<Image>, Shared<Image>)> sources, DeliveryPolicy deliveryPolicy = null)
        {
            return sources.Process<ValueTuple<Shared<Image>, Shared<Image>>, Shared<Image>>(
                (images, env, e) =>
                {
                    using (var destImage = images.Item1.Resource.AbsDiff(images.Item2.Resource))
                    {
                        e.Post(destImage, env.OriginatingTime);
                    }
                }, deliveryPolicy);
        }

        /// <summary>
        /// Thresholds the image. See Threshold for what modes of thresholding are available.
        /// </summary>
        /// <param name="image">Images to threshold.</param>
        /// <param name="threshold">Threshold value.</param>
        /// <param name="maxvalue">Maximum value.</param>
        /// <param name="thresholdType">Type of thresholding to perform.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Producer that returns the difference image.</returns>
        public static IProducer<Shared<Image>> Threshold(this IProducer<Shared<Image>> image, int threshold, int maxvalue, Threshold thresholdType, DeliveryPolicy deliveryPolicy = null)
        {
            return image.Process<Shared<Image>, Shared<Image>>(
                (srcimage, env, e) =>
                {
                    using (var destImage = srcimage.Resource.Threshold(threshold, maxvalue, thresholdType))
                    {
                        e.Post(destImage, env.OriginatingTime);
                    }
                }, deliveryPolicy);
        }
    }
}