// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

namespace Microsoft.Psi.Imaging
{
    using System.Drawing;

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
        /// Flips an image along a specified axis.
        /// </summary>
        /// <param name="image">Image to flip.</param>
        /// <param name="mode">Axis along which to flip.</param>
        /// <returns>A new flipped image.</returns>
        public static Shared<Image> Flip(this Image image, FlipMode mode)
        {
            if (image.PixelFormat == PixelFormat.Gray_16bpp)
            {
                // We can't handle this through GDI.
                Shared<Image> dstImage = ImagePool.GetOrCreate(image.Width, image.Height, image.PixelFormat);
                unsafe
                {
                    int srcBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(image.PixelFormat);
                    int dstBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(dstImage.Resource.PixelFormat);
                    byte* srcRow = (byte*)image.ImageData.ToPointer();
                    byte* dstRow = (byte*)dstImage.Resource.ImageData.ToPointer();
                    int ystep = dstImage.Resource.Stride;
                    if (mode == FlipMode.AlongHorizontalAxis)
                    {
                        dstRow += dstImage.Resource.Stride * (image.Height - 1);
                        ystep = -dstImage.Resource.Stride;
                    }

                    int xstep = dstBytesPerPixel;
                    int xoffset = 0;
                    if (mode == FlipMode.AlongVerticalAxis)
                    {
                        xoffset = dstBytesPerPixel * (dstImage.Resource.Width - 1);
                        xstep = -dstBytesPerPixel;
                    }

                    for (int i = 0; i < image.Height; i++)
                    {
                        byte* srcCol = srcRow;
                        byte* dstCol = dstRow + xoffset;
                        for (int j = 0; j < image.Width; j++)
                        {
                            ((ushort*)dstCol)[0] = ((ushort*)srcCol)[0];
                            srcCol += srcBytesPerPixel;
                            dstCol += xstep;
                        }

                        srcRow += image.Stride;
                        dstRow += ystep;
                    }
                }

                return dstImage;
            }
            else
            {
                using (var bitmap = new Bitmap(image.Width, image.Height))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        switch (mode)
                        {
                            case FlipMode.AlongHorizontalAxis:
                                graphics.TranslateTransform(0.0f, image.Height - 1);
                                graphics.ScaleTransform(1.0f, -1.0f);
                                break;

                            case FlipMode.AlongVerticalAxis:
                                graphics.TranslateTransform(image.Width - 1, 0.0f);
                                graphics.ScaleTransform(-1.0f, 1.0f);
                                break;
                        }

                        using (var dstimage = image.ToManagedImage())
                        {
                            graphics.DrawImage(dstimage, new Point(0, 0));
                        }

                        return ImagePool.GetOrCreate(bitmap);
                    }
                }
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
        public static Shared<Image> Scale(this Image image, float scaleX, float scaleY, SamplingMode mode)
        {
            if (scaleX == 0.0 || scaleY == 0.0)
            {
                throw new System.Exception("Unexpected scale factors");
            }

            if (image.PixelFormat == PixelFormat.Gray_16bpp)
            {
                throw new System.NotSupportedException(
                    "Scaling 16bpp images is not currently supported. " +
                    "Convert to a supported format such as color or 8bpp grayscale first.");
            }

            int dstWidth = (int)(image.Width * scaleX);
            int dstHeight = (int)(image.Height * scaleY);
            using (var bitmap = new Bitmap(dstWidth, dstHeight))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
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

                    graphics.ScaleTransform(scaleX, scaleY);

                    using (var managedimg = image.ToManagedImage())
                    {
                        graphics.DrawImage(managedimg, new Point(0, 0));
                    }

                    return ImagePool.GetOrCreate(bitmap);
                }
            }
        }

        /// <summary>
        /// Rotates an image.
        /// </summary>
        /// <param name="image">Image to rotate.</param>
        /// <param name="angleInDegrees">Number of degrees to rotate in counter clockwise direction.</param>
        /// <param name="mode">Pixel resampling method.</param>
        /// <returns>Rotated image.</returns>
        public static Shared<Image> Rotate(this Image image, float angleInDegrees, SamplingMode mode)
        {
            float ca = (float)System.Math.Cos(angleInDegrees * System.Math.PI / 180.0f);
            float sa = (float)System.Math.Sin(angleInDegrees * System.Math.PI / 180.0f);
            float minx = 0.0f;
            float miny = 0.0f;
            float maxx = 0.0f;
            float maxy = 0.0f;
            float x = image.Width - 1;
            float y = 0.0f;
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

            x = image.Width - 1;
            y = image.Height - 1;
            nx = (x * ca) - (y * sa);
            ny = (x * sa) + (y * ca);
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

            x = 0.0f;
            y = image.Height - 1;
            nx = (x * ca) - (y * sa);
            ny = (x * sa) + (y * ca);
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

            int dstWidth = (int)(maxx - minx + 1);
            int dstHeight = (int)(maxy - miny + 1);
            using (var bitmap = new Bitmap(dstWidth, dstHeight))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
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

                    graphics.TranslateTransform(-minx, -miny);
                    graphics.RotateTransform(angleInDegrees);

                    using (var managedimg = image.ToManagedImage())
                    {
                        graphics.DrawImage(managedimg, new Point(0, 0));
                    }

                    return ImagePool.GetOrCreate(bitmap);
                }
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
            using (Bitmap bm = image.ToManagedImage(false))
            {
                using (var graphics = Graphics.FromImage(bm))
                {
                    using (var pen = new Pen(new SolidBrush(color)))
                    {
                        pen.Width = width;
                        graphics.DrawRectangle(pen, rect);
                    }
                }
            }
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
            using (Bitmap bm = image.ToManagedImage(false))
            {
                using (var graphics = Graphics.FromImage(bm))
                {
                    using (var pen = new Pen(new SolidBrush(color)))
                    {
                        pen.Width = width;
                        graphics.DrawLine(pen, p0, p1);
                    }
                }
            }
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
            using (Bitmap bm = image.ToManagedImage(false))
            {
                using (var graphics = Graphics.FromImage(bm))
                {
                    using (var pen = new Pen(new SolidBrush(color)))
                    {
                        pen.Width = width;
                        graphics.DrawEllipse(pen, p0.X - radius, p0.Y - radius, 2 * radius, 2 * radius);
                    }
                }
            }
        }

        /// <summary>
        /// Renders text on the image at the specified pixel (p0).
        /// </summary>
        /// <param name="image">Image to draw on.</param>
        /// <param name="str">Text to render.</param>
        /// <param name="p0">Pixel coordinates for center of circle.</param>
        public static void DrawText(this Image image, string str, Point p0)
        {
            using (Bitmap bm = image.ToManagedImage(false))
            {
                using (var graphics = Graphics.FromImage(bm))
                {
                    using (Font drawFont = new Font("Arial", 24))
                    {
                        using (SolidBrush drawBrush = new SolidBrush(Color.Black))
                        {
                            using (StringFormat drawFormat = new StringFormat())
                            {
                                drawFormat.FormatFlags = 0;
                                graphics.DrawString(str, drawFont, drawBrush, p0.X, p0.Y, drawFormat);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Set of transforms for copying image data.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Copies a source image into a destination image using the specified masking image.
        /// Only pixels in the source image whose corresponding mask image pixels are > 0
        /// are copied to the destination image.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="dstImage">Destination image.</param>
        /// <param name="maskImage">Masking image.</param>
        public static void CopyTo(this Image srcImage, Image dstImage, Image maskImage)
        {
            if (srcImage.Width != dstImage.Width || srcImage.Height != dstImage.Height)
            {
                throw new System.Exception("Source and destination images must be the same size");
            }

            Rectangle srcRect = new Rectangle(0, 0, srcImage.Width - 1, srcImage.Height - 1);
            Rectangle dstRect = new Rectangle(0, 0, dstImage.Width - 1, dstImage.Height - 1);
            srcImage.CopyTo(srcRect, dstImage, dstRect, maskImage);
        }

        /// <summary>
        /// Copies a source image into a destination image using the specified masking image.
        /// Only pixels in the source image whose corresponding mask image pixels are > 0
        /// are copied to the destination image. Only pixels from the srcRect are copied
        /// to the destination rect.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="dstImage">Destination image.</param>
        /// <param name="rect">Rectangle to copy.</param>
        public static void CopyTo(this Image srcImage, Image dstImage, Rectangle rect)
        {
            if (srcImage.Width != dstImage.Width || srcImage.Height != dstImage.Height)
            {
                throw new System.Exception("Source and destination images must be the same size");
            }

            srcImage.CopyTo(rect, dstImage, rect, null);
        }

        /// <summary>
        /// Copies a source image into a destination image using the specified masking image.
        /// Only pixels in the source image whose corresponding mask image pixels are > 0
        /// are copied to the destination image. Only pixels from the srcRect are copied
        /// to the destination rect.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="srcRect">Source rectangle to copy from.</param>
        /// <param name="dstImage">Destination image.</param>
        /// <param name="dstRect">Destunatuin rectangle to copy to.</param>
        public static void CopyTo(this Image srcImage, Rectangle srcRect, Image dstImage, Rectangle dstRect)
        {
            if (srcImage.Width != dstImage.Width || srcImage.Height != dstImage.Height)
            {
                throw new System.Exception("Source and destination images must be the same size");
            }

            srcImage.CopyTo(srcRect, dstImage, dstRect, null);
        }

        /// <summary>
        /// Copies a source image into a destination image using the specified masking image.
        /// Only pixels in the source image whose corresponding mask image pixels are > 0
        /// are copied to the destination image. Only pixels from the srcRect are copied
        /// to the destination rect.
        /// </summary>
        /// <param name="srcImage">Source image.</param>
        /// <param name="srcRect">Source rectangle to copy from.</param>
        /// <param name="dstImage">Destination image.</param>
        /// <param name="dstRect">Destination rectangle to copy to.</param>
        /// <param name="maskImage">Masking image.</param>
        public static void CopyTo(this Image srcImage, Rectangle srcRect, Image dstImage, Rectangle dstRect, Image maskImage)
        {
            if (srcRect.Width != dstRect.Width || srcRect.Height != dstRect.Height)
            {
                throw new System.Exception("Source and destination rectangles sizes must match");
            }

            if (maskImage != null)
            {
                if (srcImage.Width != maskImage.Width || srcImage.Height != maskImage.Height)
                {
                    throw new System.Exception("Mask image size must match source image size");
                }

                if (maskImage.PixelFormat != PixelFormat.Gray_8bpp)
                {
                    throw new System.Exception("Mask image must be of type PixelFormat.Gray_8bpp");
                }
            }

            PixelFormat srcFormat = srcImage.PixelFormat;
            PixelFormat dstFormat = dstImage.PixelFormat;
            System.IntPtr srcBuffer = srcImage.ImageData;
            System.IntPtr dstBuffer = dstImage.ImageData;
            System.IntPtr maskBuffer = (maskImage != null) ? maskImage.ImageData : System.IntPtr.Zero;
            unsafe
            {
                int srcBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(srcFormat);
                int dstBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(dstFormat);
                int maskBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(PixelFormat.Gray_8bpp);
                byte* srcRow = (byte*)srcBuffer.ToPointer() + (srcRect.Y * srcImage.Stride) + (srcRect.X * srcBytesPerPixel);
                byte* dstRow = (byte*)dstBuffer.ToPointer() + (dstRect.Y * dstImage.Stride) + (dstRect.X * dstBytesPerPixel);
                byte* maskRow = null;
                if (maskImage != null)
                {
                    maskRow = (byte*)maskBuffer.ToPointer() + (srcRect.Y * maskImage.Stride) + (srcRect.X * maskBytesPerPixel);
                }

                for (int i = 0; i < srcRect.Height; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    byte* maskCol = maskRow;
                    for (int j = 0; j < srcRect.Width; j++)
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

                                case PixelFormat.RGBA_64bpp:
                                    red = ((ushort*)srcCol)[0];
                                    green = ((ushort*)srcCol)[1];
                                    blue = ((ushort*)srcCol)[2];
                                    alpha = ((ushort*)srcCol)[3];
                                    break;
                            }

                            switch (dstFormat)
                            {
                                case PixelFormat.Gray_8bpp:
                                    dstCol[0] = Image.Rgb2Gray((byte)red, (byte)green, (byte)blue);
                                    break;

                                case PixelFormat.Gray_16bpp:
                                    ((ushort*)dstCol)[0] = Image.Rgb2Gray((ushort)red, (ushort)green, (ushort)blue);
                                    break;

                                case PixelFormat.BGR_24bpp:
                                case PixelFormat.BGRX_32bpp:
                                    dstCol[0] = (byte)blue;
                                    dstCol[1] = (byte)green;
                                    dstCol[2] = (byte)red;
                                    break;

                                case PixelFormat.BGRA_32bpp:
                                    dstCol[0] = (byte)blue;
                                    dstCol[1] = (byte)green;
                                    dstCol[2] = (byte)red;
                                    dstCol[3] = (byte)alpha;
                                    break;

                                case PixelFormat.RGBA_64bpp:
                                    ((ushort*)dstCol)[0] = (ushort)red;
                                    ((ushort*)dstCol)[1] = (ushort)green;
                                    ((ushort*)dstCol)[2] = (ushort)blue;
                                    ((ushort*)dstCol)[3] = (ushort)alpha;
                                    break;
                            }
                        }

                        srcCol += srcBytesPerPixel;
                        dstCol += dstBytesPerPixel;
                        maskCol += maskBytesPerPixel;
                    }

                    srcRow += srcImage.Stride;
                    dstRow += dstImage.Stride;
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
        /// Inverts an image.
        /// </summary>
        /// <param name="image">Image to invert.</param>
        /// <returns>Returns the inverted image.</returns>
        public static Shared<Image> Invert(this Image image)
        {
            Shared<Image> dstImage = ImagePool.GetOrCreate(image.Width, image.Height, image.PixelFormat);
            unsafe
            {
                int srcBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(image.PixelFormat);
                int dstBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(dstImage.Resource.PixelFormat);
                byte* srcRow = (byte*)image.ImageData.ToPointer();
                byte* dstRow = (byte*)dstImage.Resource.ImageData.ToPointer();
                for (int i = 0; i < image.Height; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    for (int j = 0; j < image.Width; j++)
                    {
                        switch (image.PixelFormat)
                        {
                            case PixelFormat.Gray_8bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                break;

                            case PixelFormat.Gray_16bpp:
                                ((ushort*)dstCol)[0] = (byte)(65535 - srcCol[0]);
                                break;

                            case PixelFormat.BGR_24bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                dstCol[1] = (byte)(255 - srcCol[1]);
                                dstCol[2] = (byte)(255 - srcCol[2]);
                                break;

                            case PixelFormat.BGRX_32bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                dstCol[1] = (byte)(255 - srcCol[1]);
                                dstCol[2] = (byte)(255 - srcCol[2]);
                                break;

                            case PixelFormat.BGRA_32bpp:
                                dstCol[0] = (byte)(255 - srcCol[0]);
                                dstCol[1] = (byte)(255 - srcCol[1]);
                                dstCol[2] = (byte)(255 - srcCol[2]);
                                dstCol[3] = (byte)(255 - srcCol[3]);
                                break;
                        }

                        srcCol += srcBytesPerPixel;
                        dstCol += dstBytesPerPixel;
                    }

                    srcRow += image.Stride;
                    dstRow += dstImage.Resource.Stride;
                }
            }

            return dstImage;
        }

        /// <summary>
        /// Clears an image.
        /// </summary>
        /// <param name="image">Image to clear.</param>
        /// <param name="clr">Color to clear to.</param>
        public static void Clear(this Image image, Color clr)
        {
            unsafe
            {
                int srcBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(image.PixelFormat);
                byte* srcRow = (byte*)image.ImageData.ToPointer();
                for (int i = 0; i < image.Height; i++)
                {
                    byte* srcCol = srcRow;
                    for (int j = 0; j < image.Width; j++)
                    {
                        switch (image.PixelFormat)
                        {
                            case PixelFormat.Gray_8bpp:
                                srcCol[0] = Image.Rgb2Gray(clr.R, clr.G, clr.B);
                                break;

                            case PixelFormat.Gray_16bpp:
                                ((ushort*)srcCol)[0] = Image.Rgb2Gray((ushort)clr.R, (ushort)clr.G, (ushort)clr.B);
                                break;

                            case PixelFormat.BGR_24bpp:
                                srcCol[2] = clr.R;
                                srcCol[1] = clr.G;
                                srcCol[0] = clr.B;
                                break;

                            case PixelFormat.BGRX_32bpp:
                                srcCol[2] = clr.R;
                                srcCol[1] = clr.G;
                                srcCol[0] = clr.B;
                                break;

                            case PixelFormat.BGRA_32bpp:
                                srcCol[3] = clr.R;
                                srcCol[2] = clr.G;
                                srcCol[1] = clr.B;
                                srcCol[0] = clr.A;
                                break;
                        }

                        srcCol += srcBytesPerPixel;
                    }

                    srcRow += image.Stride;
                }
            }
        }

        /// <summary>
        /// Extracts a single channel from the image and returns it as a gray scale image.
        /// </summary>
        /// <param name="image">Image to extract from.</param>
        /// <param name="channel">Index of channel to extract from.</param>
        /// <returns>Returns a new grayscale image containing the color from the specified channel in the original source image.</returns>
        public static Shared<Image> ExtractChannel(this Image image, int channel /* 0=red, 1=green, 2=blue, 3=alpha */)
        {
            if (image.PixelFormat != PixelFormat.BGRA_32bpp &&
                image.PixelFormat != PixelFormat.BGRX_32bpp &&
                image.PixelFormat != PixelFormat.BGR_24bpp)
            {
                throw new System.Exception("Extract only supports the following pixel formats: BGRA_32bpp, BGRX_32bpp, and BGR_24bpp");
            }

            if (channel < 0 ||
                (image.PixelFormat != PixelFormat.BGR_24bpp && channel > 3) ||
                (image.PixelFormat == PixelFormat.BGR_24bpp && channel > 2))
            {
                throw new System.Exception("Unsupported channel");
            }

            Shared<Image> dstImage = ImagePool.GetOrCreate(image.Width, image.Height, PixelFormat.Gray_8bpp);
            unsafe
            {
                int srcBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(image.PixelFormat);
                int dstBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(PixelFormat.Gray_8bpp);
                byte* srcRow = (byte*)image.ImageData.ToPointer();
                byte* dstRow = (byte*)dstImage.Resource.ImageData.ToPointer();
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
                    dstRow += dstImage.Resource.Stride;
                }
            }

            return dstImage;
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
        public static Shared<Image> Threshold(this Image image, int threshold, int maxvalue, Threshold type)
        {
            Shared<Image> dstImage = ImagePool.GetOrCreate(image.Width, image.Height, image.PixelFormat);

            unsafe
            {
                int bytesPerPixel = PixelFormatHelper.GetBytesPerPixel(image.PixelFormat);
                byte* srcRow = (byte*)image.ImageData.ToPointer();
                byte* dstRow = (byte*)dstImage.Resource.ImageData.ToPointer();
                for (int i = 0; i < image.Height; i++)
                {
                    byte* srcCol = srcRow;
                    byte* dstCol = dstRow;
                    for (int j = 0; j < image.Width; j++)
                    {
                        int r = 0, g = 0, b = 0, a = 0;
                        switch (image.PixelFormat)
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

                            case PixelFormat.RGBA_64bpp:
                                r = ((ushort*)srcCol)[0];
                                g = ((ushort*)srcCol)[1];
                                b = ((ushort*)srcCol)[2];
                                a = ((ushort*)srcCol)[3];
                                break;

                            default:
                                break;
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
                        }

                        switch (image.PixelFormat)
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
                                break;

                            case PixelFormat.BGR_24bpp:
                                dstCol[0] = (byte)b;
                                dstCol[1] = (byte)g;
                                dstCol[2] = (byte)r;
                                break;

                            case PixelFormat.Gray_16bpp:
                                ((ushort*)srcCol)[0] = (ushort)r;
                                break;

                            case PixelFormat.Gray_8bpp:
                                srcCol[0] = (byte)r;
                                break;

                            case PixelFormat.RGBA_64bpp:
                                ((ushort*)srcCol)[0] = (ushort)r;
                                ((ushort*)srcCol)[1] = (ushort)g;
                                ((ushort*)srcCol)[2] = (ushort)b;
                                ((ushort*)srcCol)[3] = (ushort)a;
                                break;

                            default:
                                break;
                        }

                        srcCol += bytesPerPixel;
                        dstCol += bytesPerPixel;
                    }

                    srcRow += image.Stride;
                    dstRow += dstImage.Resource.Stride;
                }
            }

            return dstImage;
        }

        /// <summary>
        /// Computes the absolute difference between two images.
        /// </summary>
        /// <param name="imageA">First image.</param>
        /// <param name="imageB">Second image.</param>
        /// <returns>Difference image.</returns>
        public static Shared<Image> AbsDiff(this Image imageA, Image imageB)
        {
            if (imageA.Width != imageB.Width || imageA.Height != imageB.Height || imageA.PixelFormat != imageB.PixelFormat)
            {
                throw new System.Exception("Images sizes/types don't match");
            }

            Shared<Image> dstImage = ImagePool.GetOrCreate(imageA.Width, imageA.Height, imageA.PixelFormat);

            unsafe
            {
                int bytesPerPixel = PixelFormatHelper.GetBytesPerPixel(imageA.PixelFormat);
                byte* srcRowA = (byte*)imageA.ImageData.ToPointer();
                byte* srcRowB = (byte*)imageB.ImageData.ToPointer();
                byte* dstRow = (byte*)dstImage.Resource.ImageData.ToPointer();
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
                                break;

                            case PixelFormat.BGR_24bpp:
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

                            default:
                                throw new System.Exception("Unexpected image format");
                        }

                        srcColA += bytesPerPixel;
                        srcColB += bytesPerPixel;
                        dstCol += bytesPerPixel;
                    }

                    srcRowA += imageA.Stride;
                    srcRowB += imageB.Stride;
                    dstRow += dstImage.Resource.Stride;
                }
            }

            return dstImage;
        }
    }
}