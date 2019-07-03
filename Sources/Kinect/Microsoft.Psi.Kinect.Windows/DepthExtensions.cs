// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Define set of extensions for dealing with depth maps.
    /// </summary>
    public static class DepthExtensions
    {
        /// <summary>
        /// Simple producer for converting from depth map to colored version of depth map.
        /// </summary>
        /// <param name="depthImage">Depth image to convert.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns colored representation of the depth map.</returns>
        public static IProducer<Shared<Image>> ToColor(this IProducer<Shared<Image>> depthImage, DeliveryPolicy deliveryPolicy = null)
        {
            return depthImage.PipeTo(new DepthToColorConverter(depthImage.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Creates a gzipped byte array of the depth image.
        /// </summary>
        /// <param name="depthImage">Depth image to compress.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Byte array containing the compressed depth map.</returns>
        public static IProducer<byte[]> GZipCompressDepthImage(this IProducer<Shared<Image>> depthImage, DeliveryPolicy deliveryPolicy = null)
        {
            var memoryStream = new MemoryStream();
            var memoryStreamLo = new MemoryStream();
            var memoryStreamHi = new MemoryStream();
            byte[] buffer = null;
            return depthImage.Select(
                image =>
                {
                    if (buffer == null)
                    {
                        buffer = new byte[image.Resource.Size];
                    }

                    image.Resource.CopyTo(buffer);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using (var compressedStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true))
                    {
                        compressedStream.Write(buffer, 0, buffer.Length);
                    }

                    var output = new byte[memoryStream.Position];
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Read(output, 0, output.Length);
                    return output;
                }, deliveryPolicy);
        }

        /// <summary>
        /// Uncompressed a depth map that was previously compressed with GZip.
        /// </summary>
        /// <param name="compressedDepthBytes">Byte array of compressed depth values.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Uncompressed depth map as an image.</returns>
        public static IProducer<Shared<Image>> GZipUncompressDepthImage(this IProducer<byte[]> compressedDepthBytes, DeliveryPolicy deliveryPolicy = null)
        {
            var buffer = new byte[424 * 512 * 2];
            return compressedDepthBytes.Select(
                bytes =>
                {
                    using (var compressedStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
                    {
                        compressedStream.Read(buffer, 0, buffer.Length);
                    }

                    var psiImage = ImagePool.GetOrCreate(512, 424, PixelFormat.Gray_16bpp);
                    psiImage.Resource.CopyFrom(buffer);
                    return psiImage;
                }, deliveryPolicy);
        }

        /// <summary>
        /// Performs a ray/mesh intersection with the depth map.
        /// </summary>
        /// <param name="kinectCalibration">Defines the calibration (extrinsics and intrinsics) for the Kinect.</param>
        /// <param name="line">Ray to intersect against depth map.</param>
        /// <param name="depthImage">Depth map to ray cast against.</param>
        /// <param name="skipFactor">Distance to march on each step along ray.</param>
        /// <param name="undistort">Whether undistortion should be applied to the point.</param>
        /// <returns>Returns point of intersection.</returns>
        internal static Point3D? IntersectLineWithDepthMesh(IKinectCalibration kinectCalibration, Line3D line, Image depthImage, double skipFactor, bool undistort = true)
        {
            // max distance to check for intersection with the scene
            double totalDistance = 5;
            var delta = skipFactor * (line.EndPoint - line.StartPoint).Normalize();

            // size of increment along the ray
            int maxSteps = (int)(totalDistance / delta.Length);
            var hypothesisPoint = line.StartPoint;
            for (int i = 0; i < maxSteps; i++)
            {
                hypothesisPoint += delta;

                // get the mesh distance at the extended point
                float meshDistance = DepthExtensions.GetMeshDepthAtPoint(kinectCalibration, depthImage, hypothesisPoint, undistort);

                // if the mesh distance is less than the distance to the point we've hit the mesh
                if (!float.IsNaN(meshDistance) && (meshDistance < hypothesisPoint.Z))
                {
                    return hypothesisPoint;
                }
            }

            return null;
        }

        private static float GetMeshDepthAtPoint(IKinectCalibration kinectCalibration, Image depthImage, Point3D point, bool undistort)
        {
            Point2D depthSpacePoint = kinectCalibration.DepthIntrinsics.ToPixelSpace(point, undistort);

            // depthSpacePoint = new Point2D(depthSpacePoint.X, kinectCalibration.DepthIntrinsics.ImageHeight - depthSpacePoint.Y);
            int x = (int)Math.Round(depthSpacePoint.X);
            int y = (int)Math.Round(depthSpacePoint.Y);
            if ((x < 0) || (x >= depthImage.Width) || (y < 0) || (y >= depthImage.Height))
            {
                return float.NaN;
            }

            int byteOffset = (int)((y * depthImage.Stride) + (x * 2));
            var depth = BitConverter.ToUInt16(depthImage.ReadBytes(2, byteOffset), 0);
            if (depth == 0)
            {
                return float.NaN;
            }

            return (float)depth / 1000;
        }
    }
}
