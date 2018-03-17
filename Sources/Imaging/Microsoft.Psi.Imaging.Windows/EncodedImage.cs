// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Defines an encoded image. Currently only the following image formats are supported:
    ///   - PixelFormat.BGR_24bpp
    ///   - PixelFormat.Gray_8bpp
    ///   - PixelFormats.BGRX_32bpp
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
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        /// <param name="contents">Byte array used to initialize the image data</param>
        public EncodedImage(int width, int height, byte[] contents)
        {
            this.Width = width;
            this.Height = height;
            this.stream = new MemoryStream();
            this.stream.Write(contents, 0, contents.Length);
            this.stream.Position = 0;
            this.CountBytes = contents.Length;
        }

        /// <summary>
        /// Gets the width of the image in pixels
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        /// Gets the height of the image in pixels
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// Gets number of bytes of data in the image
        /// </summary>
        public int CountBytes { get; internal set; }

        /// <summary>
        /// Releases the image
        /// </summary>
        public void Dispose()
        {
            this.stream.Dispose();
            this.stream = null;
        }

        /// <summary>
        /// Returns the image data as a byte array
        /// </summary>
        /// <returns>Byte array containing the image data</returns>
        public byte[] GetBuffer()
        {
            return this.stream.GetBuffer();
        }

        /// <summary>
        /// Returns the image data as a byte array
        /// </summary>
        /// <param name="newLength">Number of bytes to return</param>/>
        /// <returns>Byte array containing the image data</returns>
        public byte[] GetBuffer(int newLength)
        {
            this.stream.SetLength(newLength);
            return this.stream.GetBuffer();
        }

        /// <summary>
        /// Compresses an image using the specified encoder
        /// </summary>
        /// <param name="image">Image to compress</param>
        /// <param name="encoder">Encoder to use to compress</param>
        public void EncodeFrom(Image image, BitmapEncoder encoder)
        {
            System.Windows.Media.PixelFormat format;
            if (image.PixelFormat == Imaging.PixelFormat.BGR_24bpp)
            {
                format = System.Windows.Media.PixelFormats.Bgr24;
            }
            else if (image.PixelFormat == Imaging.PixelFormat.Gray_16bpp)
            {
                format = System.Windows.Media.PixelFormats.Gray16;
            }
            else if (image.PixelFormat == Imaging.PixelFormat.Gray_8bpp)
            {
                format = System.Windows.Media.PixelFormats.Gray8;
            }
            else
            {
                format = System.Windows.Media.PixelFormats.Bgr32;
            }

            BitmapSource bitmapSource = BitmapSource.Create(image.Width, image.Height, 96, 96, format, null, image.ImageData, image.Stride * image.Height, image.Stride);
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            this.stream.Position = 0;
            encoder.Save(this.stream);
            this.stream.Flush();
            this.Width = image.Width;
            this.Height = image.Height;
            this.CountBytes = (int)this.stream.Position;
            this.stream.Position = 0;
        }

        /// <summary>
        /// Returns the pixel format of the image
        /// </summary>
        /// <returns>Returns the image's pixel format</returns>
        public Imaging.PixelFormat GetPixelFormat()
        {
            this.stream.Position = 0;
            var decoder = BitmapDecoder.Create(this.stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            if (bitmapSource.Format == System.Windows.Media.PixelFormats.Bgr24)
            {
                return Imaging.PixelFormat.BGR_24bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Gray16)
            {
                return Imaging.PixelFormat.Gray_16bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Gray8)
            {
                return Imaging.PixelFormat.Gray_8bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Bgr32)
            {
                return Imaging.PixelFormat.BGRX_32bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Bgra32)
            {
                return Imaging.PixelFormat.BGRA_32bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Rgba64)
            {
                return Imaging.PixelFormat.RGBA_64bpp;
            }
            else
            {
                throw new NotImplementedException("Format not supported.");
            }
        }

        /// <summary>
        /// Decompresses the current image into another another image
        /// </summary>
        /// <param name="image">Image used to store decompressed results</param>
        public void DecodeTo(Image image)
        {
            this.stream.Position = 0;
            var decoder = BitmapDecoder.Create(this.stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            bitmapSource.CopyPixels(Int32Rect.Empty, image.ImageData, image.Stride * image.Height, image.Stride);
            this.stream.Position = 0;
        }
    }
}
