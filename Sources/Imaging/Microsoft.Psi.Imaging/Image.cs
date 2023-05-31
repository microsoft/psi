// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents an image, stored in unmanaged memory.
    /// </summary>
    /// <remarks>Using this class it is possible as to allocate a new image in unmanaged memory,
    /// as to just wrap provided pointer to unmanaged memory, where an image is stored.</remarks>
    [Serializer(typeof(Image.CustomSerializer))]
    public class Image : ImageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="unmanagedBuffer">The unmanaged array containing the image.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public Image(UnmanagedBuffer unmanagedBuffer, int width, int height, int stride, PixelFormat pixelFormat)
            : base(unmanagedBuffer, width, height, stride, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="imageData">Pointer to image data in unmanaged memory.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public Image(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat)
            : base(imageData, width, height, stride, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="width">Width of image in pixels.</param>
        /// <param name="height">Height of image in pixels.</param>
        /// <param name="pixelFormat">Pixel format.</param>
        public Image(int width, int height, PixelFormat pixelFormat)
            : base(width, height, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public Image(int width, int height, int stride, PixelFormat pixelFormat)
            : base(width, height, stride, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="bitmapData">Locked bitmap data.</param>
        /// <param name="makeCopy">Indicates whether a copy is made (default is false).</param>
        /// <remarks>
        /// <para>When the <paramref name="makeCopy"/> parameter is false (default), the image simply wraps
        /// the bitmap data. As such, the bitmap data must stay locked for the duration of using the <see cref="Image"/> object.
        /// </para>
        /// <para>If the <paramref name="makeCopy"/> parameter is set to true, a copy of the bitmap
        /// data is made, and the bitmap data can be released right after the <see cref="Image"/> has been constructed.
        /// </para>
        /// </remarks>
        public Image(BitmapData bitmapData, bool makeCopy = false)
            : base(bitmapData, makeCopy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="unmanagedBufferSize">The size of the unmanaged buffer that holds the image.</param>
        internal Image(int unmanagedBufferSize)
            : base(unmanagedBufferSize)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Image"/> from a specified bitmap.
        /// </summary>
        /// <param name="bitmap">A bitmap to create the image from.</param>
        /// <returns>A new image, which contains a copy of the specified bitmap.</returns>
        public static Image FromBitmap(Bitmap bitmap)
        {
            Image image = null;

            // Make sure that the bitmap format specified is supported (not all Bitmap.PixelFormats are supported)
            PixelFormatHelper.FromSystemPixelFormat(bitmap.PixelFormat);

            BitmapData sourceData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            try
            {
                image = new Image(sourceData, true);
            }
            finally
            {
                bitmap.UnlockBits(sourceData);
            }

            return image;
        }

        /// <summary>
        /// Creates a new <see cref="Image"/> from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file from which to create the <see cref="Image"/>.</param>
        /// <returns>A new <see cref="Image"/> created from the specified file.</returns>
        public static Image FromFile(string filename)
        {
            // Create the Bitmap using Image.FromStream instead of FromFile as FromFile does not release the file handle.
            // Though the remarks in the doc https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromstream
            // state that the stream must be kept open for the lifetime of the image, we effectively create a copy of the
            // image data in the call to FromBitmap, so it is safe to dispose the FileStream upon exiting this method.
            using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            return FromBitmap((Bitmap)Bitmap.FromStream(fileStream));
        }

        /// <summary>
        /// Saves this <see cref="Image"/> to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to which to save the <see cref="Image"/>.</param>
        public void Save(string filename)
        {
            if (this.PixelFormat == PixelFormat.Gray_16bpp || this.PixelFormat == PixelFormat.RGBA_64bpp)
            {
                throw new NotSupportedException($"Saving {this.PixelFormat} images is not currently supported. Convert to a supported format such as 8bpp grayscale or 24/32bpp color prior to saving.");
            }

            // There is no equivalent system pixel format for RGB_24bpp, so convert to BGR_24bpp then save
            if (this.PixelFormat == PixelFormat.RGB_24bpp)
            {
                int stride = 4 * ((this.Width * 3 + 3) / 2); // Rounding to nearest word boundary
                using var tmpImage = new Image(this.Width, this.Height, stride, PixelFormat.BGR_24bpp);
                this.CopyTo(tmpImage);
                tmpImage.Save(filename);
            }
            else
            {
                this.ToBitmap().Save(filename);
            }
        }

        /// <summary>
        /// Copies the image contents from a specified source locked bitmap data.
        /// </summary>
        /// <param name="bitmapData">Source locked bitmap data.</param>
        /// <remarks><para>The method copies data from the specified bitmap into the image.
        /// The image must be allocated and must have the same size as the specified
        /// bitmap data.</para></remarks>
        public void CopyFrom(BitmapData bitmapData)
        {
            int numBytes = bitmapData.Height * bitmapData.Stride;
            this.UnmanagedBuffer.CopyFrom(bitmapData.Scan0, numBytes);
        }

        /// <summary>
        /// Copies the image contents from a memory pointer.
        /// </summary>
        /// <param name="source">Memory pointer from which to copy data.</param>
        /// <param name="size">The maximum number of bytes to copy.</param>
        /// <remarks><para>The method copies data from the memory pointer into the image.
        /// The image must be allocated and must have the same size.</para></remarks>
        public void CopyFrom(IntPtr source, int size)
        {
            this.UnmanagedBuffer.CopyFrom(source, size);
        }

        /// <summary>
        /// Copies the image contents from a specified bitmap.
        /// </summary>
        /// <param name="bitmap">A bitmap to copy from.</param>
        /// <remarks><para>The method copies data from the specified bitmap into the image.
        /// The image must be allocated and must have the same size.</para></remarks>
        public void CopyFrom(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, this.Width, this.Height),
                ImageLockMode.ReadWrite,
                PixelFormatHelper.ToSystemPixelFormat(this.PixelFormat));
            try
            {
                if (this.Stride != bitmapData.Stride)
                {
                    unsafe
                    {
                        byte* src = (byte*)bitmapData.Scan0.ToPointer();
                        byte* dst = (byte*)this.ImageData.ToPointer();
                        for (int y = 0; y < this.Height; y++)
                        {
                            Buffer.MemoryCopy(src, dst, this.Stride, this.Stride);
                            src += bitmapData.Stride;
                            dst += this.Stride;
                        }
                    }
                }
                else
                {
                    int numBytes = bitmapData.Height * bitmapData.Stride;
                    this.UnmanagedBuffer.CopyFrom(bitmapData.Scan0, numBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Copies the image from a specified source image of the same size.
        /// </summary>
        /// <param name="source">Source image to copy the image from.</param>
        /// <remarks><para>The method copies the current image from the specified source image.
        /// The size of the images must be the same. Some differences in pixel
        /// formats are allowed and the method implements a translation of pixel formats.</para></remarks>
        public void CopyFrom(Image source)
        {
            source.CopyTo(this);
        }

        /// <summary>
        /// Copies the image from a specified source depth image of the same size.
        /// </summary>
        /// <param name="source">Source depth image to copy the image from.</param>
        /// <remarks><para>The method copies the current image from the specified source depth image.
        /// The size of the images must be the same and the method implements a translation of pixel formats.</para></remarks>
        public void CopyFrom(DepthImage source)
        {
            source.CopyTo(this);
        }

        /// <summary>
        /// Decodes a specified encoded image with a specified decoder into the current image.
        /// </summary>
        /// <param name="encodedImage">The encoded image to decode.</param>
        /// <param name="imageDecoder">The image decoder to use.</param>
        /// <remarks>The image width, height and pixel format must match. The method should not be called concurrently.</remarks>
        public void DecodeFrom(EncodedImage encodedImage, IImageFromStreamDecoder imageDecoder)
        {
            if (encodedImage.Width != this.Width || encodedImage.Height != this.Height ||
                (encodedImage.PixelFormat != PixelFormat.Undefined && encodedImage.PixelFormat != this.PixelFormat))
            {
                throw new InvalidOperationException("Cannot decode from an encoded image that has a different width, height, or pixel format.");
            }

            imageDecoder.DecodeFromStream(encodedImage.ToStream(), this);
        }

        /// <summary>
        /// Encodes the image using a specified encoder.
        /// </summary>
        /// <param name="imageEncoder">The image encoder to use.</param>
        /// <returns>A new, corresponding encoded image.</returns>
        public EncodedImage Encode(IImageToStreamEncoder imageEncoder)
        {
            var encodedImage = new EncodedImage(this.Width, this.Height, this.PixelFormat);
            encodedImage.EncodeFrom(this, imageEncoder);
            return encodedImage;
        }

        /// <summary>
        /// Copies the image into a specified target image of the same size.
        /// </summary>
        /// <param name="target">Target image to copy this image to.</param>
        /// <remarks><para>The method copies the current image into the specified target image.
        /// The size of the images must be the same. Some differences in pixel
        /// formats are allowed and the method implements a translation of pixel formats.</para></remarks>
        public void CopyTo(Image target)
        {
            this.CopyTo(target.ImageData, target.Width, target.Height, target.Stride, target.PixelFormat);
        }

        /// <summary>
        /// Copies the image into a target depth image of the same size.
        /// </summary>
        /// <param name="target">Target depth image to copy this image to.</param>
        /// <remarks><para>The method copies the current image into the specified depth image.
        /// The size of the images must be the same, and the image must have a <see cref="PixelFormat.Gray_16bpp"/> pixel format.</para></remarks>
        public void CopyTo(DepthImage target)
        {
            if (this.PixelFormat != PixelFormat.Gray_16bpp)
            {
                throw new InvalidOperationException($"The image must have the {nameof(PixelFormat.Gray_16bpp)} pixel format in order to copy it to a {nameof(DepthImage)}.");
            }

            this.CopyTo(target.ImageData, target.Width, target.Height, target.Stride, target.PixelFormat);
        }

        /// <summary>
        /// Gets a pixel value in the image.
        /// </summary>
        /// <param name="x">The x coordinate of the pixel.</param>
        /// <param name="y">The y coordinate of the pixel.</param>
        /// <returns>A tuple containing the channel values of the pixel.</returns>
        public (int r, int g, int b, int a) GetPixel(int x, int y)
        {
            unsafe
            {
                byte* src = (byte*)this.ImageData.ToPointer();
                int pixelOffset = x * this.BitsPerPixel / 8 + y * this.Stride;
                int red;
                int green;
                int blue;
                int alpha;
                switch (this.PixelFormat)
                {
                    case PixelFormat.BGRA_32bpp:
                        red = src[pixelOffset + 2];
                        green = src[pixelOffset + 1];
                        blue = src[pixelOffset];
                        alpha = src[pixelOffset + 3];
                        break;

                    case PixelFormat.BGR_24bpp:
                    case PixelFormat.BGRX_32bpp:
                        red = src[pixelOffset + 2];
                        green = src[pixelOffset + 1];
                        blue = src[pixelOffset];
                        alpha = 255;
                        break;

                    case PixelFormat.RGB_24bpp:
                        red = src[pixelOffset + 0];
                        green = src[pixelOffset + 1];
                        blue = src[pixelOffset + 2];
                        alpha = 255;
                        break;

                    case PixelFormat.Gray_16bpp:
                        red = green = blue = ((ushort*)src)[pixelOffset];
                        alpha = 65535;
                        break;

                    case PixelFormat.Gray_8bpp:
                        red = green = blue = src[pixelOffset];
                        alpha = 255;
                        break;

                    case PixelFormat.RGBA_64bpp:
                        red = ((ushort*)src)[pixelOffset];
                        green = ((ushort*)src)[pixelOffset + 1];
                        blue = ((ushort*)src)[pixelOffset + 2];
                        alpha = ((ushort*)src)[pixelOffset + 3];
                        break;

                    case PixelFormat.Undefined:
                    default:
                        throw new ArgumentException(ExceptionDescriptionUnexpectedPixelFormat);
                }

                return (red, green, blue, alpha);
            }
        }

        /// <summary>
        /// Sets a pixel in the image.
        /// </summary>
        /// <param name="x">Pixel's X coordinate.</param>
        /// <param name="y">Pixel's Y coordinate.</param>
        /// <param name="r">Red channel's value.</param>
        /// <param name="g">Green channel's value.</param>
        /// <param name="b">Blue channel's value.</param>
        /// <param name="a">Alpha channel's value.</param>
        /// <remarks>
        /// For grayscale images (Gray_8bpp and Gray_16bpp), the pixel value is set using the <paramref name="r"/>
        /// parameter. The <paramref name="g"/>, <paramref name="b"/> and <paramref name="a"/> parameters are ignored.
        /// </remarks>
        public void SetPixel(int x, int y, int r, int g, int b, int a)
        {
            if (x < 0 || y < 0 || x >= this.Width || y >= this.Height)
            {
                return;
            }

            unsafe
            {
                byte* src = (byte*)this.ImageData.ToPointer();
                int pixelOffset = x * this.BitsPerPixel / 8 + y * this.Stride;
                switch (this.PixelFormat)
                {
                    case PixelFormat.BGRA_32bpp:
                        src[pixelOffset + 0] = (byte)b;
                        src[pixelOffset + 1] = (byte)g;
                        src[pixelOffset + 2] = (byte)r;
                        src[pixelOffset + 3] = (byte)a;
                        break;

                    case PixelFormat.BGR_24bpp:
                    case PixelFormat.BGRX_32bpp:
                        src[pixelOffset + 0] = (byte)b;
                        src[pixelOffset + 1] = (byte)g;
                        src[pixelOffset + 2] = (byte)r;
                        break;

                    case PixelFormat.RGB_24bpp:
                        src[pixelOffset + 0] = (byte)r;
                        src[pixelOffset + 1] = (byte)g;
                        src[pixelOffset + 2] = (byte)b;
                        break;

                    case PixelFormat.Gray_16bpp:
                        src[pixelOffset + 0] = (byte)((r >> 8) & 0xff);
                        src[pixelOffset + 1] = (byte)(r & 0xff);
                        break;

                    case PixelFormat.Gray_8bpp:
                        src[pixelOffset] = (byte)r;
                        break;

                    case PixelFormat.RGBA_64bpp:
                        src[pixelOffset + 0] = (byte)((r >> 8) & 0xff);
                        src[pixelOffset + 1] = (byte)(r & 0xff);
                        src[pixelOffset + 2] = (byte)((g >> 8) & 0xff);
                        src[pixelOffset + 3] = (byte)(g & 0xff);
                        src[pixelOffset + 4] = (byte)((b >> 8) & 0xff);
                        src[pixelOffset + 5] = (byte)(b & 0xff);
                        src[pixelOffset + 6] = (byte)((a >> 8) & 0xff);
                        src[pixelOffset + 7] = (byte)(a & 0xff);
                        break;

                    case PixelFormat.Undefined:
                    default:
                        throw new ArgumentException(ExceptionDescriptionUnexpectedPixelFormat);
                }
            }
        }

        /// <summary>
        /// Sets a pixel in the image.
        /// </summary>
        /// <param name="x">Pixel's X coordinate.</param>
        /// <param name="y">Pixel's Y coordinate.</param>
        /// <param name="gray">Gray value to set pixel to.</param>
        public void SetPixel(int x, int y, int gray)
        {
            this.SetPixel(x, y, gray, gray, gray, 65535);
        }

        /// <inheritdoc/>
        public override ImageBase CreateEmptyOfSameSize()
        {
            return new Image(this.Width, this.Height, this.PixelFormat);
        }

        /// <summary>
        /// Custom serializer used for reading/writing images.
        /// </summary>
        public class CustomSerializer : ImageBase.CustomSerializer<Image>
        {
            private static IImageCompressor imageCompressor = null;

            /// <summary>
            /// Configure the type of compression to use when serializing images. Default is no compression.
            /// </summary>
            /// <param name="imageCompressor">Compressor to be used.</param>
            public static void ConfigureCompression(IImageCompressor imageCompressor)
            {
                CustomSerializer.imageCompressor = imageCompressor;
            }

            /// <inheritdoc/>
            public override void Serialize(BufferWriter writer, Image instance, SerializationContext context)
            {
                CompressionMethod compressionMethod = (imageCompressor == null) ? CompressionMethod.None : imageCompressor.CompressionMethod;
                Serializer.Serialize(writer, compressionMethod, context);
                if (compressionMethod == CompressionMethod.None)
                {
                    base.Serialize(writer, instance, context);
                }
                else
                {
                    imageCompressor.Serialize(writer, instance, context);
                }
            }

            /// <inheritdoc/>
            public override void Deserialize(BufferReader reader, ref Image target, SerializationContext context)
            {
                var compressionMethod = CompressionMethod.None;
                if (this.Schema.Version >= 4)
                {
                    Serializer.Deserialize(reader, ref compressionMethod, context);
                }

                if (compressionMethod == CompressionMethod.None)
                {
                    base.Deserialize(reader, ref target, context);
                }
                else
                {
                    imageCompressor.Deserialize(reader, ref target, context);
                }
            }
        }
    }
}
