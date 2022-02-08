// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Calibration
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
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
            bool useColor = false;
            bool reverseDirection = true;
            int width = useColor ? 1280 : 640;
            int height = useColor ? 720 : 576;
            var img = new Microsoft.Psi.Imaging.Image(width, height, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
            unsafe
            {
                byte* row = (byte*)img.ImageData.ToPointer();
                var bytesPerPixel = img.BitsPerPixel / 8;
                for (int i = 0; i < img.Height; i++)
                {
                    byte* col = row;
                    for (int j = 0; j < img.Width; j++)
                    {
                        if ((i / 20 + j / 20) % 2 == 0)
                        {
                            col[0] = (byte)(255.0f * (float)j / (float)img.Width);
                            col[1] = (byte)(255.0f * (1.0f - (float)j / (float)img.Width));
                            col[2] = 0;
                        }
                        else
                        {
                            col[0] = 0;
                            col[1] = (byte)(255.0f * (float)i / (float)img.Height);
                            col[2] = (byte)(255.0f * (1.0f - (float)i / (float)img.Height));
                        }

                        col += bytesPerPixel;
                    }

                    row += img.Stride;
                }
#if DUMP_IMAGES
                SaveColorToBMP("checkerboard.bmp", img);
#endif // DUMP_IMAGES
            }

            double[] colorAzureDistortionCoefficients = new double[6]
            {
                0.609246314,
                -2.84837151,
                1.63566089,
                0.483219713,
                -2.66301942,
                1.55776918,
            };
            double[] colorAzureTangentialCoefficients = new double[2]
            {
                -0.000216085638,
                0.000744335062,
            };
            double[] colorIntrinsics = new double[4]
            {
                638.904968, // cx
                350.822327, // cy
                607.090698, // fx
                607.030762, // fy
            };
            double[] depthIntrinsics = new double[4]
            {
                326.131775, // cx
                324.755524, // cy
                504.679749, // fx
                504.865875, // fy
            };

            double[] depthAzureDistortionCoefficients = new double[6]
            {
                0.228193134,
                -0.0650567561,
                -0.000764187891,
                0.568694472,
                -0.0599768497,
                -0.0119919786,
            };
            double[] depthAzureTangentialCoefficients = new double[2]
            {
                -9.04210319e-05,
                -9.16166828e-05,
            };

            // Setup our distortion coefficients
            var distortionCoefficients = useColor ? colorAzureDistortionCoefficients : depthAzureDistortionCoefficients;
            var tangentialCoefficients = useColor ? colorAzureTangentialCoefficients : depthAzureTangentialCoefficients;

            // Next run distort on the image
            var distortedImage = new Microsoft.Psi.Imaging.Image(img.Width, img.Height, img.PixelFormat);
            double[] colorArray = new double[9] { colorIntrinsics[2], 0.0, 0.0, 0.0, colorIntrinsics[3], 0.0, colorIntrinsics[0], colorIntrinsics[1], 1.0, };
            double[] depthArray = new double[9] { depthIntrinsics[2], 0.0, 0.0, 0.0, depthIntrinsics[3], 0.0, depthIntrinsics[0], depthIntrinsics[1], 1.0, };
            var intrinsicMat = CreateMatrix.Dense<double>(3, 3, useColor ? colorArray : depthArray);
            var ci = new CameraIntrinsics(
                img.Width,
                img.Height,
                intrinsicMat,
                Vector<double>.Build.DenseOfArray(distortionCoefficients),
                Vector<double>.Build.DenseOfArray(tangentialCoefficients),
                reverseDirection);

            unsafe
            {
                byte* dstrow = (byte*)distortedImage.ImageData.ToPointer();
                var imgBytesPerPixel = img.BitsPerPixel / 8;
                var distortedImageBytesPerPixel = distortedImage.BitsPerPixel / 8;
                for (int i = 0; i < distortedImage.Height; i++)
                {
                    byte* dstcol = dstrow;
                    for (int j = 0; j < distortedImage.Width; j++)
                    {
                        MathNet.Spatial.Euclidean.Point2D pixelCoord = new MathNet.Spatial.Euclidean.Point2D(
                            ((float)j - ci.PrincipalPoint.X) / ci.FocalLengthXY.X,
                            ((float)i - ci.PrincipalPoint.Y) / ci.FocalLengthXY.Y);

                        Point2D undistortedPoint;
                        bool converged = ci.TryUndistortPoint(pixelCoord, out undistortedPoint);

                        int px = (int)(undistortedPoint.X * ci.FocalLengthXY.X + ci.PrincipalPoint.X);
                        int py = (int)(undistortedPoint.Y * ci.FocalLengthXY.Y + ci.PrincipalPoint.Y);
                        if (converged && px >= 0 && px < img.Width && py >= 0 && py < img.Height)
                        {
                            byte* src = (byte*)img.ImageData.ToPointer() + py * img.Stride + px * imgBytesPerPixel;
                            dstcol[0] = src[0];
                            dstcol[1] = src[1];
                            dstcol[2] = src[2];
                        }

                        dstcol += distortedImageBytesPerPixel;
                    }

                    dstrow += distortedImage.Stride;
                }
#if DUMP_IMAGES
                SaveColorToBMP("distorted.bmp", distortedImage);
#endif // DUMP_IMAGES
            }

            // Finally run undistort on the result
            var undistortedImage = new Microsoft.Psi.Imaging.Image(img.Width, img.Height, img.PixelFormat);
            unsafe
            {
                double err = 0.0;
                int numPts = 0;
                byte* dstrow = (byte*)undistortedImage.ImageData.ToPointer();
                var imgBytesPerPixel = img.BitsPerPixel / 8;
                var undistortedImageBytesPerPixel = undistortedImage.BitsPerPixel / 8;
                for (int i = 0; i < undistortedImage.Height; i++)
                {
                    byte* dstcol = dstrow;
                    for (int j = 0; j < undistortedImage.Width; j++)
                    {
                        MathNet.Spatial.Euclidean.Point2D pixelCoord = new MathNet.Spatial.Euclidean.Point2D(
                            ((float)j - ci.PrincipalPoint.X) / ci.FocalLengthXY.X,
                            ((float)i - ci.PrincipalPoint.Y) / ci.FocalLengthXY.Y);
                        MathNet.Spatial.Euclidean.Point2D distortedPixelCoord, undistortedPixelCoord;
                        ci.TryDistortPoint(pixelCoord, out distortedPixelCoord);
                        bool converged = ci.TryUndistortPoint(distortedPixelCoord, out undistortedPixelCoord);

                        int px = (int)(undistortedPixelCoord.X * ci.FocalLengthXY.X + ci.PrincipalPoint.X);
                        int py = (int)(undistortedPixelCoord.Y * ci.FocalLengthXY.Y + ci.PrincipalPoint.Y);
                        if (converged && px >= 0 && px < img.Width && py >= 0 && py < img.Height)
                        {
                            byte* src = (byte*)img.ImageData.ToPointer() + py * img.Stride + px * imgBytesPerPixel;
                            dstcol[0] = src[0];
                            dstcol[1] = src[1];
                            dstcol[2] = src[2];
                            byte* src2 = (byte*)img.ImageData.ToPointer() + i * img.Stride + j * imgBytesPerPixel;
                            double dx = (double)src2[0] - (double)src[0];
                            double dy = (double)src2[1] - (double)src[1];
                            double dz = (double)src2[2] - (double)src[2];
                            err += dx * dx + dy * dy + dz * dz;
                            numPts++;
                        }

                        dstcol += undistortedImageBytesPerPixel;
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
