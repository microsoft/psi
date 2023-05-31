// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines an encoded depth image.
    /// </summary>
    public class EncodedDepthImage : IDepthImage, IDisposable
    {
        [OptionalField]
        private readonly DepthValueSemantics? depthValueSemantics;

        [OptionalField]
        private readonly double? depthValueToMetersScaleFactor;

        /// <summary>
        /// The memory stream storing the encoded bytes.
        /// </summary>
        private MemoryStream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImage"/> class.
        /// </summary>
        /// <param name="width">Width of encoded depth image in pixels.</param>
        /// <param name="height">Height of encoded depth image in pixels.</param>
        /// <param name="depthValueSemantics">Optional depth value semantics.</param>
        /// <param name="depthValueToMetersScaleFactor">Optional scale factor to convert from depth values to meters.</param>
        public EncodedDepthImage(int width, int height, DepthValueSemantics depthValueSemantics = DepthValueSemantics.DistanceToPlane, double depthValueToMetersScaleFactor = 0.001)
        {
            this.Width = width;
            this.Height = height;
            this.depthValueSemantics = depthValueSemantics;
            this.depthValueToMetersScaleFactor = depthValueToMetersScaleFactor;
            this.PixelFormat = PixelFormat.Gray_16bpp;
            this.stream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImage"/> class.
        /// </summary>
        /// <param name="width">Width of image in pixels.</param>
        /// <param name="height">Height of image in pixels.</param>
        /// <param name="contents">Byte array used to initialize the image data.</param>
        /// <param name="depthValueSemantics">Optional depth value semantics.</param>
        /// <param name="depthValueToMetersScaleFactor">Optional scale factor to convert from depth values to meters.</param>
        public EncodedDepthImage(int width, int height, byte[] contents, DepthValueSemantics depthValueSemantics = DepthValueSemantics.DistanceToPlane, double depthValueToMetersScaleFactor = 0.001)
        {
            this.Width = width;
            this.Height = height;
            this.depthValueSemantics = depthValueSemantics;
            this.depthValueToMetersScaleFactor = depthValueToMetersScaleFactor;
            this.PixelFormat = PixelFormat.Gray_16bpp;
            this.stream = new MemoryStream();
            this.stream.Write(contents, 0, contents.Length);
            this.stream.Position = 0;
        }

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public PixelFormat PixelFormat { get; }

        /// <inheritdoc />
        public DepthValueSemantics DepthValueSemantics => this.depthValueSemantics ?? DepthValueSemantics.DistanceToPlane;

        /// <inheritdoc />
        public double DepthValueToMetersScaleFactor => this.depthValueToMetersScaleFactor ?? 0.001;

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
        /// Sets the depth image data from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the image data.</param>
        /// <param name="offset">The offset in buffer at which to begin copying bytes.</param>
        /// <param name="count">The maximum number of bytes to copy.</param>
        public void SetBuffer(byte[] buffer, int offset, int count)
        {
            this.stream.Position = 0;
            this.stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Encodes a specified depth image with a specified encoder into the current encoded image.
        /// </summary>
        /// <param name="depthImage">The depth image to encode.</param>
        /// <param name="depthImageEncoder">The depth image encoder to use.</param>
        /// <remarks>The depth image width, height and pixel format must match. The method should not be called concurrently.</remarks>
        public void EncodeFrom(DepthImage depthImage, IDepthImageToStreamEncoder depthImageEncoder)
        {
            if (depthImage.Width != this.Width ||
                depthImage.Height != this.Height ||
                depthImage.PixelFormat != this.PixelFormat ||
                depthImage.DepthValueSemantics != this.DepthValueSemantics ||
                depthImage.DepthValueToMetersScaleFactor != this.DepthValueToMetersScaleFactor)
            {
                throw new InvalidOperationException("Cannot encode from an image that has a different width, height, pixel format, depth value semantics, or depth value scale factor.");
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
            var depthImage = new DepthImage(this.Width, this.Height, this.DepthValueSemantics, this.DepthValueToMetersScaleFactor);
            depthImage.DecodeFrom(this, depthImageDecoder);
            return depthImage;
        }
    }
}
