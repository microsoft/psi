// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Implements operators for processing streams of images.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts a stream of images into a stream of depth images.
        /// </summary>
        /// <param name="source">A producer of images.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedDepthImageAllocator ">Optional image allocator for creating new shared depth image.</param>
        /// <returns>A corresponding stream of depth images.</returns>
        /// <remarks>The images in the source stream need to be in <see cref="PixelFormat.Gray_16bpp"/> format.</remarks>
        public static IProducer<Shared<DepthImage>> ToDepthImage(this IProducer<Shared<Image>> source, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, Shared<DepthImage>> sharedDepthImageAllocator = null)
        {
            sharedDepthImageAllocator ??= DepthImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<DepthImage>>(
                (sharedImage, envelope, emitter) =>
                {
                    using var sharedDepthImage = sharedDepthImageAllocator(sharedImage.Resource.Width, sharedImage.Resource.Height);
                    sharedDepthImage.Resource.CopyFrom(sharedImage.Resource);
                    emitter.Post(sharedDepthImage, envelope.OriginatingTime);
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Converts a stream of depth images into a stream of <see cref="PixelFormat.Gray_16bpp"/> format images.
        /// </summary>
        /// <param name="source">A producer of depth images.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedDepthImageAllocator ">Optional image allocator for creating new shared depth image.</param>
        /// <returns>A corresponding stream of images.</returns>
        public static IProducer<Shared<Image>> ToImage(this IProducer<Shared<DepthImage>> source, DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null, Func<int, int, Shared<Image>> sharedDepthImageAllocator = null)
        {
            sharedDepthImageAllocator ??= (width, height) => ImagePool.GetOrCreate(width, height, PixelFormat.Gray_16bpp);
            return source.Process<Shared<DepthImage>, Shared<Image>>(
                (sharedDepthImage, envelope, emitter) =>
                {
                    using var sharedImage = sharedDepthImageAllocator(sharedDepthImage.Resource.Width, sharedDepthImage.Resource.Height);
                    sharedImage.Resource.CopyFrom(sharedDepthImage.Resource);
                    emitter.Post(sharedImage, envelope.OriginatingTime);
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Converts the source image to a different pixel format.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="pixelFormat">The pixel format to convert to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator for creating new shared image.</param>
        /// <returns>The resulting stream.</returns>
        public static IProducer<Shared<Image>> ToPixelFormat(this IProducer<Shared<Image>> source, PixelFormat pixelFormat, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            return source.PipeTo(new ToPixelFormat(source.Out.Pipeline, pixelFormat, sharedImageAllocator), deliveryPolicy);
        }

        /// <summary>
        /// Converts a shared image to a different pixel format using the specified transformer.
        /// </summary>
        /// <param name="source">Source image to compress.</param>
        /// <param name="transformer">Method for converting an image sample.</param>
        /// <param name="pixelFormat">Pixel format to use for converted image.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator for creating new shared image.</param>
        /// <returns>Returns a producer that generates the transformed images.</returns>
        public static IProducer<Shared<Image>> Transform(this IProducer<Shared<Image>> source, TransformDelegate transformer, PixelFormat pixelFormat, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            return source.PipeTo(new ImageTransformer(source.Out.Pipeline, transformer, pixelFormat, sharedImageAllocator), deliveryPolicy);
        }

        /// <summary>
        /// Crops a shared depth image using the specified rectangle.
        /// </summary>
        /// <param name="source">Source of image and rectangle samples.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer generating new cropped image samples.</returns>
        public static IProducer<Shared<Image>> Crop(this IProducer<(Shared<Image>, Rectangle)> source, DeliveryPolicy<(Shared<Image>, Rectangle)> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<(Shared<Image>, Rectangle), Shared<Image>>(
                (tupleOfSharedImageAndRectangle, envelope, emitter) =>
                {
                    using var croppedSharedImage = sharedImageAllocator(tupleOfSharedImageAndRectangle.Item2.Width, tupleOfSharedImageAndRectangle.Item2.Height, tupleOfSharedImageAndRectangle.Item1.Resource.PixelFormat);
                    tupleOfSharedImageAndRectangle.Item1.Resource.Crop(
                        croppedSharedImage.Resource,
                        tupleOfSharedImageAndRectangle.Item2.Left,
                        tupleOfSharedImageAndRectangle.Item2.Top,
                        tupleOfSharedImageAndRectangle.Item2.Width,
                        tupleOfSharedImageAndRectangle.Item2.Height);
                    emitter.Post(croppedSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Crops a shared image using the specified rectangle.
        /// </summary>
        /// <param name="source">Source of image and rectangle samples.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedDepthImageAllocator">Optional image allocator to create new shared depth image.</param>
        /// <returns>Returns a producer generating new cropped image samples.</returns>
        public static IProducer<Shared<DepthImage>> Crop(this IProducer<(Shared<DepthImage>, Rectangle)> source, DeliveryPolicy<(Shared<DepthImage>, Rectangle)> deliveryPolicy = null, Func<int, int, Shared<DepthImage>> sharedDepthImageAllocator = null)
        {
            sharedDepthImageAllocator ??= DepthImagePool.GetOrCreate;
            return source.Process<(Shared<DepthImage>, Rectangle), Shared<DepthImage>>(
                (tupleOfSharedImageAndRectangle, envelope, emitter) =>
                {
                    using var croppedSharedImage = sharedDepthImageAllocator(tupleOfSharedImageAndRectangle.Item2.Width, tupleOfSharedImageAndRectangle.Item2.Height);
                    tupleOfSharedImageAndRectangle.Item1.Resource.Crop(
                        croppedSharedImage.Resource,
                        tupleOfSharedImageAndRectangle.Item2.Left,
                        tupleOfSharedImageAndRectangle.Item2.Top,
                        tupleOfSharedImageAndRectangle.Item2.Width,
                        tupleOfSharedImageAndRectangle.Item2.Height);
                    emitter.Post(croppedSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Convert a producer of depth images into pseudo-colorized images, where more distant pixels are blue, and closer pixels are reddish.
        /// </summary>
        /// <param name="source">Source producer of depth images.</param>
        /// <param name="range">A tuple indicating the range (MinValue, MaxValue) of the depth values in the image.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared images (in <see cref="PixelFormat.BGR_24bpp"/> format).</param>
        /// <returns>A producer of pseudo-colorized images.</returns>
        public static IProducer<Shared<Image>> PseudoColorize(
            this IProducer<Shared<DepthImage>> source,
            (ushort MinValue, ushort MaxValue) range,
            DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null,
            Func<int, int, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= (width, height) => ImagePool.GetOrCreate(width, height, PixelFormat.BGR_24bpp);
            return source.Process<Shared<DepthImage>, Shared<Image>>(
                (sharedDepthImage, envelope, emitter) =>
                {
                    using var colorizedImage = sharedImageAllocator(sharedDepthImage.Resource.Width, sharedDepthImage.Resource.Height);
                    sharedDepthImage.Resource.PseudoColorize(colorizedImage.Resource, range);
                    emitter.Post(colorizedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Converts a shared image to grayscale.
        /// </summary>
        /// <param name="source">Image producer to use as source images.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Producers of grayscale images.</returns>
        public static IProducer<Shared<Image>> ToGray(this IProducer<Shared<Image>> source, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            return source.ToPixelFormat(PixelFormat.Gray_8bpp, deliveryPolicy, sharedImageAllocator);
        }

        /// <summary>
        /// Resizes a shared image.
        /// </summary>
        /// <param name="source">Image to scale.</param>
        /// <param name="finalWidth">Final width of desired output.</param>
        /// <param name="finalHeight">Final height of desired output.</param>
        /// <param name="samplingMode">Method for sampling pixels when rescaling.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates resized images.</returns>
        public static IProducer<Shared<Image>> Resize(this IProducer<Shared<Image>> source, float finalWidth, float finalHeight, SamplingMode samplingMode = SamplingMode.Bilinear, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    using var resizedSharedImage = sharedImageAllocator((int)finalWidth, (int)finalHeight, sharedImage.Resource.PixelFormat);
                    sharedImage.Resource.Resize(resizedSharedImage.Resource, finalWidth, finalHeight, samplingMode);
                    emitter.Post(resizedSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Scales a shared by the specified scale factors.
        /// </summary>
        /// <param name="source">Image to scale.</param>
        /// <param name="scaleX">Scale factor for X.</param>
        /// <param name="scaleY">Scale factor for Y.</param>
        /// <param name="samplingMode">Method for sampling pixels when rescaling.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates resized images.</returns>
        public static IProducer<Shared<Image>> Scale(this IProducer<Shared<Image>> source, float scaleX, float scaleY, SamplingMode samplingMode = SamplingMode.Bilinear, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    int finalWidth = (int)(sharedImage.Resource.Width * scaleX);
                    int finalHeight = (int)(sharedImage.Resource.Height * scaleY);
                    using var scaledSharedImage = sharedImageAllocator(finalWidth, finalHeight, sharedImage.Resource.PixelFormat);
                    sharedImage.Resource.Scale(scaledSharedImage.Resource, scaleX, scaleY, samplingMode);
                    emitter.Post(scaledSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Flips a shared image about the horizontal or vertical axis.
        /// </summary>
        /// <param name="source">Image to flip.</param>
        /// <param name="mode">Axis about which to flip.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>A producer that generates flip images.</returns>
        public static IProducer<Shared<Image>> Flip(this IProducer<Shared<Image>> source, FlipMode mode, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            if (mode == FlipMode.None)
            {
                // just post original image in the case of a no-op
                return source;
            }
            else
            {
                sharedImageAllocator ??= ImagePool.GetOrCreate;
                return source.Process<Shared<Image>, Shared<Image>>(
                    (sharedImage, envelope, emitter) =>
                    {
                        using var flippedSharedImage = sharedImageAllocator(sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.PixelFormat);
                        sharedImage.Resource.Flip(flippedSharedImage.Resource, mode);
                        emitter.Post(flippedSharedImage, envelope.OriginatingTime);
                    }, deliveryPolicy);
            }
        }

        /// <summary>
        /// Rotates a shared image by the specified angle.
        /// </summary>
        /// <param name="source">Image to rotate.</param>
        /// <param name="angleInDegrees">Angle for rotation specified in degrees.</param>
        /// <param name="samplingMode">Sampling mode to use when sampling pixels.</param>
        /// <param name="fit">Used to describe the fit of the output image. Tight=output image is cropped to match exactly the required size. Loose=output image will be maximum size possible (i.e. length of source image diagonal).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates rotated images.</returns>
        public static IProducer<Shared<Image>> Rotate(this IProducer<Shared<Image>> source, float angleInDegrees, SamplingMode samplingMode, RotationFitMode fit = RotationFitMode.Tight, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    DetermineRotatedWidthHeight(
                        sharedImage.Resource.Width,
                        sharedImage.Resource.Height,
                        angleInDegrees,
                        fit,
                        out int rotatedWidth,
                        out int rotateHeight,
                        out float originx,
                        out float originy);
                    using var rotatedSharedImage = sharedImageAllocator(rotatedWidth, rotateHeight, sharedImage.Resource.PixelFormat);
                    rotatedSharedImage.Resource.Clear(Color.Black);
                    sharedImage.Resource.Rotate(rotatedSharedImage.Resource, angleInDegrees, samplingMode, fit);
                    emitter.Post(rotatedSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Draws a rectangle over a shared image.
        /// </summary>
        /// <param name="source">Image to draw rectangle on.</param>
        /// <param name="rect">Pixel coordinates for rectangle.</param>
        /// <param name="color">Color to use when drawing the rectangle.</param>
        /// <param name="width">Line width (in pixels) of each side of the rectangle.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates images overdrawn with a rectangle.</returns>
        public static IProducer<Shared<Image>> DrawRectangle(this IProducer<Shared<Image>> source, Rectangle rect, Color color, int width, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    using var drawRectSharedImage = sharedImageAllocator(sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.PixelFormat);
                    drawRectSharedImage.Resource.CopyFrom(sharedImage.Resource);
                    drawRectSharedImage.Resource.DrawRectangle(rect, color, width);
                    emitter.Post(drawRectSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Draws a line over a shared image.
        /// </summary>
        /// <param name="source">Image to draw line on.</param>
        /// <param name="p0">Pixel coordinates for one end of the line.</param>
        /// <param name="p1">Pixel coordinates for the other end of the line.</param>
        /// <param name="color">Color to use when drawing the line.</param>
        /// <param name="width">Line width (in pixels).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates images overdrawn with a line.</returns>
        public static IProducer<Shared<Image>> DrawLine(this IProducer<Shared<Image>> source, Point p0, Point p1, Color color, int width, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    using var drawLineSharedImage = sharedImageAllocator(sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.PixelFormat);
                    drawLineSharedImage.Resource.CopyFrom(sharedImage.Resource);
                    drawLineSharedImage.Resource.DrawLine(p0, p1, color, width);
                    emitter.Post(drawLineSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Draws a circle over a shared image.
        /// </summary>
        /// <param name="source">Image to draw circle on.</param>
        /// <param name="p0">Center of circle (in pixels).</param>
        /// <param name="radius">Radius of circle (in pixels).</param>
        /// <param name="color">Color to use when drawing the circle.</param>
        /// <param name="width">Line width (in pixels).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates images overdrawn with a circle.</returns>
        public static IProducer<Shared<Image>> DrawCircle(this IProducer<Shared<Image>> source, Point p0, int radius, Color color, int width, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    using var drawCircleSharedImage = sharedImageAllocator(sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.PixelFormat);
                    drawCircleSharedImage.Resource.CopyFrom(sharedImage.Resource);
                    drawCircleSharedImage.Resource.DrawCircle(p0, radius, color, width);
                    emitter.Post(drawCircleSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Draws a piece of text over a shared image.
        /// </summary>
        /// <param name="source">Image to draw text on.</param>
        /// <param name="text">Text to render.</param>
        /// <param name="p0">Coordinates for start of text (in pixels).</param>
        /// <param name="color">Color to use while drawing text.</param>
        /// <param name="font">Name of font to use. Optional.</param>
        /// <param name="fontSize">Size of font. Optional.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Returns a producer that generates images overdrawn with text.</returns>
        public static IProducer<Shared<Image>> DrawText(this IProducer<Shared<Image>> source, string text, Point p0, Color color, string font = null, float fontSize = 24.0f, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sharedImage, envelope, emitter) =>
                {
                    using var drawTextSharedImage = sharedImageAllocator(sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.PixelFormat);
                    drawTextSharedImage.Resource.CopyFrom(sharedImage.Resource);
                    drawTextSharedImage.Resource.DrawText(text, p0, color, font, fontSize);
                    emitter.Post(drawTextSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Inverts each color channel in a shared image.
        /// </summary>
        /// <param name="source">Images to invert.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Producer that returns the inverted image.</returns>
        public static IProducer<Shared<Image>> Invert(this IProducer<Shared<Image>> source, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sourceImage, envelope, emitter) =>
                {
                    using var invertedSharedImage = sharedImageAllocator(sourceImage.Resource.Width, sourceImage.Resource.Height, sourceImage.Resource.PixelFormat);
                    sourceImage.Resource.Invert(invertedSharedImage.Resource);
                    emitter.Post(invertedSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Clears a shared image to the specified color.
        /// </summary>
        /// <param name="source">Images to clear.</param>
        /// <param name="clr">Color to set image to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Producer that returns the cleared image.</returns>
        public static IProducer<Shared<Image>> Clear(this IProducer<Shared<Image>> source, Color clr, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sourceImage, envelope, emitter) =>
                {
                    using var clearedSharedImage = sharedImageAllocator(sourceImage.Resource.Width, sourceImage.Resource.Height, sourceImage.Resource.PixelFormat);
                    clearedSharedImage.Resource.Clear(clr);
                    emitter.Post(clearedSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Extracts a color channel from a shared image. Returned image is of type Gray_8bpp.
        /// </summary>
        /// <param name="source">Images to extract channel from.</param>
        /// <param name="channel">Index of which channel to extract.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Producer that returns the extracted channel as a gray scale image.</returns>
        public static IProducer<Shared<Image>> ExtractChannel(this IProducer<Shared<Image>> source, int channel, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return source.Process<Shared<Image>, Shared<Image>>(
                (sourceImage, envelope, emitter) =>
                {
                    using var channelSharedImage = sharedImageAllocator(sourceImage.Resource.Width, sourceImage.Resource.Height, PixelFormat.Gray_8bpp);
                    sourceImage.Resource.ExtractChannel(channelSharedImage.Resource, channel);
                    emitter.Post(channelSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Computes the absolute difference between two images.
        /// </summary>
        /// <param name="sources">Images to diff.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Producer that returns the difference image.</returns>
        public static IProducer<Shared<Image>> AbsDiff(this IProducer<(Shared<Image>, Shared<Image>)> sources, DeliveryPolicy<(Shared<Image>, Shared<Image>)> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return sources.Process<(Shared<Image>, Shared<Image>), Shared<Image>>(
                (tupleOfSharedImages, envelope, emitter) =>
                {
                    using var absdiffSharedImage = sharedImageAllocator(tupleOfSharedImages.Item1.Resource.Width, tupleOfSharedImages.Item1.Resource.Height, tupleOfSharedImages.Item1.Resource.PixelFormat);
                    tupleOfSharedImages.Item1.Resource.AbsDiff(tupleOfSharedImages.Item2.Resource, absdiffSharedImage.Resource);
                    emitter.Post(absdiffSharedImage, envelope.OriginatingTime);
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
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <returns>Producer that returns the difference image.</returns>
        public static IProducer<Shared<Image>> Threshold(this IProducer<Shared<Image>> image, int threshold, int maxvalue, Threshold thresholdType, DeliveryPolicy<Shared<Image>> deliveryPolicy = null, Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            sharedImageAllocator ??= ImagePool.GetOrCreate;
            return image.Process<Shared<Image>, Shared<Image>>(
                (sharedSourceImage, envelope, emitter) =>
                {
                    using var thresholdSharedImage = sharedImageAllocator(sharedSourceImage.Resource.Width, sharedSourceImage.Resource.Height, sharedSourceImage.Resource.PixelFormat);
                    sharedSourceImage.Resource.Threshold(thresholdSharedImage.Resource, threshold, maxvalue, thresholdType);
                    emitter.Post(thresholdSharedImage, envelope.OriginatingTime);
                }, deliveryPolicy);
        }

        /// <summary>
        /// Encodes a shared image using a specified encoder component.
        /// </summary>
        /// <param name="source">A producer of images to encode.</param>
        /// <param name="encoderConstructor">Constructor function that returns an encoder component given a pipeline.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the encoded images.</returns>
        public static IProducer<Shared<EncodedImage>> Encode(
            this IProducer<Shared<Image>> source,
            Func<Pipeline, IConsumerProducer<Shared<Image>, Shared<EncodedImage>>> encoderConstructor,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return source.PipeTo(encoderConstructor(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Encodes a shared image using a specified image-to-stream encoder.
        /// </summary>
        /// <param name="source">A producer of images to encode.</param>
        /// <param name="encoder">The image-to-stream encoder to use when encoding images.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the encoded images.</returns>
        public static IProducer<Shared<EncodedImage>> Encode(
            this IProducer<Shared<Image>> source,
            IImageToStreamEncoder encoder,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return source.Encode(p => new ImageEncoder(p, encoder), deliveryPolicy);
        }

        /// <summary>
        /// Decodes an encoded image using a specified decoder component.
        /// </summary>
        /// <param name="source">A producer of images to decode.</param>
        /// <param name="decoderConstructor">Constructor function that returns a decoder component given a pipeline.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the decoded images.</returns>
        public static IProducer<Shared<Image>> Decode(
            this IProducer<Shared<EncodedImage>> source,
            Func<Pipeline, IConsumerProducer<Shared<EncodedImage>, Shared<Image>>> decoderConstructor,
            DeliveryPolicy<Shared<EncodedImage>> deliveryPolicy = null)
        {
            return source.PipeTo(decoderConstructor(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Decodes an encoded image using a specified image-from-stream decoder.
        /// </summary>
        /// <param name="source">A producer of images to decode.</param>
        /// <param name="decoder">The image-from-stream decoder to use when decoding images.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the decoded images.</returns>
        public static IProducer<Shared<Image>> Decode(
            this IProducer<Shared<EncodedImage>> source,
            IImageFromStreamDecoder decoder,
            DeliveryPolicy<Shared<EncodedImage>> deliveryPolicy = null)
        {
            return source.Decode(p => new ImageDecoder(p, decoder), deliveryPolicy);
        }

        /// <summary>
        /// Encodes a depth image using a specified encoder component.
        /// </summary>
        /// <param name="source">A producer of depth images to encode.</param>
        /// <param name="encoderConstructor">Constructor function that returns an encoder component given a pipeline.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the encoded depth images.</returns>
        public static IProducer<Shared<EncodedDepthImage>> Encode(
            this IProducer<Shared<DepthImage>> source,
            Func<Pipeline, IConsumerProducer<Shared<DepthImage>, Shared<EncodedDepthImage>>> encoderConstructor,
            DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null)
        {
            return source.PipeTo(encoderConstructor(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Encodes a depth image using a specified depth-image-to-stream encoder.
        /// </summary>
        /// <param name="source">A producer of depth images to encode.</param>
        /// <param name="encoder">The depth image encoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the encoded depth images.</returns>
        public static IProducer<Shared<EncodedDepthImage>> Encode(
            this IProducer<Shared<DepthImage>> source,
            IDepthImageToStreamEncoder encoder,
            DeliveryPolicy<Shared<DepthImage>> deliveryPolicy = null)
        {
            return source.Encode(p => new DepthImageEncoder(p, encoder), deliveryPolicy);
        }

        /// <summary>
        /// Decodes a depth image using a specified decoder component.
        /// </summary>
        /// <param name="source">A producer of depth images to decode.</param>
        /// <param name="decoderConstructor">Constructor function that returns a decoder component given a pipeline.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the decoded depth images.</returns>
        public static IProducer<Shared<DepthImage>> Decode(
            this IProducer<Shared<EncodedDepthImage>> source,
            Func<Pipeline, IConsumerProducer<Shared<EncodedDepthImage>, Shared<DepthImage>>> decoderConstructor,
            DeliveryPolicy<Shared<EncodedDepthImage>> deliveryPolicy = null)
        {
            return source.PipeTo(decoderConstructor(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Decodes a depth image using a specified depth-image-from-stream decoder.
        /// </summary>
        /// <param name="source">A producer of depth images to decode.</param>
        /// <param name="decoder">The depth image decoder to use.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A producer that generates the decoded depth images.</returns>
        public static IProducer<Shared<DepthImage>> Decode(
            this IProducer<Shared<EncodedDepthImage>> source,
            IDepthImageFromStreamDecoder decoder,
            DeliveryPolicy<Shared<EncodedDepthImage>> deliveryPolicy = null)
        {
            return source.Decode(p => new DepthImageDecoder(p, decoder), deliveryPolicy);
        }
    }
}