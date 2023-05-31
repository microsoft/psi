// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    /// Mode for determining final output size when rotating an image.
    /// </summary>
    public enum RotationFitMode
    {
        /// <summary>
        /// Output image will always be the maximum width/height regardless of the specified
        /// rotation amount. For instance, if rotating a 100x200 image by 10 degrees the output
        /// image size will be 224x224. That is the diagonal length and the amount of the rotation
        /// is irrelevant.
        /// </summary>
        Loose,

        /// <summary>
        /// Output image will be fit exactly. Thus if we rotate a 100x200 image by 10 degrees
        /// the output image size will be 35x180.
        /// </summary>
        Tight,
    }

    /// <summary>
    /// Sampling mode used by various imaging operators.
    /// </summary>
    public enum SamplingMode
    {
        /// <summary>
        /// Sampling mode using nearest neighbor interpolation.
        /// </summary>
        Point,

        /// <summary>
        /// Sampling mode using bilinear interpolation.
        /// </summary>
        Bilinear,

        /// <summary>
        /// Sampling mode using bicubic interpolation.
        /// </summary>
        Bicubic,
    }

    /// <summary>
    /// Thresholding modes.
    /// </summary>
    public enum Threshold
    {
        /// <summary>
        /// Thresholds pixels such that:
        ///    dst(x,y) = maxvalue   if (src(x,y)>threshold)
        ///             = 0          otherwise
        /// </summary>
        Binary,

        /// <summary>
        /// Thresholds pixels such that:
        ///    dst(x,y) = 0          if (src(x,y)>threshold)
        ///             = maxvalue   otherwise
        /// </summary>
        BinaryInv,

        /// <summary>
        /// Thresholds pixels such that:
        ///    dst(x,y) = threshold   if (src(x,y)>threshold)
        ///             = src(x,y)    otherwise
        /// </summary>
        Truncate,

        /// <summary>
        /// Thresholds pixels such that:
        ///    dst(x,y) = src(x,y)   if (src(x,y)>threshold)
        ///             = 0          otherwise
        /// </summary>
        ToZero,

        /// <summary>
        /// Thresholds pixels such that:
        ///    dst(x,y) = 0          if (src(x,y)>threshold)
        ///             = src(x,y)   otherwise
        /// </summary>
        ToZeroInv,
    }

    /// <summary>
    /// Axis along which to flip an image.
    /// </summary>
    public enum FlipMode
    {
        /// <summary>
        /// Leave image unflipped
        /// </summary>
        None,

        /// <summary>
        /// Flips image along the horizontal axis
        /// </summary>
        AlongHorizontalAxis,

        /// <summary>
        /// Flips image along the vertical axis
        /// </summary>
        AlongVerticalAxis,
    }

    /// <summary>
    /// Various imaging operators.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Constant passed to ExtractChannel to indcate the red channel should be extracted.
        /// </summary>
        public const int ExtractRedChannel = 0;

        /// <summary>
        /// Constant passed to ExtractChannel to indcate the green channel should be extracted.
        /// </summary>
        public const int ExtractGreenChannel = 1;

        /// <summary>
        /// Constant passed to ExtractChannel to indicate the blue channel should be extracted.
        /// </summary>
        public const int ExtractBlueChannel = 2;

        /// <summary>
        /// Constant passed to ExtractChannel to indicate the alpha channel should be extracted.
        /// </summary>
        public const int ExtractAlphaChannel = 3;

        /// <summary>
        /// Set palette of the 8 bpp indexed image to grayscale.
        /// </summary>
        /// <param name="bitmap">Image to initialize.</param>
        /// <remarks>The method initializes palette of
        /// <see cref="System.Drawing.Imaging.PixelFormat">Format8bppIndexed</see>
        /// image with 256 gradients of gray color.</remarks>
        public static void SetGrayscalePalette(Bitmap bitmap)
        {
            // check pixel format
            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                throw new ArgumentException("Source image is not 8 bpp image.");
            }

            // get palette
            ColorPalette cp = bitmap.Palette;

            // init palette
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }

            // set palette back
            bitmap.Palette = cp;
        }

        /// <summary>
        /// Function to convert RGB color into grayscale.
        /// </summary>
        /// <param name="r">red component (Range=0..255).</param>
        /// <param name="g">green component (Range=0..255).</param>
        /// <param name="b">Blue component (Range=0..255).</param>
        /// <returns>Grayscale value (Range=0..255).</returns>
        public static byte Rgb2Gray(byte r, byte g, byte b)
        {
            return (byte)(((4897 * r) + (9617 * g) + (1868 * b)) >> 14);
        }

        /// <summary>
        /// Function to convert RGB color into grayscale.
        /// </summary>
        /// <param name="r">red component (Range=0..65535).</param>
        /// <param name="g">green component (Range=0..65535).</param>
        /// <param name="b">Blue component (Range=0..65535).</param>
        /// <returns>Grayscale value (Range=0..65535).</returns>
        public static ushort Rgb2Gray(ushort r, ushort g, ushort b)
        {
            return (ushort)(((4897 * r) + (9617 * g) + (1868 * b)) >> 14);
        }

        /// <summary>
        /// Converts an image to the specified pixel format.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <param name="format">The pixel format of the converted image.</param>
        /// <returns>A copy of the image in the specified format.</returns>
        public static Image Convert(this Image image, PixelFormat format)
        {
            var destImage = new Image(image.Width, image.Height, format);
            image.CopyTo(destImage);
            return destImage;
        }

        /// <summary>
        /// Flips an image along a specified axis.
        /// </summary>
        /// <param name="image">Image to flip.</param>
        /// <param name="mode">Axis along which to flip.</param>
        /// <returns>A new flipped image.</returns>
        public static Image Flip(this Image image, FlipMode mode)
        {
            var destImage = new Image(image.Width, image.Height, image.PixelFormat);
            image.Flip(destImage, mode);
            return destImage;
        }

        /// <summary>
        /// Flips an image along a specified axis.
        /// </summary>
        /// <param name="image">Image to flip.</param>
        /// <param name="destImage">Destination image where to store results.</param>
        /// <param name="mode">Axis along which to flip.</param>
        public static void Flip(this Image image, Image destImage, FlipMode mode)
        {
            if (destImage.PixelFormat != image.PixelFormat)
            {
                throw new ArgumentException("destImage.PixelFormat", "Destination image pixel format doesn't match source image pixel format");
            }

            if (image.Width != destImage.Width || image.Height != destImage.Height)
            {
                throw new ArgumentException("Destination image's width/height must match the source image width/height");
            }

            if (image.PixelFormat == PixelFormat.Gray_16bpp ||
                image.PixelFormat == PixelFormat.Gray_8bpp ||
                image.PixelFormat == PixelFormat.RGB_24bpp ||
                image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                // We can't handle grayscale images through GDI. Also, while GDI can read 64bpp images, it will convert
                // to 8-bits-per-channel for processing.
                //
                // See https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=net-5.0#remarks
                // and https://docs.microsoft.com/en-us/dotnet/api/system.drawing.imaging.pixelformat?view=net-5.0#remarks
                //
                // Additionally, there is no corresponding System pixel format for RGB 24bpp. We will therefore handle
                // all these cases using raw byte manipulation.
                unsafe
                {
                    int sourceBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                    int destinationBytesPerPixel = destImage.PixelFormat.GetBytesPerPixel();
                    byte* sourceRow = (byte*)image.ImageData.ToPointer();
                    byte* destinationRow = (byte*)destImage.ImageData.ToPointer();
                    int ystep = destImage.Stride;
                    if (mode == FlipMode.AlongHorizontalAxis)
                    {
                        destinationRow += destImage.Stride * (image.Height - 1);
                        ystep = -destImage.Stride;
                    }

                    int xstep = destinationBytesPerPixel;
                    int xoffset = 0;
                    if (mode == FlipMode.AlongVerticalAxis)
                    {
                        xoffset = destinationBytesPerPixel * (destImage.Width - 1);
                        xstep = -destinationBytesPerPixel;
                    }

                    for (int i = 0; i < image.Height; i++)
                    {
                        byte* sourceColumn = sourceRow;
                        byte* destinationColumn = destinationRow + xoffset;
                        for (int j = 0; j < image.Width; j++)
                        {
                            if (image.PixelFormat == PixelFormat.Gray_8bpp)
                            {
                                destinationColumn[0] = sourceColumn[0];
                            }
                            else if (image.PixelFormat == PixelFormat.Gray_16bpp)
                            {
                                ((ushort*)destinationColumn)[0] = ((ushort*)sourceColumn)[0];
                            }
                            else
                            {
                                for (int k = 0; k < destinationBytesPerPixel; k++)
                                {
                                    destinationColumn[k] = sourceColumn[k];
                                }
                            }

                            sourceColumn += sourceBytesPerPixel;
                            destinationColumn += xstep;
                        }

                        sourceRow += image.Stride;
                        destinationRow += ystep;
                    }
                }
            }
            else
            {
                // This block handles the rest of the pixel format cases
                using var bitmap = new Bitmap(image.Width, image.Height, PixelFormatHelper.ToSystemPixelFormat(image.PixelFormat));
                using var graphics = Graphics.FromImage(bitmap);
                switch (mode)
                {
                    case FlipMode.AlongHorizontalAxis:
                        graphics.TranslateTransform(0.0f, image.Height);
                        graphics.ScaleTransform(1.0f, -1.0f);
                        break;

                    case FlipMode.AlongVerticalAxis:
                        graphics.TranslateTransform(image.Width, 0.0f);
                        graphics.ScaleTransform(-1.0f, 1.0f);
                        break;

                    case FlipMode.None:
                        break;
                }

                using (var destinationImage = image.ToBitmap())
                {
                    graphics.DrawImage(destinationImage, new Point(0, 0));
                }

                destImage.CopyFrom(bitmap);
            }
        }

        /// <summary>
        /// Resizes an image by the specified scale factors using the specified sampling mode.
        /// </summary>
        /// <param name="image">Image to resize.</param>
        /// <param name="scaleX">Scale factor to apply in X direction.</param>
        /// <param name="scaleY">Scale factor to apply in Y direction.</param>
        /// <param name="mode">Sampling mode for sampling of pixels.</param>
        /// <returns>Returns a new image scaled by the specified scale factors.</returns>
        public static Image Scale(this Image image, float scaleX, float scaleY, SamplingMode mode)
        {
            int scaledWidth = (int)Math.Abs(image.Width * scaleX);
            int scaledHeight = (int)Math.Abs(image.Height * scaleY);
            var destImage = new Image(scaledWidth, scaledHeight, image.PixelFormat);
            image.Resize(destImage, scaledWidth, scaledHeight, mode);
            return destImage;
        }

        /// <summary>
        /// Resizes an image by the specified scale factors using the specified sampling mode.
        /// </summary>
        /// <param name="image">Image to resize.</param>
        /// <param name="destImage">Image to store scaled results.</param>
        /// <param name="scaleX">Scale factor to apply in X direction.</param>
        /// <param name="scaleY">Scale factor to apply in Y direction.</param>
        /// <param name="mode">Sampling mode for sampling of pixels.</param>
        public static void Scale(this Image image, Image destImage, float scaleX, float scaleY, SamplingMode mode)
        {
            int scaledWidth = (int)Math.Abs(image.Width * scaleX);
            int scaledHeight = (int)Math.Abs(image.Height * scaleY);
            image.Resize(destImage, scaledWidth, scaledHeight, mode);
        }

        /// <summary>
        /// Compares two images to see if they are identical (within some specified tolerance).
        /// </summary>
        /// <param name="image1">First image in comparison.</param>
        /// <param name="image2">Second image in comparison.</param>
        /// <param name="tolerance">Maximum allowable distance between pixels in RGB or Grayscale space.</param>
        /// <param name="percentOutliersAllowed">Percetange of pixels allowed to be outside tolerance.</param>
        /// <param name="errorMetrics">Error metrics across all pixels.</param>
        /// <returns>True if images are considered identical. False otherwise.</returns>
        public static bool Compare(this ImageBase image1, ImageBase image2, double tolerance, double percentOutliersAllowed, ref ImageError errorMetrics)
        {
            if (image1.GetType() != image2.GetType() ||
                image1.PixelFormat != image2.PixelFormat ||
                image1.Width != image2.Width ||
                image1.Height != image2.Height)
            {
                return false;
            }

            errorMetrics.MaxError = 0.0f;
            errorMetrics.AvgError = 0.0f;
            errorMetrics.NumberOutliers = 0;
            double dist = 0.0f;
            unsafe
            {
                int bytesPerPixel1 = image1.BitsPerPixel / 8;
                int bytesPerPixel2 = image2.BitsPerPixel / 8;

                byte* row1 = (byte*)image1.ImageData.ToPointer();
                byte* row2 = (byte*)image2.ImageData.ToPointer();
                for (int y = 0; y < image1.Height; y++)
                {
                    byte* col1 = row1;
                    byte* col2 = row2;
                    for (int x = 0; x < image1.Width; x++)
                    {
                        switch (image1.PixelFormat)
                        {
                            case PixelFormat.BGRA_32bpp:
                                {
                                    int db = col1[0] - col2[0];
                                    int dg = col1[1] - col2[1];
                                    int dr = col1[2] - col2[2];
                                    int da = col1[3] - col2[3];
                                    dist = (double)(db * db + dg * dg + dr * dr + da * da);
                                }

                                break;

                            case PixelFormat.BGRX_32bpp:
                            case PixelFormat.BGR_24bpp:
                                {
                                    int db = col1[0] - col2[0];
                                    int dg = col1[1] - col2[1];
                                    int dr = col1[2] - col2[2];
                                    dist = (double)(db * db + dg * dg + dr * dr);
                                }

                                break;

                            case PixelFormat.Gray_16bpp:
                                {
                                    int d = ((ushort*)col1)[0] - ((ushort*)col2)[0];
                                    dist = (double)(d * d);
                                }

                                break;

                            case PixelFormat.Gray_8bpp:
                                {
                                    int d = col1[0] - col2[0];
                                    dist = (double)(d * d);
                                }

                                break;

                            case PixelFormat.RGB_24bpp:
                                {
                                    int dr = col1[0] - col2[0];
                                    int dg = col1[1] - col2[1];
                                    int db = col1[2] - col2[2];
                                    dist = (double)(dr * dr + dg * dg + db * db);
                                }

                                break;

                            case PixelFormat.RGBA_64bpp:
                                {
                                    int dr = ((ushort*)col1)[0] - ((ushort*)col2)[0];
                                    int dg = ((ushort*)col1)[1] - ((ushort*)col2)[1];
                                    int db = ((ushort*)col1)[2] - ((ushort*)col2)[2];
                                    int da = ((ushort*)col1)[3] - ((ushort*)col2)[3];
                                    dist = (double)(dr * dr + dg * dg + db * db + da * da);
                                }

                                break;

                            case PixelFormat.Undefined:
                            default:
                                throw new ArgumentException("Unsupported image format");
                        }

                        if (dist > errorMetrics.MaxError)
                        {
                            errorMetrics.MaxError = dist;
                        }

                        errorMetrics.AvgError += dist;
                        if (dist > tolerance * tolerance)
                        {
                            errorMetrics.NumberOutliers++;
                        }

                        col1 += bytesPerPixel1;
                        col2 += bytesPerPixel2;
                    }

                    row1 += image1.Stride;
                    row2 += image2.Stride;
                }
            }

            errorMetrics.AvgError /= (double)image1.Width * image1.Height;
            errorMetrics.MaxError = Math.Sqrt(errorMetrics.MaxError);

            return errorMetrics.NumberOutliers <= percentOutliersAllowed * image1.Width * image1.Height;
        }

        /// <summary>
        /// Compares two depth images to see if they are identical (within some specified tolerance).
        /// </summary>
        /// <param name="depthImage1">First image in comparison.</param>
        /// <param name="depthImage2">Second image in comparison.</param>
        /// <param name="tolerance">Maximum allowable distance between pixels in Grayscale space.</param>
        /// <param name="percentOutliersAllowed">Percetange of pixels allowed to be outside tolerance.</param>
        /// <param name="errorMetrics">Error metrics across all pixels.</param>
        /// <returns>True if images are considered identical. False otherwise.</returns>
        public static bool Compare(this DepthImage depthImage1, DepthImage depthImage2, double tolerance, double percentOutliersAllowed, ref ImageError errorMetrics)
        {
            if (depthImage1.DepthValueSemantics != depthImage2.DepthValueSemantics ||
                depthImage1.DepthValueToMetersScaleFactor != depthImage2.DepthValueToMetersScaleFactor)
            {
                return false;
            }
            else
            {
                return Compare(depthImage1 as ImageBase, depthImage2 as ImageBase, tolerance, percentOutliersAllowed, ref errorMetrics);
            }
        }

        /// <summary>
        /// Resizes an image by the specified scale factors using the specified sampling mode.
        /// </summary>
        /// <param name="image">Image to resize.</param>
        /// <param name="destImage">Image to store scaled results.</param>
        /// <param name="newWidth">Desired output width in pixels.</param>
        /// <param name="newHeight">Desired output height in pixels.</param>
        /// <param name="mode">Sampling mode for sampling of pixels.</param>
        public static void Resize(this Image image, Image destImage, float newWidth, float newHeight, SamplingMode mode)
        {
            if (destImage.PixelFormat != image.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (newWidth <= 0.0f || newHeight <= 0.0f)
            {
                throw new System.ArgumentOutOfRangeException("Pixel width/height must be greater than 0");
            }

            if (destImage.Width != newWidth || destImage.Height != newHeight)
            {
                throw new ArgumentException($"Destination image must be size={newWidth}x{newHeight}.");
            }

            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Scaling 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the resize,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                using var resizedImage = new Image((int)newWidth, (int)newHeight, PixelFormat.BGR_24bpp);
                tmpImage.Resize(resizedImage, newWidth, newHeight, mode);
                destImage.CopyFrom(resizedImage);
                return;
            }

            using var bitmap = new Bitmap((int)newWidth, (int)newHeight, PixelFormatHelper.ToSystemPixelFormat(image.PixelFormat));
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            switch (mode)
            {
                case SamplingMode.Point:
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                    break;

                case SamplingMode.Bilinear:
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    break;

                case SamplingMode.Bicubic:
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    break;
            }

            float scaleX = newWidth / image.Width;
            float scaleY = newHeight / image.Height;
            graphics.ScaleTransform(scaleX, scaleY);

            using (var managedimg = image.ToBitmap())
            {
                graphics.DrawImage(managedimg, new Point(0, 0));
            }

            destImage.CopyFrom(bitmap);
        }

        /// <summary>
        /// Resize an image.
        /// </summary>
        /// <param name="image">Image to resize.</param>
        /// <param name="finalWidth">Final width of desired output.</param>
        /// <param name="finalHeight">Final height of desired output.</param>
        /// <param name="samplingMode">Method for sampling pixels when rescaling.</param>
        /// <returns>Returns a new image resized to the specified width/height.</returns>
        public static Image Resize(this Image image, int finalWidth, int finalHeight, SamplingMode samplingMode = SamplingMode.Bilinear)
        {
            var destImage = new Image(finalWidth, finalHeight, image.PixelFormat);
            image.Resize(destImage, finalWidth, finalHeight, samplingMode);
            return destImage;
        }

        /// <summary>
        /// Rotates an image.
        /// </summary>
        /// <param name="image">Image to rotate.</param>
        /// <param name="angleInDegrees">Number of degrees to rotate in counter clockwise direction.</param>
        /// <param name="mode">Pixel resampling method.</param>
        /// <param name="fit">Used to describe the fit of the output image. Tight=output image is cropped to match exactly the required size. Loose=output image will be maximum size possible (i.e. length of source image diagonal).</param>
        /// <returns>Rotated image.</returns>
        public static Image Rotate(this Image image, float angleInDegrees, SamplingMode mode, RotationFitMode fit = RotationFitMode.Tight)
        {
            DetermineRotatedWidthHeight(image.Width, image.Height, angleInDegrees, fit, out int rotatedWidth, out int rotatedHeight, out _, out _);
            var rotatedImage = new Image(rotatedWidth, rotatedHeight, image.PixelFormat);
            image.Rotate(rotatedImage, angleInDegrees, mode, fit);
            return rotatedImage;
        }

        /// <summary>
        /// Rotates an image.
        /// </summary>
        /// <param name="image">Image to rotate.</param>
        /// <param name="destImage">Image where to store rotated source image.</param>
        /// <param name="angleInDegrees">Number of degrees to rotate in counter clockwise direction.</param>
        /// <param name="mode">Pixel resampling method.</param>
        /// <param name="fit">Used to describe the fit of the output image. Tight=output image is cropped to match exactly the required size. Loose=output image will be maximum size possible (i.e. length of source image diagonal).</param>
        public static void Rotate(this Image image, Image destImage, float angleInDegrees, SamplingMode mode, RotationFitMode fit = RotationFitMode.Tight)
        {
            if (destImage.PixelFormat != image.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Rotating 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            DetermineRotatedWidthHeight(image.Width, image.Height, angleInDegrees, fit, out int rotatedWidth, out int rotatedHeight, out float originx, out float originy);

            if (rotatedWidth != destImage.Width || rotatedHeight != destImage.Height)
            {
                throw new ArgumentException($"Destination image must be size={rotatedWidth}x{rotatedHeight}.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the rotation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                using var rotatedImage = new Image(rotatedWidth, rotatedHeight, PixelFormat.BGR_24bpp);
                tmpImage.Rotate(rotatedImage, angleInDegrees, mode, fit);
                destImage.CopyFrom(rotatedImage);
                return;
            }

            using var bitmap = new Bitmap(rotatedWidth, rotatedHeight, PixelFormatHelper.ToSystemPixelFormat(image.PixelFormat));
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            switch (mode)
            {
                case SamplingMode.Point:
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                    break;

                case SamplingMode.Bilinear:
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    break;

                case SamplingMode.Bicubic:
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    break;
            }

            graphics.TranslateTransform(-originx, -originy);
            graphics.RotateTransform(angleInDegrees);

            using (var managedimg = image.ToBitmap())
            {
                graphics.DrawImage(managedimg, new Point(0, 0));
            }

            destImage.CopyFrom(bitmap);
        }

        /// <summary>
        /// Creates a copy of the image cropped to the specified dimensions.
        /// </summary>
        /// <param name="image">Image to crop.</param>
        /// <param name="left">The left of the region to crop.</param>
        /// <param name="top">The top of the region to crop.</param>
        /// <param name="width">The width of the region to crop.</param>
        /// <param name="height">The height of the region to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the region (by default false).</param>
        /// <returns>The cropped image.</returns>
        public static Image Crop(this Image image, int left, int top, int width, int height, bool clip = false) =>
            image.Crop(new Rectangle(left, top, width, height), clip);

        /// <summary>
        /// Creates a copy of the image cropped to the specified rectangle.
        /// </summary>
        /// <param name="image">Image to crop.</param>
        /// <param name="rectangle">The rectangle to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the rectangle (by default false).</param>
        /// <returns>The cropped image.</returns>
        public static Image Crop(this Image image, Rectangle rectangle, bool clip = false)
        {
            var actualRectangle = clip ? GetImageSizeClippedRectangle(rectangle, image.Width, image.Height) : rectangle;
            if (actualRectangle.IsEmpty)
            {
                return null;
            }
            else
            {
                var croppedImage = new Image(actualRectangle.Width, actualRectangle.Height, image.PixelFormat);
                image.Crop(croppedImage, actualRectangle, clip: false);
                return croppedImage;
            }
        }

        /// <summary>
        /// Creates a copy of the image cropped to the specified rectangle.
        /// </summary>
        /// <param name="image">Image to crop.</param>
        /// <param name="croppedImage">Destination image that cropped area is copied to.</param>
        /// <param name="left">The left of the rectangle to crop.</param>
        /// <param name="top">The top of the rectangle to crop.</param>
        /// <param name="width">The width of the rectangle to crop.</param>
        /// <param name="height">The height of the rectangle to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the region (by default false).</param>
        public static void Crop(this Image image, Image croppedImage, int left, int top, int width, int height, bool clip = false) =>
            image.Crop(croppedImage, new Rectangle(left, top, width, height), clip);

        /// <summary>
        /// Creates a copy of the image cropped to the specified dimensions.
        /// </summary>
        /// <param name="image">Image to crop.</param>
        /// <param name="croppedImage">Destination image that cropped area is copied to.</param>
        /// <param name="rectangle">The rectangle to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the region (by default false).</param>
        public static void Crop(this Image image, Image croppedImage, Rectangle rectangle, bool clip = false)
        {
            if (croppedImage.PixelFormat != image.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("croppedImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            var actualRectangle = clip ? GetImageSizeClippedRectangle(rectangle, image.Width, image.Height) : rectangle;

            if (actualRectangle.IsEmpty)
            {
                return;
            }

            if (croppedImage.Width < actualRectangle.Width)
            {
                throw new ArgumentOutOfRangeException("croppedImage.Width", "destination image width is too small");
            }

            if (croppedImage.Height < actualRectangle.Height)
            {
                throw new ArgumentOutOfRangeException("croppedImage.Height", "destination image height is too small");
            }

            if ((actualRectangle.Left < 0) || (actualRectangle.Left >= image.Width))
            {
                throw new ArgumentOutOfRangeException("left", "left is out of range");
            }

            if ((actualRectangle.Top < 0) || (actualRectangle.Top >= image.Height))
            {
                throw new ArgumentOutOfRangeException("top", "top is out of range");
            }

            if ((actualRectangle.Width < 0) || ((actualRectangle.Left + actualRectangle.Width) > image.Width))
            {
                throw new ArgumentOutOfRangeException("width", "width is out of range");
            }

            if ((actualRectangle.Height < 0) || ((actualRectangle.Top + actualRectangle.Height) > image.Height))
            {
                throw new ArgumentOutOfRangeException("height", "height is out of range");
            }

            // Cropped image will be returned as a new image - original (this) image is not modified
            System.Diagnostics.Debug.Assert(croppedImage.ImageData != IntPtr.Zero, "Unexpected empty image");
            unsafe
            {
                int bytesPerPixel = image.BitsPerPixel / 8;

                // Compute the number of bytes in each line of the crop region
                int copyLength = actualRectangle.Width * bytesPerPixel;

                // Start at top-left of region to crop
                byte* src = (byte*)image.ImageData.ToPointer() + (actualRectangle.Top * image.Stride) + (actualRectangle.Left * bytesPerPixel);
                byte* dst = (byte*)croppedImage.ImageData.ToPointer();

                // Copy line by line
                for (int i = 0; i < actualRectangle.Height; i++)
                {
                    Buffer.MemoryCopy(src, dst, copyLength, copyLength);

                    src += image.Stride;
                    dst += croppedImage.Stride;
                }
            }
        }

        /// <summary>
        /// Creates a copy of the depth image cropped to the specified dimensions.
        /// </summary>
        /// <param name="depthImage">Depth image to crop.</param>
        /// <param name="left">The left of the region to crop.</param>
        /// <param name="top">The top of the region to crop.</param>
        /// <param name="width">The width of the region to crop.</param>
        /// <param name="height">The height of the region to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the region (by default false).</param>
        /// <returns>The cropped depth image.</returns>
        public static DepthImage Crop(this DepthImage depthImage, int left, int top, int width, int height, bool clip = false) =>
            depthImage.Crop(new Rectangle(left, top, width, height), clip);

        /// <summary>
        /// Creates a copy of the depth image cropped to the specified rectangle.
        /// </summary>
        /// <param name="depthImage">Depth image to crop.</param>
        /// <param name="rectangle">The rectangle region to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the rectangle to the image boundaries (by default false).</param>
        /// <returns>The cropped depth image.</returns>
        public static DepthImage Crop(this DepthImage depthImage, Rectangle rectangle, bool clip = false)
        {
            var actualRectangle = clip ? GetImageSizeClippedRectangle(rectangle, depthImage.Width, depthImage.Height) : rectangle;
            var croppedDepthImage = new DepthImage(actualRectangle.Width, actualRectangle.Height, depthImage.Stride, depthImage.DepthValueSemantics, depthImage.DepthValueToMetersScaleFactor);
            depthImage.Crop(croppedDepthImage, actualRectangle, clip: false);
            return croppedDepthImage;
        }

        /// <summary>
        /// Creates a copy of the depth image cropped to the specified dimensions.
        /// </summary>
        /// <param name="depthImage">Image to crop.</param>
        /// <param name="croppedDepthImage">Destination image that cropped area is copied to.</param>
        /// <param name="left">The left of the region to crop.</param>
        /// <param name="top">The top of the region to crop.</param>
        /// <param name="width">The width of the region to crop.</param>
        /// <param name="height">The height of the region to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the region (by default false).</param>
        public static void Crop(this DepthImage depthImage, DepthImage croppedDepthImage, int left, int top, int width, int height, bool clip = false) =>
            depthImage.Crop(croppedDepthImage, new Rectangle(left, top, width, height), clip);

        /// <summary>
        /// Creates a copy of the depth image cropped to the specified rectangle.
        /// </summary>
        /// <param name="depthImage">Image to crop.</param>
        /// <param name="croppedDepthImage">Destination image that cropped area is copied to.</param>
        /// <param name="rectangle">The rectangle region to crop.</param>
        /// <param name="clip">An optional parameter indicating whether to clip the rectangle to the image boundaries (by default false).</param>
        public static void Crop(this DepthImage depthImage, DepthImage croppedDepthImage, Rectangle rectangle, bool clip = false)
        {
            if (croppedDepthImage.PixelFormat != depthImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException($"{nameof(croppedDepthImage)}.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (croppedDepthImage.DepthValueSemantics != depthImage.DepthValueSemantics)
            {
                throw new ArgumentOutOfRangeException($"{nameof(croppedDepthImage)}.DepthValueSemantics", "destination image depth value semantics doesn't match source depth value semantics.");
            }

            if (croppedDepthImage.DepthValueToMetersScaleFactor != depthImage.DepthValueToMetersScaleFactor)
            {
                throw new ArgumentOutOfRangeException($"{nameof(croppedDepthImage)}.DepthValueToMetersScaleFactor", "destination image depth value scale factor doesn't match source depth value scale factor.");
            }

            var actualRectangle = clip ? GetImageSizeClippedRectangle(rectangle, depthImage.Width, depthImage.Height) : rectangle;

            if (actualRectangle.IsEmpty)
            {
                return;
            }

            if (croppedDepthImage.Width < actualRectangle.Width)
            {
                throw new ArgumentOutOfRangeException($"{nameof(croppedDepthImage)}.Width", "destination image width is too small");
            }

            if (croppedDepthImage.Height < actualRectangle.Height)
            {
                throw new ArgumentOutOfRangeException($"{nameof(croppedDepthImage)}.Height", "destination image height is too small");
            }

            if ((actualRectangle.Left < 0) || (actualRectangle.Left >= depthImage.Width))
            {
                throw new ArgumentOutOfRangeException("left", "left is out of range");
            }

            if ((actualRectangle.Top < 0) || (actualRectangle.Top >= depthImage.Height))
            {
                throw new ArgumentOutOfRangeException("top", "top is out of range");
            }

            if ((actualRectangle.Width < 0) || ((actualRectangle.Left + actualRectangle.Width) > depthImage.Width))
            {
                throw new ArgumentOutOfRangeException("width", "width is out of range");
            }

            if ((actualRectangle.Height < 0) || ((actualRectangle.Top + actualRectangle.Height) > depthImage.Height))
            {
                throw new ArgumentOutOfRangeException("height", "height is out of range");
            }

            // Cropped image will be returned as a new image - original (this) image is not modified
            System.Diagnostics.Debug.Assert(croppedDepthImage.ImageData != IntPtr.Zero, "Unexpected empty image");
            unsafe
            {
                int bytesPerPixel = depthImage.BitsPerPixel / 8;

                // Compute the number of bytes in each line of the crop region
                int copyLength = actualRectangle.Width * bytesPerPixel;

                // Start at top-left of region to crop
                byte* src = (byte*)depthImage.ImageData.ToPointer() + (actualRectangle.Top * depthImage.Stride) + (actualRectangle.Left * bytesPerPixel);
                byte* dst = (byte*)croppedDepthImage.ImageData.ToPointer();

                // Copy line by line
                for (int i = 0; i < actualRectangle.Height; i++)
                {
                    Buffer.MemoryCopy(src, dst, copyLength, copyLength);

                    src += depthImage.Stride;
                    dst += croppedDepthImage.Stride;
                }
            }
        }

        /// <summary>
        /// Convert a depth image into a pseudo-colorized image, where more distant pixels are closer to blue, and near pixels are closer to red.
        /// </summary>
        /// <param name="depthImage">Depth image to pseudo-colorize.</param>
        /// <param name="range">A tuple indicating the range (MinValue, MaxValue) of the depth values in the image.</param>
        /// <param name="invalidValue">Indicates invalid depth values. These values are left black, or set to transparent based on the invalidAsTransparent parameter.</param>
        /// <param name="invalidAsTransparent">Indicates whether to render invalid values as transparent in the image.</param>
        /// <returns>The pseudo-colorized image in BGR_24bpp format.</returns>
        public static Image PseudoColorize(
            this DepthImage depthImage,
            (ushort MinValue, ushort MaxValue) range,
            ushort? invalidValue = null,
            bool invalidAsTransparent = false)
        {
            var colorizedImage = new Image(depthImage.Width, depthImage.Height, PixelFormat.BGR_24bpp);
            depthImage.PseudoColorize(colorizedImage, range, invalidValue, invalidAsTransparent);
            return colorizedImage;
        }

        /// <summary>
        /// Convert a depth image into a pseudo-colorized image, where more distant pixels are closer to blue, and near pixels are closer to red.
        /// </summary>
        /// <param name="depthImage">Depth image to pseudo-colorize.</param>
        /// <param name="colorizedImage">Target color image. Must be in BGRA_32bpp format.</param>
        /// <param name="range">A tuple indicating the range (MinValue, MaxValue) of the depth values in the image.</param>
        /// <param name="invalidValue">Indicates invalid depth values. These values are left black, or set to transparent based on the invalidAsTransparent parameter.</param>
        /// <param name="invalidAsTransparent">Indicates whether to render invalid values as transparent in the image.</param>
        public static void PseudoColorize(
            this DepthImage depthImage,
            Image colorizedImage,
            (ushort MinValue, ushort MaxValue) range,
            ushort? invalidValue = null,
            bool invalidAsTransparent = false)
        {
            if (depthImage.Width != colorizedImage.Width || depthImage.Height != colorizedImage.Height)
            {
                throw new ArgumentException("Destination color image must have same width and height as source depth image.");
            }

            if (colorizedImage.PixelFormat != PixelFormat.BGRA_32bpp)
            {
                throw new InvalidOperationException("Only BGR 32bpp pixel format is supported for the destination color image.");
            }

            unsafe
            {
                // Portions adapted from:
                // https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/tools/k4aviewer/k4adepthpixelcolorizer.h
                Parallel.For(0, depthImage.Height, iy =>
                {
                    ushort* src = (ushort*)((byte*)depthImage.ImageData.ToPointer() + (iy * depthImage.Stride));
                    byte* dst = (byte*)colorizedImage.ImageData.ToPointer() + (iy * colorizedImage.Stride);

                    for (int ix = 0; ix < depthImage.Width; ix++)
                    {
                        ushort depth = *src;

                        if (invalidValue.HasValue && depth == invalidValue.Value)
                        {
                            dst[0] = 0;
                            dst[1] = 0;
                            dst[2] = 0;
                            dst[3] = (byte)(invalidAsTransparent ? 0 : 255);

                            dst += 4;
                            src += 1;
                            continue;
                        }

                        // clamp the pixel
                        ushort clampedDepth = depth > range.MaxValue ? range.MaxValue : depth;
                        clampedDepth = depth < range.MinValue ? range.MinValue : depth;

                        // get the scaled depth (0-1 range)
                        float scaledDepth = range.MaxValue == range.MinValue ? 0 : (clampedDepth - range.MinValue) / (float)(range.MaxValue - range.MinValue);
                        scaledDepth = 1 - scaledDepth;

                        // We want to go from blue (at 2/3 in hue space) to red (at 0 in hue space), so
                        // remap accordingly
                        // See also: https://en.wikipedia.org/wiki/HSL_and_HSV#/media/File:HSV-RGB-comparison.svg
                        float red = .0f;
                        float green = .0f;
                        float blue = .0f;

                        if (scaledDepth < 0.25)
                        {
                            red = 1.0f;
                            green = scaledDepth / 0.25f;
                            blue = 0.0f;
                        }
                        else if (scaledDepth < 0.5)
                        {
                            red = 1.0f - (scaledDepth - 0.25f) / 0.25f;
                            green = 1.0f;
                            blue = 0.0f;
                        }
                        else if (scaledDepth < 0.75)
                        {
                            red = 0.0f;
                            green = 1.0f;
                            blue = (scaledDepth - 0.5f) / 0.25f;
                        }
                        else
                        {
                            red = 0.0f;
                            green = 1 - (scaledDepth - 0.75f) / 0.25f;
                            blue = 1.0f;
                        }

                        dst[0] = (byte)(blue * 255);
                        dst[1] = (byte)(green * 255);
                        dst[2] = (byte)(red * 255);
                        dst[3] = 255;

                        dst += 4;
                        src += 1;
                    }
                });
            }
        }

        /// <summary>
        /// Determines the dimensions of an image after it has been rotated.
        /// </summary>
        /// <param name="imageWidth">Width (in pixels) of original image.</param>
        /// <param name="imageHeight">Height (in pixels) of original image.</param>
        /// <param name="angleInDegrees">Angle (in degrees) of rotation being applied.</param>
        /// <param name="fit">Used to describe the fit of the output image. Tight=output image is cropped to match exactly the required size. Loose=output image will be maximum size possible (i.e. length of source image diagonal).</param>
        /// <param name="rotatedWidth">Outputs the rotated image's width.</param>
        /// <param name="rotatedHeight">Outputs the rotated image's height.</param>
        /// <param name="originx">The X coordinate of the origin after rotation (maybe negative).</param>
        /// <param name="originy">The Y coordinate of the origin after rotation (maybe negative).</param>
        public static void DetermineRotatedWidthHeight(int imageWidth, int imageHeight, float angleInDegrees, RotationFitMode fit, out int rotatedWidth, out int rotatedHeight, out float originx, out float originy)
        {
            float ca = (float)System.Math.Cos(angleInDegrees * System.Math.PI / 180.0f);
            float sa = (float)System.Math.Sin(angleInDegrees * System.Math.PI / 180.0f);
            float minx = 0.0f;
            float miny = 0.0f;
            float maxx = 0.0f;
            float maxy = 0.0f;
            AddRotatedPointToBBox((float)(imageWidth - 1), 0.0f, ref minx, ref miny, ref maxx, ref maxy, ca, sa);
            AddRotatedPointToBBox((float)(imageWidth - 1), (float)(imageHeight - 1), ref minx, ref miny, ref maxx, ref maxy, ca, sa);
            AddRotatedPointToBBox(0.0f, (float)(imageHeight - 1), ref minx, ref miny, ref maxx, ref maxy, ca, sa);
            int computedWidth = (int)(maxx - minx + 1);
            int computedHeight = (int)(maxy - miny + 1);
            if (fit == RotationFitMode.Tight)
            {
                rotatedWidth = computedWidth;
                rotatedHeight = computedHeight;
                originx = minx;
                originy = miny;
            }
            else
            {
                float diagonal = (float)Math.Sqrt(imageWidth * imageWidth + imageHeight * imageHeight);
                int additionalOffsetX = ((int)diagonal - computedWidth) / 2;
                int additionalOffsetY = ((int)diagonal - computedHeight) / 2;
                rotatedWidth = (int)diagonal;
                rotatedHeight = (int)diagonal;
                originx = minx - additionalOffsetX;
                originy = miny - additionalOffsetY;
            }
        }

        /// <summary>
        /// Determines the average color of an image.
        /// </summary>
        /// <param name="image">The image to inspect.</param>
        /// <returns>A color with the average RGB values of the image.</returns>
        public static Color AverageColor(this Image image)
        {
            return image.AverageColor(0, 0, image.Width, image.Height);
        }

        /// <summary>
        /// Determines the average color of a region of an image.
        /// </summary>
        /// <param name="image">The image to inspect.</param>
        /// <param name="left">The left of the region to determine average color for.</param>
        /// <param name="top">The top of the region to determine average color for.</param>
        /// <param name="width">The width of the region to determine average color for.</param>
        /// <param name="height">The height of the region to determine average color for.</param>
        /// <returns>A color with the average RGB values of the region.</returns>
        public static Color AverageColor(this Image image, int left, int top, int width, int height)
        {
            var (r, g, b) = image.AverageColorF(left, top, width, height);
            return Color.FromArgb((byte)Math.Round(255 * r), (byte)Math.Round(255 * g), (byte)Math.Round(255 * b));
        }

        /// <summary>
        /// Determines the average color of an image.
        /// </summary>
        /// <param name="image">The image to inspect.</param>
        /// <returns>A triple containing the average RGB values of the region, in the range from 0.0 (lowest intensity) to 1.0 (highest intensity).</returns>
        public static (double R, double G, double B) AverageColorF(this Image image)
        {
            return image.AverageColorF(0, 0, image.Width, image.Height);
        }

        /// <summary>
        /// Determines the average color of a region of an image.
        /// </summary>
        /// <param name="image">The image to inspect.</param>
        /// <param name="left">The left of the region to determine average color for.</param>
        /// <param name="top">The top of the region to determine average color for.</param>
        /// <param name="width">The width of the region to determine average color for.</param>
        /// <param name="height">The height of the region to determine average color for.</param>
        /// <returns>A triple containing the average RGB values of the region, in the range from 0.0 (lowest intensity) to 1.0 (highest intensity).</returns>
        public static (double R, double G, double B) AverageColorF(this Image image, int left, int top, int width, int height)
        {
            double totalR = 0;
            double totalG = 0;
            double totalB = 0;

            if (top < 0)
            {
                top = 0;
            }

            if (left < 0)
            {
                left = 0;
            }

            if (left + width > image.Width)
            {
                width = image.Width - left;
            }

            if (top + height > image.Height)
            {
                height = image.Height - top;
            }

            unsafe
            {
                byte* ptrFirstPixel = (byte*)image.ImageData.ToPointer();
                var bytesPerPixel = image.BitsPerPixel / 8;

                for (var y = top; y < (top + height); y++)
                {
                    byte* ptrCurrentRow = ptrFirstPixel + (y * image.Stride);

                    for (var x = left; x < (left + width); x++)
                    {
                        byte* ptrCurrentPixel = ptrCurrentRow + x * bytesPerPixel;

                        switch (image.PixelFormat)
                        {
                            case PixelFormat.BGRA_32bpp:
                            case PixelFormat.BGRX_32bpp:
                            case PixelFormat.BGR_24bpp:
                                totalR += ptrCurrentPixel[2];
                                totalG += ptrCurrentPixel[1];
                                totalB += ptrCurrentPixel[0];
                                break;

                            case PixelFormat.RGB_24bpp:
                                totalR += ptrCurrentPixel[0];
                                totalG += ptrCurrentPixel[1];
                                totalB += ptrCurrentPixel[2];
                                break;

                            case PixelFormat.RGBA_64bpp:
                                totalR += ((ushort*)ptrCurrentPixel)[0];
                                totalG += ((ushort*)ptrCurrentPixel)[1];
                                totalB += ((ushort*)ptrCurrentPixel)[2];
                                break;

                            case PixelFormat.Gray_8bpp:
                                totalR += ptrCurrentPixel[0];
                                totalG += ptrCurrentPixel[0];
                                totalB += ptrCurrentPixel[0];
                                break;

                            case PixelFormat.Gray_16bpp:
                                totalR += ((ushort*)ptrCurrentPixel)[0];
                                totalG += ((ushort*)ptrCurrentPixel)[0];
                                totalB += ((ushort*)ptrCurrentPixel)[0];
                                break;

                            default:
                                throw new NotSupportedException($"Pixel format {image.PixelFormat} not supported by Image.AverageColor().");
                        }
                    }
                }
            }

            var totalPixels = width * height;
            var scale = image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp ? 65535 : 255;
            return (totalR / totalPixels / scale, totalG / totalPixels / scale, totalB / totalPixels / scale);
        }

        private static void AddRotatedPointToBBox(float x, float y, ref float minx, ref float miny, ref float maxx, ref float maxy, float ca, float sa)
        {
            float nx = (x * ca) - (y * sa);
            float ny = (x * sa) + (y * ca);
            if (nx < minx)
            {
                minx = nx;
            }

            if (nx > maxx)
            {
                maxx = nx;
            }

            if (ny < miny)
            {
                miny = ny;
            }

            if (ny > maxy)
            {
                maxy = ny;
            }
        }
    }

    /// <summary>
    /// Set of operators used for drawing on an image.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Draws a rectangle at the specified pixel coordinates on the image.
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="rect">Pixel coordinates for rectangle.</param>
        /// <param name="color">Color to use for drawing.</param>
        /// <param name="width">Width of line.</param>
        public static void DrawRectangle(this Image image, Rectangle rect, Color color, int width)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.DrawRectangle(rect, color, width);
                image.CopyFrom(tmpImage);
                return;
            }

            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var pen = new Pen(new SolidBrush(color));
            pen.Width = width;
            graphics.DrawRectangle(pen, rect);
        }

        /// <summary>
        /// Draws a line from point p0 to p1 in pixel coordinates on the image.
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="p0">Pixel coordinates for start of line.</param>
        /// <param name="p1">Pixel coordinates for end of line.</param>
        /// <param name="color">Color to use for drawing.</param>
        /// <param name="width">Width of line.</param>
        public static void DrawLine(this Image image, Point p0, Point p1, Color color, int width)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.DrawLine(p0, p1, color, width);
                image.CopyFrom(tmpImage);
                return;
            }

            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var pen = new Pen(new SolidBrush(color));
            pen.Width = width;
            graphics.DrawLine(pen, p0, p1);
        }

        /// <summary>
        /// Draws a circle centered at the specified pixel (p0) with the specified radius.
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="p0">Pixel coordinates for center of circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="color">Color to use for drawing.</param>
        /// <param name="width">Width of line.</param>
        public static void DrawCircle(this Image image, Point p0, int radius, Color color, int width)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.DrawCircle(p0, radius, color, width);
                image.CopyFrom(tmpImage);
                return;
            }

            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var pen = new Pen(new SolidBrush(color));
            pen.Width = width;
            graphics.DrawEllipse(pen, p0.X - radius, p0.Y - radius, 2 * radius, 2 * radius);
        }

        /// <summary>
        /// Renders text on the image at the specified pixel (p0).
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="str">Text to render.</param>
        /// <param name="p0">Pixel coordinates for center of circle.</param>
        /// <param name="color">Color to use when drawing text. Optional.</param>
        /// <param name="font">Name of font to use. Optional.</param>
        /// <param name="fontSize">Size of font. Optional.</param>
        public static void DrawText(this Image image, string str, Point p0, Color color = default, string font = "Arial", float fontSize = 24.0f)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.DrawText(str, p0, color, font, fontSize);
                image.CopyFrom(tmpImage);
                return;
            }

            font ??= "Arial";
            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var drawFont = new Font(font, fontSize);
            using var drawBrush = new SolidBrush(color);
            using var drawFormat = new StringFormat();
            drawFormat.FormatFlags = 0;
            graphics.DrawString(str, drawFont, drawBrush, p0.X, p0.Y, drawFormat);
        }

        /// <summary>
        /// Renders text on the image at the specified pixel (p0).
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="str">Text to render.</param>
        /// <param name="p0">Pixel coordinates for upper-left corner of the text.</param>
        /// <param name="backgroundColor">Background color to use when drawing text.</param>
        /// <param name="textColor">Color to use to draw the text.</param>
        /// <param name="font">Name of font to use. Optional.</param>
        /// <param name="fontSize">Size of font. Optional.</param>
        public static void DrawText(this Image image, string str, Point p0, Color backgroundColor, Color textColor, string font = "Arial", float fontSize = 24.0f)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.DrawText(str, p0, backgroundColor, textColor, font, fontSize);
                image.CopyFrom(tmpImage);
                return;
            }

            font ??= "Arial";
            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var drawFont = new Font(font, fontSize);
            using var textBrush = new SolidBrush(textColor);
            using var backgroundBrush = new SolidBrush(backgroundColor);
            using var drawFormat = new StringFormat();
            drawFormat.FormatFlags = 0;

            SizeF textSize = graphics.MeasureString(str, drawFont);

            // Drawing the background before drawing the text
            var bg = new RectangleF(p0.X, p0.Y, textSize.Width, textSize.Height);
            graphics.FillRectangle(backgroundBrush, bg);
            graphics.DrawString(str, drawFont, textBrush, p0.X, p0.Y, drawFormat);
        }

        /// <summary>
        /// Fills a rectangle at the specified pixel coordinates on the image.
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="rect">Pixel coordinates for rectangle.</param>
        /// <param name="color">Color to use for drawing.</param>
        public static void FillRectangle(this Image image, Rectangle rect, Color color)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.FillRectangle(rect, color);
                image.CopyFrom(tmpImage);
                return;
            }

            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var drawingBrush = new SolidBrush(color);
            graphics.FillRectangle(drawingBrush, rect);
        }

        /// <summary>
        /// Fills a circle centered at the specified pixel (p0) with the specified radius.
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="p0">Pixel coordinates for center of circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="color">Color to use for drawing.</param>
        public static void FillCircle(this Image image, Point p0, int radius, Color color)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp || image.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new InvalidOperationException(
                    "Drawing on 16bpp and 64bpp images is not currently supported. " +
                    "Convert to a supported format such as 8bpp grayscale or 24/32bpp color first.");
            }

            // If our image is 8bpp we won't be able to call Graphics.FromImage because
            // that call doesn't support the 8bpp pixel format. See:
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.fromimage?view=dotnet-plat-ext-3.1
            // for details.
            //
            // Additionally, there is no corresponding System pixel format for RGB 24bpp.
            //
            // To work around these issues, we will convert the image to 24bpp, perform the operation,
            // and then convert back to the original format.
            if (image.PixelFormat == PixelFormat.Gray_8bpp || image.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((image.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(image.Width, image.Height, stride, PixelFormat.BGR_24bpp);
                image.CopyTo(tmpImage);
                tmpImage.FillCircle(p0, radius, color);
                image.CopyFrom(tmpImage);
                return;
            }

            using Bitmap bm = image.ToBitmap(false);
            using var graphics = Graphics.FromImage(bm);
            using var drawingBrush = new SolidBrush(color);
            graphics.FillEllipse(drawingBrush, p0.X - radius, p0.Y - radius, 2 * radius, 2 * radius);
        }
    }

    /// <summary>
    /// Set of transforms for copying image data.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Copies a source image into a destination image using the specified masking image.
        /// See <see cref="CopyTo(Image, Rectangle, Image, Point, Image)"/> for further details.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="destImage">Destination image.</param>
        /// <param name="maskImage">Masking image. If null then ignored.</param>
        public static void CopyTo(this Image srcImage, Image destImage, Image maskImage)
        {
            if (destImage.PixelFormat != srcImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (srcImage.Width != destImage.Width || srcImage.Height != destImage.Height)
            {
                throw new System.Exception(Image.ExceptionDescriptionSourceDestImageMismatch);
            }

            var srcRect = new Rectangle(0, 0, srcImage.Width - 1, srcImage.Height - 1);
            srcImage.CopyTo(srcRect, destImage, new Point(0, 0), maskImage);
        }

        /// <summary>
        /// Copies a portion of the source image into a destination image.
        /// See <see cref="CopyTo(Image, Rectangle, Image, Point, Image)"/> for further details.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="destImage">Destination image.</param>
        /// <param name="rect">Rectangle to copy.</param>
        public static void CopyTo(this Image srcImage, Image destImage, Rectangle rect)
        {
            if (destImage.PixelFormat != srcImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (srcImage.Width != destImage.Width || srcImage.Height != destImage.Height)
            {
                throw new ArgumentException(Image.ExceptionDescriptionSourceDestImageMismatch);
            }

            srcImage.CopyTo(rect, destImage, new Point(rect.Left, rect.Right), null);
        }

        /// <summary>
        /// Copies a portion of a source image into a destination image.
        /// See <see cref="CopyTo(Image, Rectangle, Image, Point, Image)"/> for further details.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="srcRect">Source rectangle to copy from.</param>
        /// <param name="destImage">Destination image.</param>
        /// <param name="destTopLeftPoint">Top left corner of destination image where to copy to.</param>
        public static void CopyTo(this Image srcImage, Rectangle srcRect, Image destImage, Point destTopLeftPoint)
        {
            if (destImage.PixelFormat != srcImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            srcImage.CopyTo(srcRect, destImage, destTopLeftPoint, null);
        }

        /// <summary>
        /// Copies a portion of a source image into a destination image using the specified masking image.
        /// Only pixels from the 'srcImage' inside the 'srcRect' are copied. If a 'maskImage' is specified
        /// (it maybe null, in which case no mask is applied) then source pixels are only copied if their
        /// corresponding mask pixel is > 0. The copied pixels are placed in the 'destImage' at a rectangle
        /// the same size (potentially clipped by the 'destImage' boundaries) located at 'destTopLeftCorner'.
        ///
        /// The following picture may help clarify. In the picture '2' are pixels that are potentially copied
        /// and 'x' are pixels in the 'maskImage' with values > 0.
        ///
        /// \verbatim
        ///       srcImage                       maskImage                               destImage
        ///      +-------------------------+   +-------------------------+             +-------------------------+
        ///      |   srcRect               |   |   (srcRect)             |             |                         |
        ///      |   +---------+           |   |   ...........           |             |                         |
        ///      |   |222222222|           |   |   .     xxxx.xxxx       |   CopyTo    |                         |
        ///      |   |222222222|           | + |   .   xxxxxx.xxxxxx     |  ========>  |       destTopLeftCorner |
        ///      |   |222222222|           |   |   .  xxxxxxx.xxxxx      |             |           O---------+   |
        ///      |   +---------+           |   |   ...........           |             |           |     2222|   |
        ///      |                         |   |                         |             |           |   222222|   |
        ///      +-------------------------+   +-------------------------+             +-----------+---------+---+
        ///                                                                     dropped pixels =>  .  xxxxxxx.
        ///                                                                     due to being       +.........+
        ///                                                                     outside image
        ///                                                                     boundary
        /// \endverbatim
        /// .
        ///
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="srcRect">Source rectangle to copy from.</param>
        /// <param name="destImage">Destination image.</param>
        /// <param name="destTopLeftCorner">Top left corner of destination image where to copy to.</param>
        /// <param name="maskImage">Masking image. If null then ignored.</param>
        public static void CopyTo(this Image srcImage, Rectangle srcRect, Image destImage, Point destTopLeftCorner, Image maskImage)
        {
            if (destImage.PixelFormat != srcImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (maskImage != null)
            {
                if (srcImage.Width != maskImage.Width || srcImage.Height != maskImage.Height)
                {
                    throw new ArgumentException("Mask image size must match source image size");
                }

                if (maskImage.PixelFormat != PixelFormat.Gray_8bpp)
                {
                    throw new ArgumentException("Mask image must be of type PixelFormat.Gray_8bpp");
                }
            }

            // Clip source rectangle against source image size
            int srcX = (srcRect.X < 0) ? 0 : srcRect.X;
            int srcY = (srcRect.Y < 0) ? 0 : srcRect.Y;
            int srcW = (srcRect.X + srcRect.Width > srcImage.Width) ? (srcImage.Width - srcRect.X) : srcRect.Width;
            int srcH = (srcRect.Y + srcRect.Height > srcImage.Height) ? (srcImage.Height - srcRect.Y) : srcRect.Height;

            // Clip destination point against destination image
            int dstX = (destTopLeftCorner.X < 0) ? 0 : destTopLeftCorner.X;
            int dstY = (destTopLeftCorner.Y < 0) ? 0 : destTopLeftCorner.Y;
            dstX = (dstX >= destImage.Width) ? destImage.Width - 1 : dstX;
            dstY = (dstY >= destImage.Height) ? destImage.Height - 1 : dstY;

            // Next clip further if rect of that size would lie outside the destination image
            srcW = (dstX + srcW > destImage.Width) ? (destImage.Width - dstX) : srcW;
            srcH = (dstY + srcH > destImage.Height) ? (destImage.Height - dstY) : srcH;

            PixelFormat srcFormat = srcImage.PixelFormat;
            PixelFormat dstFormat = destImage.PixelFormat;
            var sourceBuffer = srcImage.ImageData;
            var destBuffer = destImage.ImageData;
            var maskBuffer = (maskImage != null) ? maskImage.ImageData : IntPtr.Zero;
            unsafe
            {
                int srcBytesPerPixel = srcFormat.GetBytesPerPixel();
                int dstBytesPerPixel = dstFormat.GetBytesPerPixel();
                int maskBytesPerPixel = PixelFormat.Gray_8bpp.GetBytesPerPixel();
                byte* srcRow = (byte*)sourceBuffer.ToPointer() + (srcY * srcImage.Stride) + (srcX * srcBytesPerPixel);
                byte* dstRow = (byte*)destBuffer.ToPointer() + (dstY * destImage.Stride) + (dstX * dstBytesPerPixel);
                byte* maskRow = null;
                if (maskImage != null)
                {
                    maskRow = (byte*)maskBuffer.ToPointer() + (srcY * maskImage.Stride) + (srcX * maskBytesPerPixel);
                }

                for (int i = 0; i < srcH; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    byte* maskCol = maskRow;
                    for (int j = 0; j < srcW; j++)
                    {
                        bool copyPixel = true;
                        if (maskImage != null)
                        {
                            if (*maskCol == 0)
                            {
                                copyPixel = false;
                            }
                        }

                        if (copyPixel)
                        {
                            int red = 0;
                            int green = 0;
                            int blue = 0;
                            int alpha = 255;
                            switch (srcFormat)
                            {
                                case PixelFormat.Gray_8bpp:
                                    red = green = blue = srcCol[0];
                                    break;

                                case PixelFormat.Gray_16bpp:
                                    red = green = blue = ((ushort*)srcCol)[0];
                                    break;

                                case PixelFormat.BGR_24bpp:
                                    blue = srcCol[0];
                                    green = srcCol[1];
                                    red = srcCol[2];
                                    break;

                                case PixelFormat.BGRX_32bpp:
                                    blue = srcCol[0];
                                    green = srcCol[1];
                                    red = srcCol[2];
                                    break;

                                case PixelFormat.BGRA_32bpp:
                                    blue = srcCol[0];
                                    green = srcCol[1];
                                    red = srcCol[2];
                                    alpha = srcCol[3];
                                    break;

                                case PixelFormat.RGB_24bpp:
                                    red = srcCol[0];
                                    green = srcCol[1];
                                    blue = srcCol[2];
                                    break;

                                case PixelFormat.RGBA_64bpp:
                                    red = ((ushort*)srcCol)[0];
                                    green = ((ushort*)srcCol)[1];
                                    blue = ((ushort*)srcCol)[2];
                                    alpha = ((ushort*)srcCol)[3];
                                    break;

                                case PixelFormat.Undefined:
                                default:
                                    throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                            }

                            switch (dstFormat)
                            {
                                case PixelFormat.Gray_8bpp:
                                    dstCol[0] = Operators.Rgb2Gray((byte)red, (byte)green, (byte)blue);
                                    break;

                                case PixelFormat.Gray_16bpp:
                                    ((ushort*)dstCol)[0] = Operators.Rgb2Gray((ushort)red, (ushort)green, (ushort)blue);
                                    break;

                                case PixelFormat.BGR_24bpp:
                                case PixelFormat.BGRX_32bpp:
                                    dstCol[0] = (byte)blue;
                                    dstCol[1] = (byte)green;
                                    dstCol[2] = (byte)red;
                                    dstCol[3] = 255;
                                    break;

                                case PixelFormat.BGRA_32bpp:
                                    dstCol[0] = (byte)blue;
                                    dstCol[1] = (byte)green;
                                    dstCol[2] = (byte)red;
                                    dstCol[3] = (byte)alpha;
                                    break;

                                case PixelFormat.RGB_24bpp:
                                    dstCol[0] = (byte)red;
                                    dstCol[1] = (byte)green;
                                    dstCol[2] = (byte)blue;
                                    break;

                                case PixelFormat.RGBA_64bpp:
                                    ((ushort*)dstCol)[0] = (ushort)red;
                                    ((ushort*)dstCol)[1] = (ushort)green;
                                    ((ushort*)dstCol)[2] = (ushort)blue;
                                    ((ushort*)dstCol)[3] = (ushort)alpha;
                                    break;

                                case PixelFormat.Undefined:
                                default:
                                    throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                            }
                        }

                        srcCol += srcBytesPerPixel;
                        dstCol += dstBytesPerPixel;
                        maskCol += maskBytesPerPixel;
                    }

                    srcRow += srcImage.Stride;
                    dstRow += destImage.Stride;
                    if (maskImage != null)
                    {
                        maskRow += maskImage.Stride;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Basic color transforms on images.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Inverts each color component in an image.
        /// </summary>
        /// <param name="srcImage">Source image to invert.</param>
        /// <returns>Returns an new image with the inverted results.</returns>
        public static Image Invert(this Image srcImage)
        {
            var invertedImage = new Image(srcImage.Width, srcImage.Height, srcImage.PixelFormat);
            srcImage.Invert(invertedImage);
            return invertedImage;
        }

        /// <summary>
        /// Inverts each color component in an image.
        /// </summary>
        /// <param name="srcImage">Source image to invert.</param>
        /// <param name="destImage">Destination image where to store inverted results.</param>
        public static void Invert(this Image srcImage, Image destImage)
        {
            if (destImage.PixelFormat != srcImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (srcImage.Width != destImage.Width || srcImage.Height != destImage.Height)
            {
                throw new Exception(Image.ExceptionDescriptionSourceDestImageMismatch);
            }

            unsafe
            {
                int srcBytesPerPixel = srcImage.PixelFormat.GetBytesPerPixel();
                int dstBytesPerPixel = destImage.PixelFormat.GetBytesPerPixel();
                byte* srcRow = (byte*)srcImage.ImageData.ToPointer();
                byte* dstRow = (byte*)destImage.ImageData.ToPointer();
                for (int i = 0; i < srcImage.Height; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    for (int j = 0; j < srcImage.Width; j++)
                    {
                        switch (srcImage.PixelFormat)
                        {
                            case PixelFormat.Gray_8bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                break;

                            case PixelFormat.Gray_16bpp:
                                ((ushort*)dstCol)[0] = (ushort)(65535 - ((ushort*)srcCol)[0]);
                                break;

                            case PixelFormat.BGR_24bpp:
                            case PixelFormat.RGB_24bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                dstCol[1] = (byte)(255 - srcCol[1]);
                                dstCol[2] = (byte)(255 - srcCol[2]);
                                break;

                            case PixelFormat.BGRX_32bpp:
                            case PixelFormat.BGRA_32bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                dstCol[1] = (byte)(255 - srcCol[1]);
                                dstCol[2] = (byte)(255 - srcCol[2]);
                                dstCol[3] = (byte)srcCol[3];
                                break;

                            case PixelFormat.RGBA_64bpp:
                                ((ushort*)dstCol)[0] = (ushort)(65535 - ((ushort*)srcCol)[0]);
                                ((ushort*)dstCol)[1] = (ushort)(65535 - ((ushort*)srcCol)[1]);
                                ((ushort*)dstCol)[2] = (ushort)(65535 - ((ushort*)srcCol)[2]);
                                ((ushort*)dstCol)[3] = (ushort)((ushort*)srcCol)[3];
                                break;

                            case PixelFormat.Undefined:
                            default:
                                throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                        }

                        srcCol += srcBytesPerPixel;
                        dstCol += dstBytesPerPixel;
                    }

                    srcRow += srcImage.Stride;
                    dstRow += destImage.Stride;
                }
            }
        }

        /// <summary>
        /// Clears each color component in an image to the specified color.
        /// </summary>
        /// <param name="image">Image to clear.</param>
        /// <param name="clr">Color to clear to.</param>
        public static void Clear(this Image image, Color clr)
        {
            void ClearFast(byte b)
            {
                unsafe
                {
                    Unsafe.InitBlockUnaligned(image.ImageData.ToPointer(), b, (uint)image.Size);
                }
            }

            switch (image.PixelFormat)
            {
                case PixelFormat.Gray_8bpp:
                case PixelFormat.Gray_16bpp:
                    ClearFast(Rgb2Gray(clr.R, clr.G, clr.B));
                    return;

                case PixelFormat.BGR_24bpp:
                    if (clr.R == clr.G && clr.G == clr.B)
                    {
                        ClearFast(clr.R);
                        return;
                    }

                    break;

                case PixelFormat.BGRX_32bpp:
                    if (clr.R == clr.G && clr.G == clr.B && clr.B == 255)
                    {
                        ClearFast(255);
                        return;
                    }

                    break;

                case PixelFormat.BGRA_32bpp:
                case PixelFormat.RGBA_64bpp:
                    if (clr.R == clr.G && clr.G == clr.B && clr.B == clr.A)
                    {
                        ClearFast(clr.R);
                        return;
                    }

                    break;
            }

            ClearSlow(image, clr);
        }

        /// <summary>
        /// Extracts a single channel from the image and returns it as a gray scale image.
        /// </summary>
        /// <param name="image">Image to extract from.</param>
        /// <param name="channel">Index of channel to extract from. This should be one of the following: ExtractRedChannel, ExtractGreenChannel, ExtractBlueChannel, or ExtractAlphaChannel.</param>
        /// <returns>Returns a new grayscale image containing the color from the specified channel in the original source image.</returns>
        public static Image ExtractChannel(this Image image, int channel)
        {
            var destImage = new Image(image.Width, image.Height, PixelFormat.Gray_8bpp);
            image.ExtractChannel(destImage, channel);
            return destImage;
        }

        /// <summary>
        /// Extracts a single channel from the image and returns it as a gray scale image.
        /// </summary>
        /// <param name="image">Image to extract from.</param>
        /// <param name="destImage">Image to write results to.</param>
        /// <param name="channel">Index of channel to extract from. This should be one of the following: ExtractRedChannel, ExtractGreenChannel, ExtractBlueChannel, or ExtractAlphaChannel.</param>
        public static void ExtractChannel(this Image image, Image destImage, int channel)
        {
            if (image.Width != destImage.Width || image.Height != destImage.Height)
            {
                throw new InvalidOperationException(Image.ExceptionDescriptionSourceDestImageMismatch);
            }

            if (destImage.PixelFormat != PixelFormat.Gray_8bpp)
            {
                throw new ArgumentException("Destination must be of pixel format type: Gray_8bpp.");
            }

            if (image.PixelFormat != PixelFormat.BGRA_32bpp &&
                image.PixelFormat != PixelFormat.BGRX_32bpp &&
                image.PixelFormat != PixelFormat.BGR_24bpp)
            {
                throw new InvalidOperationException("Extract only supports the following pixel formats: BGRA_32bpp, BGRX_32bpp, and BGR_24bpp");
            }

            if (channel < 0 ||
                (image.PixelFormat != PixelFormat.BGR_24bpp && channel > 3) ||
                (image.PixelFormat == PixelFormat.BGR_24bpp && channel > 2))
            {
                throw new ArgumentException("Unsupported channel");
            }

            unsafe
            {
                int srcBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                int dstBytesPerPixel = PixelFormat.Gray_8bpp.GetBytesPerPixel();
                byte* srcRow = (byte*)image.ImageData.ToPointer();
                byte* dstRow = (byte*)destImage.ImageData.ToPointer();
                for (int i = 0; i < image.Height; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    for (int j = 0; j < image.Width; j++)
                    {
                        dstCol[0] = srcCol[channel];
                        srcCol += srcBytesPerPixel;
                        dstCol += dstBytesPerPixel;
                    }

                    srcRow += image.Stride;
                    dstRow += destImage.Stride;
                }
            }
        }

        /// <summary>
        /// Clears each color component in an image to the specified color.
        /// </summary>
        /// <param name="image">Image to clear.</param>
        /// <param name="clr">Color to clear to.</param>
        private static void ClearSlow(Image image, Color clr)
        {
            unsafe
            {
                switch (image.PixelFormat)
                {
                    case PixelFormat.BGR_24bpp:
                        int srcBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                        byte* srcRow = (byte*)image.ImageData.ToPointer();
                        for (int i = 0; i < image.Height; i++)
                        {
                            byte* srcCol = srcRow;
                            for (int j = 0; j < image.Width; j++)
                            {
                                srcCol[0] = clr.B;
                                srcCol[1] = clr.G;
                                srcCol[2] = clr.R;
                                srcCol += srcBytesPerPixel;
                            }

                            srcRow += image.Stride;
                        }

                        break;

                    case PixelFormat.BGRX_32bpp:
                        srcBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                        srcRow = (byte*)image.ImageData.ToPointer();
                        for (int i = 0; i < image.Height; i++)
                        {
                            byte* srcCol = srcRow;
                            for (int j = 0; j < image.Width; j++)
                            {
                                srcCol[0] = clr.B;
                                srcCol[1] = clr.G;
                                srcCol[2] = clr.R;
                                srcCol[3] = 255;
                                srcCol += srcBytesPerPixel;
                            }

                            srcRow += image.Stride;
                        }

                        break;

                    case PixelFormat.BGRA_32bpp:
                        srcBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                        srcRow = (byte*)image.ImageData.ToPointer();
                        for (int i = 0; i < image.Height; i++)
                        {
                            byte* srcCol = srcRow;
                            for (int j = 0; j < image.Width; j++)
                            {
                                srcCol[0] = clr.B;
                                srcCol[1] = clr.G;
                                srcCol[2] = clr.R;
                                srcCol[3] = clr.A;
                                srcCol += srcBytesPerPixel;
                            }

                            srcRow += image.Stride;
                        }

                        break;

                    case PixelFormat.RGB_24bpp:
                        srcBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                        srcRow = (byte*)image.ImageData.ToPointer();
                        for (int i = 0; i < image.Height; i++)
                        {
                            byte* srcCol = srcRow;
                            for (int j = 0; j < image.Width; j++)
                            {
                                srcCol[0] = clr.R;
                                srcCol[1] = clr.G;
                                srcCol[2] = clr.B;
                                srcCol += srcBytesPerPixel;
                            }

                            srcRow += image.Stride;
                        }

                        break;

                    case PixelFormat.RGBA_64bpp:
                        srcBytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                        srcRow = (byte*)image.ImageData.ToPointer();
                        for (int i = 0; i < image.Height; i++)
                        {
                            byte* srcCol = srcRow;
                            for (int j = 0; j < image.Width; j++)
                            {
                                ((ushort*)srcCol)[0] = (ushort)((clr.R << 8) | clr.R);
                                ((ushort*)srcCol)[1] = (ushort)((clr.G << 8) | clr.G);
                                ((ushort*)srcCol)[2] = (ushort)((clr.B << 8) | clr.B);
                                ((ushort*)srcCol)[3] = (ushort)((clr.A << 8) | clr.A);
                                srcCol += srcBytesPerPixel;
                            }

                            srcRow += image.Stride;
                        }

                        break;

                    case PixelFormat.Undefined:
                    default:
                        throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                }
            }
        }
    }

    /// <summary>
    /// Imaging math operators.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Performs per channel thresholding on the image.
        /// </summary>
        /// <param name="image">Image to be thresholded.</param>
        /// <param name="threshold">Threshold value.</param>
        /// <param name="maxvalue">Maximum value.</param>
        /// <param name="type">Type of thresholding to perform.</param>
        /// <returns>The thresholded image.</returns>
        public static Image Threshold(this Image image, int threshold, int maxvalue, Threshold type)
        {
            var thresholdedImage = new Image(image.Width, image.Height, image.PixelFormat);
            image.Threshold(thresholdedImage, threshold, maxvalue, type);
            return thresholdedImage;
        }

        /// <summary>
        /// Performs per channel thresholding on the image.
        /// </summary>
        /// <param name="srcImage">Image to be thresholded.</param>
        /// <param name="destImage">Destination image where thresholded results are stored.</param>
        /// <param name="threshold">Threshold value.</param>
        /// <param name="maxvalue">Maximum value.</param>
        /// <param name="type">Type of thresholding to perform.</param>
        public static void Threshold(this Image srcImage, Image destImage, int threshold, int maxvalue, Threshold type)
        {
            if (srcImage.PixelFormat != destImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            unsafe
            {
                int bytesPerPixel = srcImage.PixelFormat.GetBytesPerPixel();
                byte* srcRow = (byte*)srcImage.ImageData.ToPointer();
                byte* dstRow = (byte*)destImage.ImageData.ToPointer();
                for (int i = 0; i < srcImage.Height; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    for (int j = 0; j < srcImage.Width; j++)
                    {
                        int r = 0, g = 0, b = 0, a = 255;
                        switch (srcImage.PixelFormat)
                        {
                            case PixelFormat.BGRA_32bpp:
                                b = srcCol[0];
                                g = srcCol[1];
                                r = srcCol[2];
                                a = srcCol[3];
                                break;

                            case PixelFormat.BGRX_32bpp:
                                b = srcCol[0];
                                g = srcCol[1];
                                r = srcCol[2];
                                break;

                            case PixelFormat.BGR_24bpp:
                                b = srcCol[0];
                                g = srcCol[1];
                                r = srcCol[2];
                                break;

                            case PixelFormat.Gray_16bpp:
                                r = g = b = a = ((ushort*)srcCol)[0];
                                break;

                            case PixelFormat.Gray_8bpp:
                                r = g = b = a = srcCol[0];
                                break;

                            case PixelFormat.RGB_24bpp:
                                r = srcCol[0];
                                g = srcCol[1];
                                b = srcCol[2];
                                break;

                            case PixelFormat.RGBA_64bpp:
                                r = ((ushort*)srcCol)[0];
                                g = ((ushort*)srcCol)[1];
                                b = ((ushort*)srcCol)[2];
                                a = ((ushort*)srcCol)[3];
                                break;

                            case PixelFormat.Undefined:
                            default:
                                throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                        }

                        switch (type)
                        {
                            case Imaging.Threshold.Binary:
                                r = (r > threshold) ? maxvalue : 0;
                                g = (g > threshold) ? maxvalue : 0;
                                b = (b > threshold) ? maxvalue : 0;
                                a = (a > threshold) ? maxvalue : 0;
                                break;

                            case Imaging.Threshold.BinaryInv:
                                r = (r > threshold) ? 0 : maxvalue;
                                g = (g > threshold) ? 0 : maxvalue;
                                b = (b > threshold) ? 0 : maxvalue;
                                a = (a > threshold) ? 0 : maxvalue;
                                break;

                            case Imaging.Threshold.Truncate:
                                r = (r > threshold) ? threshold : r;
                                g = (g > threshold) ? threshold : g;
                                b = (b > threshold) ? threshold : b;
                                a = (a > threshold) ? threshold : a;
                                break;

                            case Imaging.Threshold.ToZero:
                                r = (r > threshold) ? r : 0;
                                g = (g > threshold) ? g : 0;
                                b = (b > threshold) ? b : 0;
                                a = (a > threshold) ? a : 0;
                                break;

                            case Imaging.Threshold.ToZeroInv:
                                r = (r > threshold) ? 0 : r;
                                g = (g > threshold) ? 0 : g;
                                b = (b > threshold) ? 0 : b;
                                a = (a > threshold) ? 0 : a;
                                break;

                            default:
                                throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                        }

                        switch (destImage.PixelFormat)
                        {
                            case PixelFormat.BGRA_32bpp:
                                dstCol[0] = (byte)b;
                                dstCol[1] = (byte)g;
                                dstCol[2] = (byte)r;
                                dstCol[3] = (byte)a;
                                break;

                            case PixelFormat.BGRX_32bpp:
                                dstCol[0] = (byte)b;
                                dstCol[1] = (byte)g;
                                dstCol[2] = (byte)r;
                                dstCol[3] = (byte)a;
                                break;

                            case PixelFormat.BGR_24bpp:
                                dstCol[0] = (byte)b;
                                dstCol[1] = (byte)g;
                                dstCol[2] = (byte)r;
                                break;

                            case PixelFormat.Gray_16bpp:
                                ((ushort*)dstCol)[0] = (ushort)r;
                                break;

                            case PixelFormat.Gray_8bpp:
                                dstCol[0] = (byte)r;
                                break;

                            case PixelFormat.RGB_24bpp:
                                dstCol[0] = (byte)r;
                                dstCol[1] = (byte)g;
                                dstCol[2] = (byte)b;
                                break;

                            case PixelFormat.RGBA_64bpp:
                                ((ushort*)dstCol)[0] = (ushort)r;
                                ((ushort*)dstCol)[1] = (ushort)g;
                                ((ushort*)dstCol)[2] = (ushort)b;
                                ((ushort*)dstCol)[3] = (ushort)a;
                                break;

                            default:
                                throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                        }

                        srcCol += bytesPerPixel;
                        dstCol += bytesPerPixel;
                    }

                    srcRow += srcImage.Stride;
                    dstRow += destImage.Stride;
                }
            }
        }

        /// <summary>
        /// Computes the absolute difference between two images.
        /// </summary>
        /// <param name="imageA">First image.</param>
        /// <param name="imageB">Second image.</param>
        /// <returns>Difference image.</returns>
        public static Image AbsDiff(this Image imageA, Image imageB)
        {
            var diffImage = new Image(imageA.Width, imageA.Height, imageA.PixelFormat);
            imageA.AbsDiff(imageB, diffImage);
            return diffImage;
        }

        /// <summary>
        /// Computes the absolute difference between two images.
        /// </summary>
        /// <param name="imageA">First image.</param>
        /// <param name="imageB">Second image.</param>
        /// <param name="destImage">Destination image where to store difference image.</param>
        public static void AbsDiff(this Image imageA, Image imageB, Image destImage)
        {
            if (imageA.PixelFormat != destImage.PixelFormat || imageB.PixelFormat != destImage.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destImage.PixelFormat", "destination image pixel format doesn't match source image pixel format");
            }

            if (imageA.Width != imageB.Width || imageA.Height != imageB.Height || imageA.PixelFormat != imageB.PixelFormat)
            {
                throw new ArgumentException("Images sizes/types don't match");
            }

            unsafe
            {
                int bytesPerPixel = imageA.PixelFormat.GetBytesPerPixel();
                byte* srcRowA = (byte*)imageA.ImageData.ToPointer();
                byte* srcRowB = (byte*)imageB.ImageData.ToPointer();
                byte* dstRow = (byte*)destImage.ImageData.ToPointer();
                for (int i = 0; i < imageA.Height; i++)
                {
                    byte* srcColA = srcRowA;
                    byte* srcColB = srcRowB;
                    byte* dstCol = dstRow;
                    int delta0, delta1, delta2, delta3;
                    for (int j = 0; j < imageA.Width; j++)
                    {
                        switch (imageA.PixelFormat)
                        {
                            case PixelFormat.BGRA_32bpp:
                                delta0 = srcColA[0] - srcColB[0];
                                delta1 = srcColA[1] - srcColB[1];
                                delta2 = srcColA[2] - srcColB[2];
                                delta3 = srcColA[3] - srcColB[3];
                                dstCol[0] = (byte)((delta0 < 0) ? -delta0 : delta0);
                                dstCol[1] = (byte)((delta1 < 0) ? -delta1 : delta1);
                                dstCol[2] = (byte)((delta2 < 0) ? -delta2 : delta2);
                                dstCol[3] = (byte)((delta3 < 0) ? -delta3 : delta3);
                                break;

                            case PixelFormat.BGRX_32bpp:
                                delta0 = srcColA[0] - srcColB[0];
                                delta1 = srcColA[1] - srcColB[1];
                                delta2 = srcColA[2] - srcColB[2];
                                dstCol[0] = (byte)((delta0 < 0) ? -delta0 : delta0);
                                dstCol[1] = (byte)((delta1 < 0) ? -delta1 : delta1);
                                dstCol[2] = (byte)((delta2 < 0) ? -delta2 : delta2);
                                dstCol[3] = 255;
                                break;

                            case PixelFormat.BGR_24bpp:
                            case PixelFormat.RGB_24bpp:
                                delta0 = srcColA[0] - srcColB[0];
                                delta1 = srcColA[1] - srcColB[1];
                                delta2 = srcColA[2] - srcColB[2];
                                dstCol[0] = (byte)((delta0 < 0) ? -delta0 : delta0);
                                dstCol[1] = (byte)((delta1 < 0) ? -delta1 : delta1);
                                dstCol[2] = (byte)((delta2 < 0) ? -delta2 : delta2);
                                break;

                            case PixelFormat.Gray_16bpp:
                                delta0 = ((ushort*)srcColA)[0] - ((ushort*)srcColB)[0];
                                ((ushort*)dstCol)[0] = (ushort)((delta0 < 0) ? -delta0 : delta0);
                                break;

                            case PixelFormat.Gray_8bpp:
                                delta0 = srcColA[0] - srcColB[0];
                                dstCol[0] = (byte)((delta0 < 0) ? -delta0 : delta0);
                                break;

                            case PixelFormat.RGBA_64bpp:
                                delta0 = (ushort)(((ushort*)srcColA)[0] - ((ushort*)srcColB)[0]);
                                delta1 = (ushort)(((ushort*)srcColA)[1] - ((ushort*)srcColB)[1]);
                                delta2 = (ushort)(((ushort*)srcColA)[2] - ((ushort*)srcColB)[2]);
                                delta3 = (ushort)(((ushort*)srcColA)[3] - ((ushort*)srcColB)[3]);
                                ((ushort*)dstCol)[0] = (ushort)((delta0 < 0) ? -delta0 : delta0);
                                ((ushort*)dstCol)[1] = (ushort)((delta1 < 0) ? -delta1 : delta1);
                                ((ushort*)dstCol)[2] = (ushort)((delta2 < 0) ? -delta2 : delta2);
                                ((ushort*)dstCol)[3] = (ushort)((delta3 < 0) ? -delta3 : delta3);
                                break;

                            case PixelFormat.Undefined:
                            default:
                                throw new ArgumentException(Image.ExceptionDescriptionUnexpectedPixelFormat);
                        }

                        srcColA += bytesPerPixel;
                        srcColB += bytesPerPixel;
                        dstCol += bytesPerPixel;
                    }

                    srcRowA += imageA.Stride;
                    srcRowB += imageB.Stride;
                    dstRow += destImage.Stride;
                }
            }
        }

        /// <summary>
        /// Convolves an image with a specified kernel.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="kernel">The kernel to convolve the image with.</param>
        /// <returns>An image contained the convolution results.</returns>
        public static Image Convolve(this Image image, int[,] kernel)
        {
            var destination = new Image(image.Width, image.Height, image.PixelFormat);
            image.Convolve(destination, kernel);
            return destination;
        }

        /// <summary>
        /// Convolves an image with a specified kernel into a specified destination image.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="destination">The destination image.</param>
        /// <param name="kernel">The kernel to convolve the image with.</param>
        public static void Convolve(this Image image, Image destination, int[,] kernel)
        {
            if (image.PixelFormat != destination.PixelFormat)
            {
                throw new ArgumentOutOfRangeException("destination.PixelFormat", "Destination image pixel format doesn't match source image pixel format.");
            }

            if (image.PixelFormat != PixelFormat.Gray_8bpp &&
                image.PixelFormat != PixelFormat.Gray_16bpp)
            {
                throw new NotSupportedException($"Currently the {nameof(Convolve)} operator only supports grayscale formats.");
            }

            if (image.Width != destination.Width || image.Height != destination.Height)
            {
                throw new ArgumentException("Images sizes/types don't match");
            }

            int kernelHeightHalf = kernel.GetLength(0) / 2;
            int kernelWidthHalf = kernel.GetLength(1) / 2;

            unsafe
            {
                int bytesPerPixel = image.PixelFormat.GetBytesPerPixel();
                byte* srcStart = (byte*)image.ImageData.ToPointer();
                byte* dst = (byte*)destination.ImageData.ToPointer();
                for (int i = 0; i < image.Height; i++)
                {
                    byte* dstCol = dst;
                    for (int j = 0; j < image.Width; j++)
                    {
                        var accumulator = 0;
                        var count = 0;
                        for (int ki = 0; ki < kernel.GetLength(0); ki++)
                        {
                            var row = i - kernelHeightHalf + ki;
                            if ((row >= 0) && (row < image.Height))
                            {
                                var srcRow = srcStart + image.Stride * row;
                                for (int kj = 0; kj < kernel.GetLength(1); kj++)
                                {
                                    var col = j - kernelWidthHalf + kj;
                                    if ((col >= 0) && (col < image.Width))
                                    {
                                        var srcCol = srcRow + col * bytesPerPixel;
                                        switch (image.PixelFormat)
                                        {
                                            case PixelFormat.Gray_8bpp:
                                                accumulator += srcCol[0] * kernel[ki, kj];
                                                count += 1;
                                                break;
                                            case PixelFormat.Gray_16bpp:
                                                accumulator += ((ushort*)srcCol)[0] * kernel[ki, kj];
                                                count += 1;
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        switch (destination.PixelFormat)
                        {
                            case PixelFormat.Gray_16bpp:
                                ((ushort*)dstCol)[0] = (ushort)(accumulator / count);
                                break;
                            case PixelFormat.Gray_8bpp:
                                dstCol[0] = (byte)(accumulator / count);
                                break;
                        }

                        dstCol += bytesPerPixel;
                    }

                    dst += destination.Stride;
                }
            }
        }

        /// <summary>
        /// Clips a rectangle based on the size of an image.
        /// </summary>
        /// <param name="rectangle">The rectangle to clip.</param>
        /// <param name="imageWidth">The image width.</param>
        /// <param name="imageHeight">The image height.</param>
        /// <returns>The clipped rectangle.</returns>
        internal static Rectangle GetImageSizeClippedRectangle(Rectangle rectangle, int imageWidth, int imageHeight)
        {
            if ((rectangle.Left >= imageWidth) ||
                (rectangle.Top >= imageHeight) ||
                (rectangle.Width < 0) ||
                (rectangle.Height < 0))
            {
                return Rectangle.Empty;
            }

            var actualLeft = rectangle.Left;
            var actualRight = rectangle.Left + rectangle.Width;
            var actualTop = rectangle.Top;
            var actualBottom = rectangle.Top + rectangle.Height;

            if (actualLeft < 0)
            {
                actualLeft = 0;
            }

            if (actualTop < 0)
            {
                actualTop = 0;
            }

            if (actualRight > imageWidth)
            {
                actualRight = imageWidth;
            }

            if (actualBottom > imageHeight)
            {
                actualBottom = imageHeight;
            }

            return new Rectangle(actualLeft, actualTop, actualRight - actualLeft, actualBottom - actualTop);
        }
    }
}