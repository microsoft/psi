// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System.IO;
    using System.IO.Compression;
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
        public static IProducer<Shared<Image>> ToColor(this IProducer<Shared<Image>> depthImage, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            return depthImage.PipeTo(new DepthToColorConverter(depthImage.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Creates a gzipped byte array of the depth image.
        /// </summary>
        /// <param name="depthImage">Depth image to compress.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Byte array containing the compressed depth map.</returns>
        public static IProducer<byte[]> GZipCompressDepthImage(this IProducer<Shared<Image>> depthImage, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
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
        public static IProducer<Shared<Image>> GZipUncompressDepthImage(this IProducer<byte[]> compressedDepthBytes, DeliveryPolicy<byte[]> deliveryPolicy = null)
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
    }
}
