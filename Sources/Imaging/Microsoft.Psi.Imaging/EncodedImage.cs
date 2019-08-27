// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    /// <summary>
    /// Defines an encoded image.
    /// </summary>
    public class EncodedImage : IDisposable
    {
        private MemoryStream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImage"/> class.
        /// </summary>
        public EncodedImage()
        {
            this.stream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImage"/> class.
        /// </summary>
        /// <param name="width">Width of image in pixels.</param>
        /// <param name="height">Height of image in pixels.</param>
        /// <param name="contents">Byte array used to initialize the image data.</param>
        public EncodedImage(int width, int height, byte[] contents)
        {
            this.Width = width;
            this.Height = height;
            this.stream = new MemoryStream();
            this.stream.Write(contents, 0, contents.Length);
            this.stream.Position = 0;
        }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// Releases the image.
        /// </summary>
        public void Dispose()
        {
            this.stream.Dispose();
            this.stream = null;
        }

        /// <summary>
        /// Returns the image data as stream.
        /// </summary>
        /// <returns>Stream containing the image data.</returns>
        public Stream GetStream()
        {
            this.stream.Position = 0;
            return this.stream;
        }

        /// <summary>
        /// Returns the image data as a byte array.
        /// </summary>
        /// <returns>Byte array containing the image data.</returns>
        public byte[] GetBuffer()
        {
            return this.stream.GetBuffer();
        }

        /// <summary>
        /// Compresses an image using the specified encoder.
        /// </summary>
        /// <param name="image">Image to compress.</param>
        /// <param name="encodeFn">Encoder to use to compress.</param>
        public void EncodeFrom(Image image, Action<Image, Stream> encodeFn)
        {
            encodeFn(image, this.GetStream());
            this.Width = image.Width;
            this.Height = image.Height;
        }
    }
}
