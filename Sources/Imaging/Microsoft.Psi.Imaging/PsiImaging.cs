// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// Implements stream operator methods for Imaging
    /// </summary>
    public static partial class ImagingOperators
    {
        /// <summary>
        /// Operator converts from an Image to an Image with a different pixel format
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="pixelFormat">The pixel format to convert to</param>
        /// <returns>The resulting stream.</returns>
        public static IProducer<Shared<Image>> Convert(this IProducer<Shared<Image>> source, PixelFormat pixelFormat)
        {
            return source.PipeTo(new ToPixelFormat(source.Out.Pipeline, pixelFormat));
        }

        /// <summary>
        /// Operator that converts an image
        /// </summary>
        /// <param name="source">Source image to compress</param>
        /// <param name="transformer">Method for converting an image sample</param>
        /// <param name="pixelFormat">Pixel format to use for converted image</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates the transformed images</returns>
        public static IProducer<Shared<Image>> Transform(this IProducer<Shared<Image>> source, TransformDelegate transformer, PixelFormat pixelFormat, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new TransformImageComponent(source.Out.Pipeline, transformer, pixelFormat), deliveryPolicy);
        }

        /// <summary>
        /// Operator that crops an image
        /// </summary>
        /// <param name="source">Source image to crop</param>
        /// <param name="region">Producer that generates rectangles to crop the image against</param>
        /// <param name="interpolator">Interpolator to use for selecting region</param>
        /// <returns>Returns a producer that generates the cropped images</returns>
        public static IProducer<Shared<Image>> Crop(this IProducer<Shared<Image>> source, IProducer<Rectangle> region, Match.Interpolator<Rectangle> interpolator)
        {
            return source.Join(region, interpolator).Crop();
        }

        /// <summary>
        /// Crops an image sample using the specified rectangle.
        /// </summary>
        /// <param name="source">Source of image and rectangle samples</param>
        /// <returns>Returns a producer generating new cropped image samples</returns>
        public static IProducer<Shared<Image>> Crop(this IProducer<(Shared<Image>, Rectangle)> source)
        {
            Shared<Image> croppedImage = null;
            return source.Select(
               rectWithImage =>
               {
                   croppedImage?.Dispose();
                   return croppedImage = rectWithImage.Item1.Resource.Crop(
                            rectWithImage.Item2.Left,
                            rectWithImage.Item2.Top,
                            rectWithImage.Item2.Width,
                            rectWithImage.Item2.Height);
               });
        }

        /// <summary>
        /// Psi component for converting an image to a grayscale image
        /// </summary>
        /// <param name="source">Image producer to use as source images</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Producers of grayscale images</returns>
        public static IProducer<Shared<Image>> ToGray(this IProducer<Shared<Image>> source, DeliveryPolicy deliveryPolicy = null)
        {
            return Convert(source, PixelFormat.Gray_8bpp, deliveryPolicy);
        }

        /// <summary>
        /// Defines a producer that resizes its input image
        /// </summary>
        /// <param name="source">Image to scale</param>
        /// <param name="finalWidth">Final width of desired output</param>
        /// <param name="finalHeight">Final height of desired output</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates resized images</returns>
        public static IProducer<Shared<Image>> Resize(this IProducer<Shared<Image>> source, float finalWidth, float finalHeight, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Process<Shared<Image>, Shared<Image>>(
                (image, env, emitter) =>
                {
                    float scaleX = finalWidth / image.Resource.Width;
                    float scaleY = finalHeight / image.Resource.Height;
                    emitter.Post(image.Resource.Scale(scaleX, scaleY, SamplingMode.Bilinear), env.OriginatingTime);
                });
        }

        /// <summary>
        /// Defines a producer that mirror its input image about the vertical axis
        /// This method is depracated. Use Flip() instead.
        /// </summary>
        /// <param name="source">Image to mirror</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Returns a producer that generates mirrored images</returns>
        [Obsolete("Mirror is deprecated. Use Flip() instead")]
        public static IProducer<Shared<Image>> Mirror(this IProducer<Shared<Image>> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Process<Shared<Image>, Shared<Image>>(
                (image, env, emitter) =>
                {
                    emitter.Post(image.Resource.Flip(FlipMode.AlongVerticalAxis), env.OriginatingTime);
                });
        }

        /// <summary>
        /// Defines a producer that flips its input image about the horizontal or vertical axis
        /// </summary>
        /// <param name="source">Image to flip</param>
        /// <param name="mode">Axis about which to flip</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>A producer that generates flip images</returns>
        public static IProducer<Shared<Image>> Flip(this IProducer<Shared<Image>> source, FlipMode mode, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Process<Shared<Image>, Shared<Image>>(
                (image, env, emitter) =>
                {
                    emitter.Post(image.Resource.Flip(mode), env.OriginatingTime);
                });
        }

        /// <summary>
        /// Psi component for converting an image to another format
        /// </summary>
        /// <param name="source">Image producer to use as source images</param>
        /// <param name="format">Pixel format to convert to</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Producers of converted images</returns>
        public static IProducer<Shared<Image>> Convert(this IProducer<Shared<Image>> source, PixelFormat format, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Process<Shared<Image>, Shared<Image>>(
                (srcImage, env, e) =>
                {
                    using (var destImage = ImagePool.GetOrCreate(srcImage.Resource.Width, srcImage.Resource.Height, format))
                    {
                        srcImage.Resource.CopyTo(destImage.Resource);
                        e.Post(destImage, env.OriginatingTime);
                    }
                }, deliveryPolicy);
        }

        /// <summary>
        /// Psi component that takes the absolute difference between two images
        /// </summary>
        /// <param name="sources">Images to diff</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Producer that returns the difference image</returns>
        public static IProducer<Shared<Image>> AbsDiff(this IProducer<ValueTuple<Shared<Image>, Shared<Image>>> sources, DeliveryPolicy deliveryPolicy = null)
        {
            return sources.Process<ValueTuple<Shared<Image>, Shared<Image>>, Shared<Image>>(
                (images, env, e) =>
                {
                    Shared<Image> destImage = images.Item1.Resource.AbsDiff(images.Item2.Resource);
                    e.Post(destImage, env.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Psi component that takes the absolute difference between two images
        /// </summary>
        /// <param name="image">Images to threshold</param>
        /// <param name="threshold">Threshold value</param>
        /// <param name="maxvalue">Maximum value</param>
        /// <param name="thresholdType">Type of thresholding to perform</param>
        /// <param name="deliveryPolicy">Delivery policy</param>
        /// <returns>Producer that returns the difference image</returns>
        public static IProducer<Shared<Image>> Threshold(this IProducer<Shared<Image>> image, int threshold, int maxvalue, Threshold thresholdType, DeliveryPolicy deliveryPolicy = null)
        {
            return image.Process<Shared<Image>, Shared<Image>>(
                (srcimage, env, e) =>
                {
                    Shared<Image> destImage = srcimage.Resource.Threshold(threshold, maxvalue, thresholdType);
                    e.Post(destImage, env.OriginatingTime);
                }, deliveryPolicy);
        }
    }
}