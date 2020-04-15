// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Calibration
{
    using System;
    using System.IO;
    using System.Net.Http.Headers;
    using System.Runtime.InteropServices;
    using MathNet.Numerics.LinearAlgebra;
    using Microsoft.Psi.Calibration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Distortion tests.
    /// </summary>
    [TestClass]
    public class DistortionTests
    {
#if DUMP_IMAGES
        public static void SaveColorToBMP(string filePath, Microsoft.Psi.Imaging.Image rgbImage)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BitmapFileHeader header = default(BitmapFileHeader);
                BitmapInfoHeader infoheader = default(BitmapInfoHeader);
                header.Type = 0x4d42;
                int headerSize = Marshal.SizeOf(header);
                int infoSize = Marshal.SizeOf(infoheader);
                header.OffBits = Marshal.SizeOf(header) + Marshal.SizeOf(infoheader);
                header.Size = header.OffBits + 3 * rgbImage.Width * rgbImage.Height;
                header.Reserved1 = 0;
                header.Reserved2 = 0;
                byte[] bufferHeader = StructAsByteArray(header);
                fs.Write(bufferHeader, 0, bufferHeader.Length);
                infoheader.Size = Marshal.SizeOf(infoheader);
                infoheader.Width = rgbImage.Width;
                infoheader.Height = rgbImage.Height;
                infoheader.Planes = 1;
                infoheader.BitCount = 24;
                infoheader.Compression = 0; // Uncompressed
                infoheader.SizeImage = rgbImage.Width * rgbImage.Height * 3;
                infoheader.XPelsPerMeter = 96;
                infoheader.YPelsPerMeter = 96;
                infoheader.ClrUsed = 0;
                infoheader.ClrImportant = 0;
                byte[] bufferInfoHeader = StructAsByteArray(infoheader);
                fs.Write(bufferInfoHeader, 0, bufferInfoHeader.Length);
                byte[] imageData = new byte[infoheader.SizeImage];
                Marshal.Copy(rgbImage.ImageData, imageData, 0, infoheader.SizeImage);
                fs.Write(imageData, 0, infoheader.SizeImage);
            }
        }
#endif // DUMP_IMAGES

        [TestMethod]
        [Timeout(60000)]
        public void TestDistortion()
        {
            // Create a checkerboard image
            var img = Microsoft.Psi.Imaging.Image.Create(1024, 1024, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
            unsafe
            {
                byte* row = (byte*)img.ImageData.ToPointer();
                for (int i = 0; i < img.Height; i++)
                {
                    byte* col = row;
                    for (int j = 0; j < img.Width; j++)
                    {
                        if ((i / 20 + j / 20) % 2 == 0)
                        {
                            col[0] = 255;
                            col[1] = 0;
                            col[2] = 0;
                        }
                        else
                        {
                            col[0] = 0;
                            col[1] = 0;
                            col[2] = 255;
                        }

                        col += img.BitsPerPixel / 8;
                    }

                    row += img.Stride;
                }
#if DUMP_IMAGES
                SaveColorToBMP("checkerboard.bmp", img);
#endif // DUMP_IMAGES
            }

            // Setup our distortion coefficients
            double[] distortionCoefficients = new double[6] { 1.10156359448570129, -0.049757665717193485, -0.0018714899575029596, 0.0, 0.0, 0.0 };
            double[] tangentialCoefficients = new double[2] { 0.0083588278483703853, 0.0 };

            // Next run distort on the image
            var distortedImage = Microsoft.Psi.Imaging.Image.Create(img.Width, img.Height, img.PixelFormat);
            var intrinsicMat = CreateMatrix.Dense<double>(3, 3, new double[9] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 });
            var ci = new CameraIntrinsics(
                img.Width,
                img.Height,
                intrinsicMat,
                Vector<double>.Build.DenseOfArray(distortionCoefficients),
                Vector<double>.Build.DenseOfArray(tangentialCoefficients));
            unsafe
            {
                byte* dstrow = (byte*)distortedImage.ImageData.ToPointer();
                for (int i = 0; i < distortedImage.Height; i++)
                {
                    byte* dstcol = dstrow;
                    for (int j = 0; j < distortedImage.Width; j++)
                    {
                        MathNet.Spatial.Euclidean.Point2D pixelCoord = new MathNet.Spatial.Euclidean.Point2D((i - 512.0) / 1024.0, (j - 512.0) / 1024.0);
                        MathNet.Spatial.Euclidean.Point2D distortedPixelCoord;
                        ci.DistortPoint(pixelCoord, out distortedPixelCoord);

                        int px = (int)(distortedPixelCoord.X * 1024.0 + 512.0);
                        int py = (int)(distortedPixelCoord.Y * 1024.0 + 512.0);
                        if (px >= 0 && px < img.Width && py >= 0 && py < img.Height)
                        {
                            byte* src = (byte*)img.ImageData.ToPointer() + py * distortedImage.Stride + px * distortedImage.BitsPerPixel / 8;
                            dstcol[0] = src[0];
                            dstcol[1] = src[1];
                            dstcol[2] = src[2];
                        }

                        dstcol += distortedImage.BitsPerPixel / 8;
                    }

                    dstrow += distortedImage.Stride;
                }
#if DUMP_IMAGES
                SaveColorToBMP("distorted.bmp", distortedImage);
#endif // DUMP_IMAGES
            }

            // Finally run undistort on the result
            var undistortedImage = Microsoft.Psi.Imaging.Image.Create(img.Width, img.Height, img.PixelFormat);
            unsafe
            {
                double err = 0.0;
                int numPts = 0;
                byte* dstrow = (byte*)undistortedImage.ImageData.ToPointer();
                for (int i = 0; i < undistortedImage.Height; i++)
                {
                    byte* dstcol = dstrow;
                    for (int j = 0; j < undistortedImage.Width; j++)
                    {
                        MathNet.Spatial.Euclidean.Point2D pixelCoord = new MathNet.Spatial.Euclidean.Point2D((i - 512.0) / 1024.0, (j - 512.0) / 1024.0);
                        MathNet.Spatial.Euclidean.Point2D distortedPixelCoord;
                        ci.DistortPoint(pixelCoord, out distortedPixelCoord);
                        var undistortedPixelCoord = ci.UndistortPoint(distortedPixelCoord);

                        int px = (int)(undistortedPixelCoord.X * 1024.0 + 512.0);
                        int py = (int)(undistortedPixelCoord.Y * 1024.0 + 512.0);
                        if (px >= 0 && px < img.Width && py >= 0 && py < img.Height)
                        {
                            byte* src = (byte*)img.ImageData.ToPointer() + py * img.Stride + px * img.BitsPerPixel / 8;
                            dstcol[0] = src[0];
                            dstcol[1] = src[1];
                            dstcol[2] = src[2];
                            byte* src2 = (byte*)img.ImageData.ToPointer() + i * img.Stride + j * img.BitsPerPixel / 8;
                            double dx = (double)src2[0] - (double)src[0];
                            double dy = (double)src2[1] - (double)src[1];
                            double dz = (double)src2[2] - (double)src[2];
                            err += dx * dx + dy * dy + dz * dz;
                            numPts++;
                        }

                        dstcol += undistortedImage.BitsPerPixel / 8;
                    }

                    dstrow += undistortedImage.Stride;
                }

                double rmse = Math.Sqrt(err / (double)numPts);
                if (rmse > 100)
                {
                    throw new AssertFailedException("Distort/Undistort returned incorrect results.");
                }
#if DUMP_IMAGES
                SaveColorToBMP("undistorted.bmp", undistortedImage);
#endif // DUMP_IMAGES
            }
        }

#if DUMP_IMAGES
        private static byte[] StructAsByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);
            byte[] arr = new byte[len];
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
#endif // DUMP_IMAGES

#if DUMP_IMAGES
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BitmapFileHeader
        {
            public short Type;
            public int Size;
            public short Reserved1;
            public short Reserved2;
            public int OffBits;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BitmapInfoHeader
        {
            public int Size;
            public int Width;
            public int Height;
            public short Planes;
            public short BitCount;
            public int Compression;
            public int SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public int ClrUsed;
            public int ClrImportant;
        }
#endif // DUMP_IMAGES
    }
}
