// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1310 // FieldNamesMustNotContainUnderscore

namespace Test.Psi.Imaging
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class ImageTester
    {
        private readonly Image testImage_Gray = Image.FromBitmap(Properties.Resources.TestImage_Gray);
        private readonly Image testImage_GrayDrawCircle = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawCircle);
        private readonly Image testImage_GrayDrawLine = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawLine);
        private readonly Image testImage_GrayDrawRect = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawRect);
        private readonly Image testImage_GrayDrawText = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawText);
        private readonly Image testImage_GrayFillRect = Image.FromBitmap(Properties.Resources.TestImage_GrayFillRect);
        private readonly Image testImage_GrayFillCircle = Image.FromBitmap(Properties.Resources.TestImage_GrayFillCircle);
        private readonly Image testImage_GrayDrawTextWithBackground = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawTextWithBackground);
        private readonly Image testImage_GrayFlip = Image.FromBitmap(Properties.Resources.TestImage_GrayFlip);
        private readonly Image testImage_GrayResized = Image.FromBitmap(Properties.Resources.TestImage_GrayResized);
        private readonly Image testImage_GrayRotate = Image.FromBitmap(Properties.Resources.TestImage_GrayRotate);
        private readonly Image testImage_GraySetPixel = Image.FromBitmap(Properties.Resources.TestImage_GraySetPixel);
        private readonly Image testImage_SetPixel = Image.FromBitmap(Properties.Resources.TestImage_SetPixel);
        private readonly Image testImage = Image.FromBitmap(Properties.Resources.TestImage);
        private readonly Image testImage2 = Image.FromBitmap(Properties.Resources.TestImage2);
        private readonly Image testImage2_Threshold = Image.FromBitmap(Properties.Resources.TestImage2_Threshold);
        private readonly Image testImage2_RedChannel = Image.FromBitmap(Properties.Resources.TestImage2_RedChannel);
        private readonly Image testImage2_GreenChannel = Image.FromBitmap(Properties.Resources.TestImage2_GreenChannel);
        private readonly Image testImage2_BlueChannel = Image.FromBitmap(Properties.Resources.TestImage2_BlueChannel);
        private readonly Image testImage2_CopyImage = Image.FromBitmap(Properties.Resources.TestImage2_CopyImage);
        private readonly Image testImage2_Invert = Image.FromBitmap(Properties.Resources.TestImage2_Invert);
        private readonly Image testImage2_Mask = Image.FromBitmap(Properties.Resources.TestImage2_Mask);
        private readonly Image testImage2_FlipHoriz = Image.FromBitmap(Properties.Resources.TestImage2_FlipHoriz);
        private readonly Image testImage2_FlipVert = Image.FromBitmap(Properties.Resources.TestImage2_FlipVert);
        private readonly Image testImage2_Rotate_Neg10 = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_Neg10);
        private readonly Image testImage2_Rotate_Neg10_Loose = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_Neg10_Loose);
        private readonly Image testImage2_Rotate_110 = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_110);
        private readonly Image testImage2_Rotate_110_Loose = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_110_Loose);
        private readonly Image testImage2_DrawRect = Image.FromBitmap(Properties.Resources.TestImage2_DrawRect);
        private readonly Image testImage2_DrawLine = Image.FromBitmap(Properties.Resources.TestImage2_DrawLine);
        private readonly Image testImage2_DrawCircle = Image.FromBitmap(Properties.Resources.TestImage2_DrawCircle);
        private readonly Image testImage2_DrawText = Image.FromBitmap(Properties.Resources.TestImage2_DrawText);
        private readonly Image testImage2_FillRect = Image.FromBitmap(Properties.Resources.TestImage2_FillRect);
        private readonly Image testImage2_FillCircle = Image.FromBitmap(Properties.Resources.TestImage2_FillCircle);
        private readonly Image testImage2_DrawTextWithBackground = Image.FromBitmap(Properties.Resources.TestImage2_DrawTextWithBackground);
        private readonly Image testImage2_AbsDiff = Image.FromBitmap(Properties.Resources.TestImage2_AbsDiff);
        private readonly Image testImage_0_0_200_100 = Image.FromBitmap(Properties.Resources.TestImage_Crop_0_0_200_100);
        private readonly Image testImage_153_57_103_199 = Image.FromBitmap(Properties.Resources.TestImage_Crop_153_57_103_199);
        private readonly Image testImage_73_41_59_37 = Image.FromBitmap(Properties.Resources.TestImage_Crop_73_41_59_37);
        private readonly Image testImage_50_25_Cubic = Image.FromBitmap(Properties.Resources.TestImage_Scale_50_25_Cubic);
        private readonly Image testImage_150_125_Point = Image.FromBitmap(Properties.Resources.TestImage_Scale_150_125_Point);
        private readonly Image testImage_25_200_Linear = Image.FromBitmap(Properties.Resources.TestImage_Scale_25_200_Linear);
        private readonly Image solidColorsImage = Image.FromBitmap(Properties.Resources.SolidColors);

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawCircle()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawCircle(new System.Drawing.Point(0, 0), 100, System.Drawing.Color.Red, 3);
            this.AssertAreImagesEqual(this.testImage_GrayDrawCircle, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawLine()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawLine(new System.Drawing.Point(0, 0), new System.Drawing.Point(100, 100), System.Drawing.Color.Red, 3);
            this.AssertAreImagesEqual(this.testImage_GrayDrawLine, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawRect()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawRectangle(new System.Drawing.Rectangle(0, 0, 20, 20), System.Drawing.Color.White, 3);
            this.AssertAreImagesEqual(this.testImage_GrayDrawRect, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawText()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawText("Test", new System.Drawing.Point(0, 20), System.Drawing.Color.Red);
            this.AssertAreImagesEqual(this.testImage_GrayDrawText, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawTextWithBackground()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawText("Test", new System.Drawing.Point(0, 20), System.Drawing.Color.Red, System.Drawing.Color.White);
            this.AssertAreImagesEqual(this.testImage_GrayDrawTextWithBackground, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayFillRect()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.FillRectangle(new System.Drawing.Rectangle(0, 0, 20, 20), System.Drawing.Color.White);
            this.AssertAreImagesEqual(this.testImage_GrayFillRect, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayFillCircle()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.FillCircle(new System.Drawing.Point(this.testImage_Gray.Width / 2, this.testImage_Gray.Height / 2), 100, System.Drawing.Color.White);
            this.AssertAreImagesEqual(this.testImage_GrayFillCircle, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayFlip()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.Flip(sharedImage.Resource, FlipMode.AlongHorizontalAxis);
            this.AssertAreImagesEqual(this.testImage_GrayFlip, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayResize()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            this.testImage_Gray.Resize(100, 100, SamplingMode.Bilinear);
            this.AssertAreImagesEqual(this.testImage_GrayResized, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayRotate()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.Rotate(20.0f, SamplingMode.Bilinear);
            this.AssertAreImagesEqual(this.testImage_GrayRotate, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp)]
        public void Image_GraySetPixel(PixelFormat pixelFormat)
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_GraySetPixel.Width, this.testImage_GraySetPixel.Height, pixelFormat);
            using var refImage = this.testImage_GraySetPixel.Convert(pixelFormat);

            // The documentation for SetPixel is as follows:
            /// <summary>
            /// Sets a pixel in the image.
            /// </summary>
            /// <param name="x">Pixel's X coordinate.</param>
            /// <param name="y">Pixel's Y coordinate.</param>
            /// <param name="gray">Gray value to set pixel to.</param>

            int shiftBits = pixelFormat.GetBitsPerChannel() - 8;
            int maxValue = (1 << pixelFormat.GetBitsPerChannel()) - 1;

            for (int x = 0; x <= 255; x++)
            {
                for (int y = 0; y <= 255; y++)
                {
                    int gray = (x << shiftBits) | x;
                    sharedImage.Resource.SetPixel(x, y, gray);
                }
            }

            this.AssertAreImagesEqual(refImage, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        public void Image_SetPixel(PixelFormat pixelFormat)
        {
            using var destImage = new Image(this.testImage_SetPixel.Width, this.testImage_SetPixel.Height, pixelFormat);
            using var refImage = this.testImage_SetPixel.Convert(pixelFormat);

            // The documentation for SetPixel is as follows:
            /// <summary>
            /// Sets a pixel in the image.
            /// </summary>
            /// <param name="x">Pixel's X coordinate.</param>
            /// <param name="y">Pixel's Y coordinate.</param>
            /// <param name="r">Red channel's value.</param>
            /// <param name="g">Green channel's value.</param>
            /// <param name="b">Blue channel's value.</param>
            /// <param name="a">Alpha channel's value.</param>

            int bitDepth = pixelFormat.GetBitsPerChannel();
            int maxPixelValue = (1 << bitDepth) - 1;

            for (int x = 0; x <= 255; x++)
            {
                for (int y = 0; y <= 255; y++)
                {
                    int r = x;
                    int b = y;

                    // scale values to match bit depth if necessary
                    if (bitDepth == 16)
                    {
                        r = (r << 8) | r;
                        b = (b << 8) | b;
                    }

                    int g = maxPixelValue - Math.Max(r, b);
                    int a = maxPixelValue;
                    destImage.SetPixel(x, y, r, g, b, a);
                }
            }

            this.AssertAreImagesEqual(refImage, destImage);
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp)]
        public void Image_ReadBytes(PixelFormat pixelFormat)
        {
            using var destImage = new Image(this.solidColorsImage.Width, this.solidColorsImage.Height, pixelFormat);
            this.solidColorsImage.CopyTo(destImage);

            (int x, int y, int r, int g, int b)[] expected = new[]
            {
                (000, 000, 000, 000, 000),
                (100, 100, 255, 000, 000),
                (200, 200, 000, 255, 000),
                (300, 100, 000, 000, 255),
                (100, 200, 255, 255, 255),
            };

            var bytesPerPixel = destImage.BitsPerPixel / 8;
            foreach (var (x, y, r, g, b) in expected)
            {
                var bytes = destImage.ReadBytes(bytesPerPixel, x * bytesPerPixel + y * destImage.Stride);
                switch (pixelFormat)
                {
                    case PixelFormat.BGR_24bpp:
                    case PixelFormat.BGRX_32bpp:
                    case PixelFormat.BGRA_32bpp:
                        Assert.AreEqual(bytes[0], b, $"First byte at ({x}, {y}) in {pixelFormat} should be the Blue channel.");
                        Assert.AreEqual(bytes[1], g, $"Second byte at ({x}, {y}) in {pixelFormat} should be the Green channel.");
                        Assert.AreEqual(bytes[2], r, $"Third byte at ({x}, {y}) in {pixelFormat} should be the Red channel.");
                        break;

                    case PixelFormat.RGB_24bpp:
                        Assert.AreEqual(bytes[0], r, $"First byte at ({x}, {y}) in {pixelFormat} should be the Red channel.");
                        Assert.AreEqual(bytes[1], g, $"Second byte at ({x}, {y}) in {pixelFormat} should be the Green channel.");
                        Assert.AreEqual(bytes[2], b, $"Third byte at ({x}, {y}) in {pixelFormat} should be the Blue channel.");
                        break;

                    case PixelFormat.RGBA_64bpp:
                        Assert.AreEqual(bytes[0], r, $"First byte at ({x}, {y}) in {pixelFormat} should be the Red channel.");
                        Assert.AreEqual(bytes[1], r, $"Second byte at ({x}, {y}) in {pixelFormat} should be the Red channel.");
                        Assert.AreEqual(bytes[2], g, $"Third byte at ({x}, {y}) in {pixelFormat} should be the Green channel.");
                        Assert.AreEqual(bytes[3], g, $"Fourth byte at ({x}, {y}) in {pixelFormat} should be the Green channel.");
                        Assert.AreEqual(bytes[4], b, $"Fifth byte at ({x}, {y}) in {pixelFormat} should be the Blue channel.");
                        Assert.AreEqual(bytes[5], b, $"Sixth byte at ({x}, {y}) in {pixelFormat} should be the Blue channel.");
                        break;

                    case PixelFormat.Gray_8bpp:
                        var gray8 = (byte)(((4897 * r) + (9617 * g) + (1868 * b)) >> 14);
                        Assert.AreEqual(bytes[0], gray8, $"First byte at ({x}, {y}) in {pixelFormat} should be the appropriate grayscale.");
                        break;

                    case PixelFormat.Gray_16bpp:
                        var gray16 = (ushort)(((4897 * (r * ushort.MaxValue / byte.MaxValue)) + (9617 * (g * ushort.MaxValue / byte.MaxValue)) + (1868 * (b * ushort.MaxValue / byte.MaxValue))) >> 14);
                        Assert.AreEqual(BitConverter.ToUInt16(bytes, 0), gray16, $"First ushort at ({x}, {y}) in {pixelFormat} should be the appropriate grayscale.");
                        break;

                    default:
                        Assert.Inconclusive($"No test for pixel format {pixelFormat}.");
                        break;
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.Gray_16bpp)]

        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.BGRA_32bpp)]

        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.RGBA_64bpp)]

        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.Gray_16bpp)]

        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.BGRA_32bpp)]

        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.RGBA_64bpp)]
        public void Image_CopyImage(PixelFormat srcFormat, PixelFormat dstFormat)
        {
            var random = new Random();
            var randomColor = System.Drawing.Color.FromArgb(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256));
            using var srcImage = new Image(1, 1, srcFormat);
            srcImage.Clear(randomColor);

            using var dstImage = new Image(1, 1, dstFormat);
            srcImage.CopyTo(dstImage);

            var srcPixel = srcImage.GetPixel(0, 0);
            var dstPixel = dstImage.GetPixel(0, 0);

            int srcDepth = srcFormat.GetBitsPerChannel();
            int dstDepth = dstFormat.GetBitsPerChannel();

            // When the target and source bit depths differ, adjust the source bit depth to match the target.
            if (srcDepth == 8 && dstDepth == 16)
            {
                srcPixel = ((srcPixel.r << 8) | srcPixel.r, (srcPixel.g << 8) | srcPixel.g, (srcPixel.b << 8) | srcPixel.b, (srcPixel.a << 8) | srcPixel.a);
            }
            else if (srcDepth == 16 && dstDepth == 8)
            {
                srcPixel = (srcPixel.r >> 8, srcPixel.g >> 8, srcPixel.b >> 8, srcPixel.a >> 8);
            }

            // When converting from a color format to gray, apply the Rgb2Gray operator to the
            // source pixel value first (using the appropriate operator for the target bit depth).
            if (dstFormat == PixelFormat.Gray_8bpp &&
                srcFormat != PixelFormat.Gray_8bpp &&
                srcFormat != PixelFormat.Gray_16bpp)
            {
                var gray = Microsoft.Psi.Imaging.Operators.Rgb2Gray((byte)srcPixel.r, (byte)srcPixel.g, (byte)srcPixel.b);
                srcPixel = (gray, gray, gray, 255);
            }
            else if (dstFormat == PixelFormat.Gray_16bpp &&
                srcFormat != PixelFormat.Gray_8bpp &&
                srcFormat != PixelFormat.Gray_16bpp)
            {
                var gray = Microsoft.Psi.Imaging.Operators.Rgb2Gray((ushort)srcPixel.r, (ushort)srcPixel.g, (ushort)srcPixel.b);
                srcPixel = (gray, gray, gray, 65535);
            }

            if (dstPixel != srcPixel)
            {
                var srcSampleBytesForErrorMessage = srcImage.ReadBytes(srcImage.BitsPerPixel / 8);
                var dstSampleBytesForErrorMessage = dstImage.ReadBytes(dstImage.BitsPerPixel / 8);

                Assert.AreEqual(srcPixel.r, dstPixel.r, 1, $"Mismatch in copied red color from {srcFormat} [{string.Join(", ", srcSampleBytesForErrorMessage)}] to {dstFormat} [{string.Join(", ", dstSampleBytesForErrorMessage)}]");
                Assert.AreEqual(srcPixel.g, dstPixel.g, 1, $"Mismatch in copied green color from {srcFormat} [{string.Join(", ", srcSampleBytesForErrorMessage)}] to {dstFormat} [{string.Join(", ", dstSampleBytesForErrorMessage)}]");
                Assert.AreEqual(srcPixel.b, dstPixel.b, 1, $"Mismatch in copied blue color from {srcFormat} [{string.Join(", ", srcSampleBytesForErrorMessage)}] to {dstFormat} [{string.Join(", ", dstSampleBytesForErrorMessage)}]");
                Assert.AreEqual(srcPixel.a, dstPixel.a, 1, $"Mismatch in copied alpha from {srcFormat} [{string.Join(", ", srcSampleBytesForErrorMessage)}] to {dstFormat} [{string.Join(", ", dstSampleBytesForErrorMessage)}]");
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.Gray_16bpp)]

        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.BGRA_32bpp)]

        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.Gray_8bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.Gray_16bpp, PixelFormat.RGBA_64bpp)]

        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.Gray_16bpp)]

        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.BGRA_32bpp)]

        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.BGR_24bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.BGRX_32bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.BGRA_32bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.RGB_24bpp, PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.RGBA_64bpp, PixelFormat.RGBA_64bpp)]
        public void Image_ConvertViaOperator(PixelFormat srcFormat, PixelFormat dstFormat)
        {
            using var pipeline = Pipeline.Create("ConvertViaOperator");
            using var srcImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, srcFormat);
            this.testImage2.CopyTo(srcImage.Resource);
            Generators.Return(pipeline, srcImage).Convert(dstFormat).Do(dstImage =>
            {
                using var refImage = srcImage.Resource.Convert(dstFormat);
                this.AssertAreImagesEqual(refImage, dstImage.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        public void Image_FlipViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("FlipViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Flip(FlipMode.None).Do((img) =>
            {
                using var refImage = this.testImage2.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Flip(FlipMode.AlongHorizontalAxis).Do((img) =>
            {
                using var refImage = this.testImage2_FlipHoriz.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Flip(FlipMode.AlongVerticalAxis).Do((img) =>
            {
                using var refImage = this.testImage2_FlipVert.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_RotateViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("RotateViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(-10.0f, SamplingMode.Point).Do((img) =>
            {
                using var refImage = this.testImage2_Rotate_Neg10.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(110.0f, SamplingMode.Point).Do((img) =>
            {
                using var refImage = this.testImage2_Rotate_110.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(-10.0f, SamplingMode.Point, RotationFitMode.Loose).Do((img) =>
            {
                using var refImage = this.testImage2_Rotate_Neg10_Loose.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(110.0f, SamplingMode.Point, RotationFitMode.Loose).Do((img) =>
            {
                using var refImage = this.testImage2_Rotate_110_Loose.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_DrawRectangleViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("DrawRectangleViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawRectangle(new System.Drawing.Rectangle(20, 20, 255, 255), System.Drawing.Color.White, 3).Do((img) =>
            {
                using var refImage = this.testImage2_DrawRect.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_DrawLineViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("DrawLineViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawLine(new System.Drawing.Point(0, 0), new System.Drawing.Point(255, 255), System.Drawing.Color.White, 3).Do((img) =>
            {
                using var refImage = this.testImage2_DrawLine.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_DrawCircleViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("DrawCircleViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawCircle(new System.Drawing.Point(250, 250), 100, System.Drawing.Color.White, 3).Do((img) =>
            {
                using var refImage = this.testImage2_DrawCircle.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_DrawTextViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("DrawTextViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawText("Testing", new System.Drawing.Point(100, 100), System.Drawing.Color.White).Do((img) =>
            {
                using var refImage = this.testImage2_DrawText.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_DrawTextWithBackgroundViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("DrawTextWithBackgroundViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawText("Testing", new System.Drawing.Point(100, 100), System.Drawing.Color.Red, System.Drawing.Color.White).Do((img) =>
            {
                using var refImage = this.testImage2_DrawTextWithBackground.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_FillRectangleViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("FillRectangleViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).FillRectangle(new System.Drawing.Rectangle(20, 20, 255, 255), System.Drawing.Color.White).Do((img) =>
            {
                using var refImage = this.testImage2_FillRect.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_FillCircleViaOperator(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("FillCircleViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).FillCircle(new System.Drawing.Point(250, 250), 100, System.Drawing.Color.White).Do((img) =>
            {
                using var refImage = this.testImage2_FillCircle.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        public void Image_CopyRegion(PixelFormat pixelFormat)
        {
            using var destImage = new Image(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            destImage.Clear(System.Drawing.Color.Black);
            var rect = new System.Drawing.Rectangle(50, 300, 100, 255);
            this.testImage2.Convert(pixelFormat).CopyTo(rect, destImage, new System.Drawing.Point(-10, 0), this.testImage2_Mask);
            this.AssertAreImagesEqual(this.testImage2_CopyImage.Convert(pixelFormat), destImage);
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        public void Image_Invert(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("ImageInvert");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Invert().Do((img) =>
            {
                using var refImage = this.testImage2_Invert.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_AbsDiff()
        {
            using var pipeline = Pipeline.Create("ImageAbsDiff");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2_DrawCircle.Width, this.testImage2_DrawCircle.Height, this.testImage2_DrawCircle.PixelFormat);
            this.testImage2_DrawCircle.CopyTo(sharedImage.Resource);
            using var sharedImage2 = ImagePool.GetOrCreate(this.testImage2_DrawRect.Width, this.testImage2_DrawRect.Height, this.testImage2_DrawRect.PixelFormat);
            this.testImage2_DrawRect.CopyTo(sharedImage2.Resource);
            Generators.Sequence(pipeline, new[] { (sharedImage, sharedImage2) }, default, null, keepOpen: false).AbsDiff().Do((img) =>
            {
                this.AssertAreImagesEqual(this.testImage2_AbsDiff, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_Threshold(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("ImageThreshold");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Threshold(10, 170, Threshold.Binary).Do((img) =>
            {
                using var refImage = this.testImage2_Threshold.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        public void Image_ExtractChannels(PixelFormat pixelFormat)
        {
            using var pipeline = Pipeline.Create("ImageExtractChannel");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, pixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            var seq = Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false);
            var bchannel = seq.ExtractChannel(0);
            var gchannel = seq.ExtractChannel(1);
            var rchannel = seq.ExtractChannel(2);
            bchannel.Join(gchannel.Join(rchannel)).Do((imgs) =>
            {
                this.AssertAreImagesEqual(this.testImage2_BlueChannel, imgs.Item1.Resource);
                this.AssertAreImagesEqual(this.testImage2_GreenChannel, imgs.Item2.Resource);
                this.AssertAreImagesEqual(this.testImage2_RedChannel, imgs.Item3.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropViaOperator()
        {
            // Test that the pipeline's operator Crop() works on a stream of images and random rectangles
            using var pipeline = Pipeline.Create("CropViaOperator");
            var generator = Generators.Sequence(pipeline, 1, x => x + 1, 100, TimeSpan.FromTicks(1));
            var p = Microsoft.Psi.Operators.Process<int, (Shared<Image>, System.Drawing.Rectangle)>(
                generator,
                (d, e, s) =>
                {
                    var r = new Random();
                    var rect = default(System.Drawing.Rectangle);
                    rect.X = r.Next() % this.testImage.Width;
                    rect.Y = r.Next() % this.testImage.Height;
                    rect.Width = r.Next() % (this.testImage.Width - rect.X);
                    rect.Height = r.Next() % (this.testImage.Height - rect.Y);
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        using var sharedImage = ImagePool.GetOrCreate(this.testImage.Width, this.testImage.Height, this.testImage.PixelFormat);
                        this.testImage.CopyTo(sharedImage.Resource);
                        s.Post((sharedImage, rect), e.OriginatingTime);
                    }
                }).Crop();
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropViaJoinOperator()
        {
            // Test that the pipeline's operator Crop() works on a stream of images and random rectangles
            using var pipeline = Pipeline.Create("CropViaOperator");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage.Width, this.testImage.Height, this.testImage.PixelFormat);
            this.testImage.CopyTo(sharedImage.Resource);

            // Use a non-insignificant interval for both Sequences to ensure that the Join processes all
            // messages from both streams (default interval of 1-tick is too small to guarantee this).
            var images = Generators.Sequence(pipeline, sharedImage, x => sharedImage, 100, TimeSpan.FromMilliseconds(1));
            var rects = Generators.Sequence(
                pipeline,
                new System.Drawing.Rectangle(0, 0, 1, 1),
                x =>
                    {
                        var r = new Random();
                        var rect = default(System.Drawing.Rectangle);
                        rect.X = r.Next(0, this.testImage.Width);
                        rect.Y = r.Next(0, this.testImage.Height);
                        rect.Width = r.Next(1, this.testImage.Width - rect.X);
                        rect.Height = r.Next(1, this.testImage.Height - rect.Y);

                        return rect;
                    },
                100,
                TimeSpan.FromMilliseconds(1));
            images.Join(rects, Reproducible.Nearest<System.Drawing.Rectangle>()).Crop();
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        public void Image_Crop(PixelFormat pixelFormat)
        {
            // Crop the entire image region (a no-op) and verify that the original image is preserved
            using var sourceImage = this.testImage.Convert(pixelFormat);
            using (var croppedImage = sourceImage.Crop(0, 0, sourceImage.Width, sourceImage.Height))
            {
                using var refImage = this.testImage.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, croppedImage);
            }

            // Crop an upper-left region and verify
            using (var croppedImage = sourceImage.Crop(0, 0, 200, 100))
            {
                using var refImage = this.testImage_0_0_200_100.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, croppedImage);
            }

            // Crop a lower-right region and verify
            using (var croppedImage = sourceImage.Crop(153, 57, 103, 199))
            {
                using var refImage = this.testImage_153_57_103_199.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, croppedImage);
            }

            // Crop an interior region and verify
            using (var croppedImage = sourceImage.Crop(73, 41, 59, 37))
            {
                using var refImage = this.testImage_73_41_59_37.Convert(pixelFormat);
                this.AssertAreImagesEqual(refImage, croppedImage);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropDifferentRegions()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            using var croppedImage = this.testImage.Crop(74, 42, 59, 37);
            var croppedImage_74_42_59_37 = croppedImage;
            CollectionAssert.AreNotEqual(
                this.testImage_73_41_59_37.ReadBytes(this.testImage_73_41_59_37.Size),
                croppedImage_74_42_59_37.ReadBytes(croppedImage_74_42_59_37.Size));
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodeImage()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            var encodedImage = this.testImage.Encode(new ImageToPngStreamEncoder());
            var decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(this.testImage, decodedImage);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodeImageJpg()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            var encodedImage = this.testImage.Encode(new ImageToJpegStreamEncoder());
            var decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            var decodedImage2 = encodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(decodedImage, decodedImage2);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodedImage_Serialize()
        {
            // encode an image with low compression (higher quality)
            var jpegEncoder = new ImageToJpegStreamEncoder() { QualityLevel = 100 };
            jpegEncoder.QualityLevel = 100;
            var encodedImage = this.testImage.Encode(jpegEncoder);

            // serialize the encoded image
            var bw = new BufferWriter(0);
            Serializer.Serialize(bw, encodedImage, new SerializationContext());
            int serializedLengthHq = bw.Position;

            // deserialize the encoded image and verify the data
            EncodedImage targetEncodedImage = null;
            var br = new BufferReader(bw.Buffer);
            Serializer.Deserialize(br, ref targetEncodedImage, new SerializationContext());
            var decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            var targetDecodedImage = targetEncodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(decodedImage, targetDecodedImage);

            // encode an image with high compression (lower quality)
            jpegEncoder = new ImageToJpegStreamEncoder() { QualityLevel = 10 };
            encodedImage = this.testImage.Encode(jpegEncoder);

            // serialize the encoded image
            bw = new BufferWriter(0);
            Serializer.Serialize(bw, encodedImage, new SerializationContext());
            int serializedLengthLq = bw.Position;

            // deserialize the encoded image (into recycled target) and verify the data
            br = new BufferReader(bw.Buffer);
            Serializer.Deserialize(br, ref targetEncodedImage, new SerializationContext());
            decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            targetDecodedImage = targetEncodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(decodedImage, targetDecodedImage);

            // verify serialized length is smaller for the more compressed image
            Assert.IsTrue(serializedLengthLq < serializedLengthHq);
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_Resize(PixelFormat pixelFormat)
        {
            // Resize using bicubic
            this.AssertAreImagesEqual(
                this.testImage_50_25_Cubic.Convert(pixelFormat),
                this.testImage.Convert(pixelFormat).Resize(
                    this.testImage_50_25_Cubic.Width,
                    this.testImage_50_25_Cubic.Height,
                    SamplingMode.Bicubic));

            // Resize using nearest-neighbor
            this.AssertAreImagesEqual(
                this.testImage_150_125_Point.Convert(pixelFormat),
                this.testImage.Convert(pixelFormat).Resize(
                    this.testImage_150_125_Point.Width,
                    this.testImage_150_125_Point.Height,
                    SamplingMode.Point));

            // Resize using bilinear
            this.AssertAreImagesEqual(
                this.testImage_25_200_Linear.Convert(pixelFormat),
                this.testImage.Convert(pixelFormat).Resize(
                    this.testImage_25_200_Linear.Width,
                    this.testImage_25_200_Linear.Height,
                    SamplingMode.Bilinear));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Compare()
        {
            this.AssertAreImagesEqual(this.testImage, this.testImage);
            this.AssertAreImagesEqual(this.testImage_Gray, this.testImage_Gray);
            var err = new ImageError();
            Assert.IsFalse(this.testImage2.Compare(this.testImage2_DrawRect, 2.0, 0.01, ref err));
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        public void Image_Scale(PixelFormat pixelFormat)
        {
            // Scale using bicubic
            this.AssertAreImagesEqual(
                this.testImage_50_25_Cubic.Convert(pixelFormat),
                this.testImage.Convert(pixelFormat).Scale(
                    (float)this.testImage_50_25_Cubic.Width / (float)this.testImage.Width,
                    (float)this.testImage_50_25_Cubic.Height / (float)this.testImage.Height,
                    SamplingMode.Bicubic));

            // Scale using nearest-neighbor
            this.AssertAreImagesEqual(
                this.testImage_150_125_Point.Convert(pixelFormat),
                this.testImage.Convert(pixelFormat).Scale(
                    (float)this.testImage_150_125_Point.Width / (float)this.testImage.Width,
                    (float)this.testImage_150_125_Point.Height / (float)this.testImage.Height,
                    SamplingMode.Point));

            // Scale using bilinear
            this.AssertAreImagesEqual(
                this.testImage_25_200_Linear.Convert(pixelFormat),
                this.testImage.Convert(pixelFormat).Scale(
                    (float)this.testImage_25_200_Linear.Width / (float)this.testImage.Width,
                    (float)this.testImage_25_200_Linear.Height / (float)this.testImage.Height,
                    SamplingMode.Bilinear));

            // Attempt to scale 16bpp grayscale - should throw NotSupportedException
            var depthImage = new Image(100, 100, PixelFormat.Gray_16bpp);
            Assert.ThrowsException<InvalidOperationException>(() => depthImage.Scale(0.5f, 0.5f, SamplingMode.Point));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Serialize()
        {
            // create the serialization context
            var knownSerializers = new KnownSerializers();
            var context = new SerializationContext(knownSerializers);

            // serialize the image
            var writer = new BufferWriter(this.testImage.Size);
            Serializer.Serialize(writer, this.testImage, context);

            // verify the image type schema
            string contract = TypeSchema.GetContractName(typeof(Image), knownSerializers.RuntimeInfo.SerializationSystemVersion);
            Assert.IsTrue(knownSerializers.Schemas.ContainsKey(contract));

            // deserialize the image and verify the data
            Image targetImage = null;
            var reader = new BufferReader(writer.Buffer);
            Serializer.Deserialize(reader, ref targetImage, context);
            this.AssertAreImagesEqual(this.testImage, targetImage);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DepthImage_Serialize()
        {
            // generate a "depth image" for testing
            var testImage16bpp = new Image(this.testImage.Width, this.testImage.Height, PixelFormat.Gray_16bpp);
            this.testImage.CopyTo(testImage16bpp);
            var testDepthImage = DepthImage.CreateFrom(testImage16bpp.ToBitmap());

            // create the serialization context
            var knownSerializers = new KnownSerializers();
            var context = new SerializationContext(knownSerializers);

            // serialize the image
            var writer = new BufferWriter(testDepthImage.Size);
            Serializer.Serialize(writer, testDepthImage, context);

            // verify the image type schema
            string contract = TypeSchema.GetContractName(typeof(DepthImage), knownSerializers.RuntimeInfo.SerializationSystemVersion);
            Assert.IsTrue(knownSerializers.Schemas.ContainsKey(contract));

            // deserialize the image and verify the data
            DepthImage targetDepthImage = null;
            var reader = new BufferReader(writer.Buffer);
            Serializer.Deserialize(reader, ref targetDepthImage, context);
            this.AssertAreImagesEqual(testDepthImage, targetDepthImage);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodeDepthImageAsTiff()
        {
            // generate a "depth image" for testing
            var testImage16bpp = new Image(this.testImage.Width, this.testImage.Height, PixelFormat.Gray_16bpp);
            this.testImage.CopyTo(testImage16bpp);
            var testDepthImage = DepthImage.CreateFrom(testImage16bpp.ToBitmap());

            // encode to TIFF and decode
            var encodedDepthTiffImage = testDepthImage.Encode(new DepthImageToTiffStreamEncoder());
            var decodedDepthImage = encodedDepthTiffImage.Decode(new DepthImageFromStreamDecoder());
            this.AssertAreImagesEqual(testDepthImage, decodedDepthImage);
        }

        [TestMethod]
        [Timeout(60000)]
        [DataRow(PixelFormat.BGRA_32bpp)]
        [DataRow(PixelFormat.BGRX_32bpp)]
        [DataRow(PixelFormat.BGR_24bpp)]
        [DataRow(PixelFormat.RGB_24bpp)]
        [DataRow(PixelFormat.RGBA_64bpp)]
        [DataRow(PixelFormat.Gray_8bpp)]
        [DataRow(PixelFormat.Gray_16bpp)]
        public void Image_SaveAndLoad(PixelFormat pixelFormat)
        {
            string filename = $"TestImage_{pixelFormat}.bmp";

            // Create a test image in the specified pixel format
            using var sourceImage = this.testImage.Convert(pixelFormat);

            try
            {
                if (pixelFormat == PixelFormat.Gray_16bpp || pixelFormat == PixelFormat.RGBA_64bpp)
                {
                    // Gray_16bpp and RGBA_64bpp are not supported for file operations
                    Assert.ThrowsException<NotSupportedException>(() => sourceImage.Save(filename));
                }
                else
                {
                    // Save the image to a file
                    sourceImage.Save(filename);

                    // Load the image from file and compare
                    using var testImage = Image.FromFile(filename);
                    if (pixelFormat == PixelFormat.RGB_24bpp)
                    {
                        // RGB_24bpp images are converted to BGR_24bpp before saving
                        this.AssertAreImagesEqual(sourceImage.Convert(PixelFormat.BGR_24bpp), testImage);
                    }
                    else if (pixelFormat == PixelFormat.BGRX_32bpp)
                    {
                        // BGRX_32bpp images are converted to BGRA_32bpp before saving
                        this.AssertAreImagesEqual(sourceImage.Convert(PixelFormat.BGRA_32bpp), testImage);
                    }
                    else
                    {
                        this.AssertAreImagesEqual(sourceImage, testImage);
                    }
                }
            }
            finally
            {
                TestRunner.SafeFileDelete(filename);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SharedImagePoolCollisionTest()
        {
            var bmp57 = new System.Drawing.Bitmap(5, 7);
            var bmp75 = new System.Drawing.Bitmap(7, 5);

            Assert.AreEqual<int>(5, bmp57.Width);
            Assert.AreEqual<int>(7, bmp57.Height);
            Assert.AreEqual<int>(7, bmp75.Width);
            Assert.AreEqual<int>(5, bmp75.Height);

            var shared57 = ImagePool.GetOrCreateFromBitmap(bmp57);
            Assert.AreEqual<int>(5, shared57.Resource.Width);
            Assert.AreEqual<int>(7, shared57.Resource.Height);

            // Ensure that the ImagePool is not recycling images based solely on the product of
            // width*height (i.e. the same number of pixels but different dimensions), as the
            // stride and total size of the recycled image could be incorrect.

            shared57.Dispose(); // release to be recycled
            var shared75 = ImagePool.GetOrCreateFromBitmap(bmp75); // should *not* get the recycled image
            Assert.AreEqual<int>(7, shared75.Resource.Width);
            Assert.AreEqual<int>(5, shared75.Resource.Height);
        }

        private void AssertAreImagesEqual(ImageBase referenceImage, ImageBase subjectImage, double tolerance = 6.0, double percentOutliersAllowed = 0.01)
        {
            var err = new ImageError();
            Assert.AreEqual(referenceImage.Stride, subjectImage.Stride); // also check for consistency in the strides of allocated Images
            Assert.IsTrue(
                referenceImage.Compare(subjectImage, tolerance, percentOutliersAllowed, ref err),
                $"Max err: {err.MaxError}, Outliers: {err.NumberOutliers}");
        }
    }
}

#pragma warning restore SA1310 // FieldNamesMustNotContainUnderscore
