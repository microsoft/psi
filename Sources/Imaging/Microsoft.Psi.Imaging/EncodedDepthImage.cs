// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;

    /// <summary>
    /// Defines an encoded depth image.
    /// </summary>
    public class EncodedDepthImage : IDisposable
    {
        /// <summary>
        /// The memory stream storing the encoded bytes.
        /// </summary>
        private MemoryStream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImage"/> class.
        /// </summary>
        /// <param name="width">Width of encoded depth image in pixels.</param>
        /// <param name="height">Height of encoded depth image in pixels.</param>
        public EncodedDepthImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.PixelFormat = PixelFormat.Gray_16bpp;
            this.stream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImage"/> class.
        /// </summary>
        /// <param name="width">Width of image in pixels.</param>
        /// <param name="height">Height of image in pixels.</param>
        /// <param name="contents">Byte array used to initialize the image data.</param>
        public EncodedDepthImage(int width, int height, byte[] contents)
        {
            this.Width = width;
            this.Height = height;
            this.PixelFormat = PixelFormat.Gray_16bpp;
            this.stream = new MemoryStream();
            this.stream.Write(contents, 0, contents.Length);
            this.stream.Position = 0;
        }

        /// <summary>
        /// Gets the width of the depth image in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of the depth image in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the pixel format for the depth image.
        /// </summary>
        public PixelFormat PixelFormat { get; }

        /// <summary>
        /// Releases the depth image.
        /// </summary>
        public void Dispose()
        {
            this.stream.Dispose();
            this.stream = null;
        }

        /// <summary>
        /// Returns the image data as stream.
        /// </summary>
        /// <returns>A new stream containing the image data.</returns>
        public Stream ToStream()
        {
            // This method will only fail if the internal buffer is not set to be publicly
            // visible, but we create the memory stream ourselves so this should not be an issue
            if (!this.stream.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                throw new InvalidOperationException("The internal buffer is not publicly visible");
            }

            return new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
        }

        /// <summary>
        /// Returns the depth image data as a byte array.
        /// </summary>
        /// <returns>Byte array containing the image data.</returns>
        public byte[] GetBuffer()
        {
            return this.stream.GetBuffer();
        }

        /// <summary>
        /// Encodes a specified depth image with a specified encoder into the current encoded image.
        /// </summary>
        /// <param name="depthImage">The depth image to encode.</param>
        /// <param name="depthImageEncoder">The depth image encoder to use.</param>
        /// <remarks>The depth image width, height and pixel format must match. The method should not be called concurrently.</remarks>
        public void EncodeFrom(DepthImage depthImage, IDepthImageToStreamEncoder depthImageEncoder)
        {
            if (depthImage.Width != this.Width || depthImage.Height != this.Height || depthImage.PixelFormat != this.PixelFormat)
            {
                throw new InvalidOperationException("Cannot encode from an image that has a different width, height, or pixel format.");
            }

            this.stream.Position = 0;
            depthImageEncoder.EncodeToStream(depthImage, this.stream);
        }

        /// <summary>
        /// Decodes the depth image using a specified decoder.
        /// </summary>
        /// <param name="depthImageDecoder">The depth image decoder to use.</param>
        /// <returns>A new, corresponding decoded depth image.</returns>
        public DepthImage Decode(IDepthImageFromStreamDecoder depthImageDecoder)
        {
            var depthImage = new DepthImage(this.Width, this.Height);
            depthImage.DecodeFrom(this, depthImageDecoder);
            return depthImage;
        }
    }
}
