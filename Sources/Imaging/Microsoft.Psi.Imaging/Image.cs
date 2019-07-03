// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading.Tasks;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// The #Image class represents wrapper of an image in unmanaged memory. Using this class
    /// it is possible as to allocate new image in unmanaged memory, as to just wrap provided
    /// pointer to unmanaged memory, where an image is stored.
    /// </summary>
    [Serializer(typeof(Image.CustomSerializer))]
    public class Image : IDisposable
    {
        private UnmanagedBuffer image;
        private int width;
        private int height;
        private int stride;
        private PixelFormat pixelFormat;

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
        {
            this.image = UnmanagedBuffer.WrapIntPtr(imageData, height * stride);
            this.width = width;
            this.height = height;
            this.stride = stride;
            this.pixelFormat = pixelFormat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="image">The unmanaged array containing the image.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public Image(UnmanagedBuffer image, int width, int height, int stride, PixelFormat pixelFormat)
        {
            this.image = image;
            this.width = width;
            this.height = height;
            this.stride = stride;
            this.pixelFormat = pixelFormat;
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
            : this(UnmanagedBuffer.Allocate(height * stride), width, height, stride, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="width">Width of image in pixels.</param>
        /// <param name="height">Height of image in pixels.</param>
        /// <param name="pixelFormat">Pixel format.</param>
        public Image(int width, int height, PixelFormat pixelFormat)
            : this(UnmanagedBuffer.Allocate(height * width * GetBytesPerPixel(pixelFormat)), width, height, width * GetBytesPerPixel(pixelFormat), pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="bitmapData">Locked bitmap data.</param>
        /// <remarks><note>Unlike <see cref="FromManagedImage(BitmapData)"/> method, this constructor does not make
        /// copy of managed image. This means that managed image must stay locked for the time of using the instance
        /// of unamanged image.</note></remarks>
        public Image(BitmapData bitmapData)
        {
            this.image = UnmanagedBuffer.WrapIntPtr(bitmapData.Scan0, bitmapData.Height * bitmapData.Stride);
            this.width = bitmapData.Width;
            this.height = bitmapData.Height;
            this.stride = bitmapData.Stride;
            this.pixelFormat = PixelFormatHelper.FromSystemPixelFormat(bitmapData.PixelFormat);
        }

        /// <summary>
        /// Interface implemented by the system specific assembly.
        /// For instance, Microsoft.Psi.Imaging.Windows will define
        /// an ImageCompressor that implements this interfaces.
        /// </summary>
        public interface IImageCompressor
        {
            /// <summary>
            /// Initialize compressor.
            /// </summary>
            /// <param name="compressionMethod">Compression method.</param>
            void Initialize(Image.CustomSerializer.CompressionMethod compressionMethod);

            /// <summary>
            /// Serialize compressor.
            /// </summary>
            /// <param name="writer">Writer to which to serialize.</param>
            /// <param name="instance">Image instance to serialize.</param>
            /// <param name="context">Serialization context.</param>
            void Serialize(BufferWriter writer, Image instance, SerializationContext context);

            /// <summary>
            /// Deserialize compressor.
            /// </summary>
            /// <param name="reader">Reader from which to deserialize.</param>
            /// <param name="target">Target image to which to deserialize.</param>
            /// <param name="context">Serialization context.</param>
            void Deserialize(BufferReader reader, ref Image target, SerializationContext context);
        }

        /// <summary>
        /// Gets a pointer to image data in unmanaged memory.
        /// </summary>
        public IntPtr ImageData => this.image.Data;

        /// <summary>
        /// Gets image width in pixels.
        /// </summary>
        public int Width => this.width;

        /// <summary>
        /// Gets image height in pixels.
        /// </summary>
        public int Height => this.height;

        /// <summary>
        /// Gets image stride (line size in bytes).
        /// </summary>
        public int Stride => this.stride;

        /// <summary>
        /// Gets the size of the image in bytes (stride times height).
        /// </summary>
        public int Size => this.stride * this.height;

        /// <summary>
        /// Gets the bits per pixel in the image.
        /// </summary>
        public int BitsPerPixel => PixelFormatHelper.GetBitsPerPixel(this.pixelFormat);

        /// <summary>
        /// Gets image pixel format.
        /// </summary>
        public PixelFormat PixelFormat => this.pixelFormat;

        /// <summary>
        /// Allocate new image in unmanaged memory.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <returns>Return image allocated in unmanaged memory.</returns>
        /// <remarks><para>Allocate new image with specified attributes in unmanaged memory.</para>
        /// </remarks>
        public static Image Create(int width, int height, PixelFormat pixelFormat)
        {
            int bytesPerPixel = pixelFormat.GetBitsPerPixel() / 8;

            // check image size
            if ((width <= 0) || (height <= 0))
            {
                throw new Exception("Invalid image size specified.");
            }

            // calculate stride
            int stride = width * bytesPerPixel;

            if (stride % 4 != 0)
            {
                stride += 4 - (stride % 4);
            }

            // allocate memory for the image
            return new Image(UnmanagedBuffer.Allocate(stride * height), width, height, stride, pixelFormat);
        }

        /// <summary>
        /// Create unmanaged image from the specified managed image.
        /// </summary>
        /// <param name="imageData">Source locked image data.</param>
        /// <returns>Returns new unmanaged image, which is a copy of source managed image.</returns>
        /// <remarks><para>The method creates an exact copy of specified managed image, but allocated
        /// in unmanaged memory. This means that managed image may be unlocked right after call to this
        /// method.</para></remarks>
        public static Image FromManagedImage(BitmapData imageData)
        {
            PixelFormat pixelFormat = PixelFormatHelper.FromSystemPixelFormat(imageData.PixelFormat);

            // allocate memory for the image
            return new Image(UnmanagedBuffer.CreateCopyFrom(imageData.Scan0, imageData.Stride * imageData.Height), imageData.Width, imageData.Height, imageData.Stride, pixelFormat);
        }

        /// <summary>
        /// Create unmanaged image from the specified managed image.
        /// </summary>
        /// <param name="image">Source managed image.</param>
        /// <returns>Returns new unmanaged image, which is a copy of source managed image.</returns>
        /// <remarks><para>The method creates an exact copy of specified managed image, but allocated
        /// in unmanaged memory.</para></remarks>
        public static Image FromManagedImage(Bitmap image)
        {
            Image dstImage = null;

            // Make sure that the bitmap format specified is supported (not all Bitmap.PixelFormats are supported)
            PixelFormatHelper.FromSystemPixelFormat(image.PixelFormat);

            BitmapData sourceData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                image.PixelFormat);

            try
            {
                dstImage = FromManagedImage(sourceData);
            }
            finally
            {
                image.UnlockBits(sourceData);
            }

            return dstImage;
        }

        /// <summary>
        /// Function to convert RGB color into grayscale.
        /// </summary>
        /// <param name="r">red component (Range=0..255).</param>
        /// <param name="g">green component (Range=0..255).</param>
        /// <param name="b">Blue component (Range=0..255).</param>
        /// <returns>Grayscale value (Range=0..255).</returns>
        public static byte Rgb2Gray(byte r, byte g, byte b)
        {
            return (byte)(((4897 * r) + (9617 * g) + (1868 * b)) >> 14);
        }

        /// <summary>
        /// Function to convert RGB color into grayscale.
        /// </summary>
        /// <param name="r">red component (Range=0..65535).</param>
        /// <param name="g">green component (Range=0..65535).</param>
        /// <param name="b">Blue component (Range=0..65535).</param>
        /// <returns>Grayscale value (Range=0..65535).</returns>
        public static ushort Rgb2Gray(ushort r, ushort g, ushort b)
        {
            return (ushort)(((4897 * r) + (9617 * g) + (1868 * b)) >> 14);
        }

        /// <summary>
        /// Set pallete of the 8 bpp indexed image to grayscale.
        /// </summary>
        /// <param name="image">Image to initialize.</param>
        /// <remarks>The method initializes palette of
        /// <see cref="System.Drawing.Imaging.PixelFormat">Format8bppIndexed</see>
        /// image with 256 gradients of gray color.</remarks>
        public static void SetGrayscalePalette(Bitmap image)
        {
            // check pixel format
            if (image.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                throw new Exception("Source image is not 8 bpp image.");
            }

            // get palette
            ColorPalette cp = image.Palette;

            // init palette
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }

            // set palette back
            image.Palette = cp;
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <remarks><para>Frees unmanaged resources used by the object. The object becomes unusable
        /// after that.</para>
        /// <par><note>The method needs to be called only in the case if unmanaged image was allocated
        /// using <see cref="Create"/> method. In the case if the class instance was created using constructor,
        /// this method does not free unmanaged memory.</note></par>
        /// </remarks>
        public void Dispose()
        {
            this.image.Dispose();
            this.image = null;
        }

        /// <summary>
        /// Copy unmanaged image.
        /// </summary>
        /// <param name="destImage">Destination image to copy this image to.</param>
        /// <remarks><para>The method copies current unmanaged image to the specified image.
        /// Size of the destination image must be exactly the same. Some differences in pixel
        /// formats are allowed and the method implements a translation of pixel formats.</para></remarks>
        public void CopyTo(Image destImage)
        {
            this.CopyTo(destImage.image.Data, destImage.Width, destImage.Height, destImage.Stride, destImage.PixelFormat);
        }

        /// <summary>
        /// Copies the psi image to a byte array buffer.
        /// </summary>
        /// <param name="destinationBuffer">The buffer to copy to.</param>
        /// <remarks><para>The method copies current unmanaged image to the specified buffer.
        /// The buffer must be allocated and must have the same size.</para></remarks>
        public void CopyTo(byte[] destinationBuffer)
        {
            this.image.CopyTo(destinationBuffer);
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
        public void SetPixel(int x, int y, int r, int g, int b, int a)
        {
            if (x < 0 || y < 0 || x >= (int)this.width || y >= (int)this.height)
            {
                return;
            }

            unsafe
            {
                byte* src = (byte*)this.image.Data.ToPointer();
                int pixelOffset = x * this.BitsPerPixel / 8 + y * this.Stride;
                switch (this.pixelFormat)
                {
                    case PixelFormat.BGRA_32bpp:
                        src[pixelOffset + 0] = (byte)r;
                        src[pixelOffset + 1] = (byte)g;
                        src[pixelOffset + 2] = (byte)b;
                        src[pixelOffset + 3] = (byte)a;
                        break;
                    case PixelFormat.BGR_24bpp:
                    case PixelFormat.BGRX_32bpp:
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
                    default:
                        throw new Exception("Unsupported type");
                }
            }
        }

        /// <summary>
        /// Copies the psi image to an unmanaged buffer.
        /// </summary>
        /// <param name="destination">The destination buffer.</param>
        /// <param name="width">The destination image width.</param>
        /// <param name="height">The destination image height.</param>
        /// <param name="dstStride">The destination image stride.</param>
        /// <param name="destinationFormat">The destination pixel format.</param>
        public void CopyTo(IntPtr destination, int width, int height, int dstStride, PixelFormat destinationFormat)
        {
            if ((this.width != width) || (this.height != height))
            {
                throw new Exception("Destination image has different size or pixel format.");
            }

            // Check if pixel formats are the same. If so, do a straight up copy
            if (this.PixelFormat == destinationFormat)
            {
                if (this.stride == dstStride)
                {
                    this.image.CopyTo(destination, dstStride * height);
                }
                else
                {
                    unsafe
                    {
                        int copyLength = (this.stride < dstStride) ? this.stride : dstStride;

                        byte* src = (byte*)this.image.Data.ToPointer();
                        byte* dst = (byte*)destination.ToPointer();

                        // copy line by line
                        for (int i = 0; i < this.height; i++)
                        {
                            Buffer.MemoryCopy(src, dst, copyLength, copyLength);

                            dst += dstStride;
                            src += this.stride;
                        }
                    }
                }
            }
            else if ((this.pixelFormat == PixelFormat.BGR_24bpp) &&
                     (destinationFormat == PixelFormat.BGRX_32bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();
                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (dstStride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            *dstCopy++ = *srcCopy++;
                            *dstCopy++ = *srcCopy++;
                            *dstCopy++ = *srcCopy++;
                            *dstCopy++ = 255;
                        }
                    });
                }
            }
            else if ((this.pixelFormat == PixelFormat.BGRX_32bpp) &&
                     (destinationFormat == PixelFormat.BGR_24bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();
                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (dstStride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            *dstCopy++ = *srcCopy++;
                            *dstCopy++ = *srcCopy++;
                            *dstCopy++ = *srcCopy++;
                            srcCopy++;
                        }
                    });
                }
            }
            else if ((this.pixelFormat == PixelFormat.BGR_24bpp) &&
                     (destinationFormat == PixelFormat.Gray_8bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();

                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (dstStride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            *dstCopy++ = Rgb2Gray(*srcCopy, *(srcCopy + 1), *(srcCopy + 2));
                            srcCopy += 3;
                        }
                    });
                }
            }
            else
            {
                this.CopyImageSlow(this.image.Data, this.pixelFormat, destination, dstStride, destinationFormat);
            }
        }

        /// <summary>
        /// Copies data from a byte array buffer into the psi image.
        /// </summary>
        /// <param name="sourceBuffer">The buffer to copy from.</param>
        /// <remarks><para>The method copies data from the specified buffer into the unmanaged image
        /// The image must be allocated and must have the same size.</para></remarks>
        public void CopyFrom(byte[] sourceBuffer)
        {
            this.image.CopyFrom(sourceBuffer);
        }

        /// <summary>
        /// Copies data from an unmanaged buffer.
        /// </summary>
        /// <param name="sourcePtr">A pointer to the unmanaged buffer to copy from.</param>
        /// <remarks><para>The method copies data from the specified buffer into the unmanaged image
        /// The image must be allocated and must have the same size.</para></remarks>
        public void CopyFrom(IntPtr sourcePtr)
        {
            this.image.CopyFrom(sourcePtr, this.image.Size);
        }

        /// <summary>
        /// Copies data from an unmanaged buffer.
        /// </summary>
        /// <param name="bitmap">A bitmap to copy from.</param>
        /// <remarks><para>The method copies data from the specified bitmap into the unmanaged image
        /// The image must be allocated and must have the same size.</para></remarks>
        public void CopyFrom(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, this.width, this.height),
                ImageLockMode.ReadWrite,
                PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat));
            try
            {
                this.image.CopyFrom(bitmapData.Scan0, this.image.Size);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Create managed image from the unmanaged.
        /// </summary>
        /// <returns>Returns managed copy of the unmanaged image.</returns>
        /// <remarks><para>The method creates a managed copy of the unmanaged image with the
        /// same size and pixel format (it calls <see cref="ToManagedImage(bool)"/> specifying
        /// <see langword="true"/> for the <b>makeCopy</b> parameter).</para></remarks>
        public Bitmap ToManagedImage()
        {
            return this.ToManagedImage(true);
        }

        /// <summary>
        /// Create managed image from the unmanaged.
        /// </summary>
        /// <param name="makeCopy">Make a copy of the unmanaged image or not.</param>
        /// <returns>Returns managed copy of the unmanaged image.</returns>
        /// <remarks><para>If the <paramref name="makeCopy"/> is set to <see langword="true"/>, then the method
        /// creates a managed copy of the unmanaged image, so the managed image stays valid even when the unmanaged
        /// image gets disposed. However, setting this parameter to <see langword="false"/> creates a managed image which is
        /// just a wrapper around the unmanaged image. So if unmanaged image is disposed, the
        /// managed image becomes no longer valid and accessing it will generate an exception.</para></remarks>
        public Bitmap ToManagedImage(bool makeCopy)
        {
            Bitmap dstImage = null;

            try
            {
                if (!makeCopy)
                {
                    dstImage = new Bitmap(this.width, this.height, this.stride, PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat), this.image.Data);
                    if (this.pixelFormat == PixelFormat.Gray_8bpp)
                    {
                        Image.SetGrayscalePalette(dstImage);
                    }
                }
                else
                {
                    // create new image of required format
                    dstImage = (this.pixelFormat == PixelFormat.Gray_8bpp) ?
                        Image.CreateGrayscaleImage(this.width, this.height) :
                        new Bitmap(this.width, this.height, PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat));

                    // lock destination bitmap data
                    BitmapData dstData = dstImage.LockBits(
                        new Rectangle(0, 0, this.width, this.height),
                        ImageLockMode.ReadWrite,
                        PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat));

                    int dstStride = dstData.Stride;
                    int lineSize = Math.Min(this.stride, dstStride);

                    unsafe
                    {
                        byte* dst = (byte*)dstData.Scan0.ToPointer();
                        byte* src = (byte*)this.image.Data.ToPointer();

                        if (this.stride != dstStride)
                        {
                            // copy image
                            for (int y = 0; y < this.height; y++)
                            {
                                Buffer.MemoryCopy(src, dst, lineSize, lineSize);
                                dst += dstStride;
                                src += this.stride;
                            }
                        }
                        else
                        {
                            var size = this.stride * this.height;
                            Buffer.MemoryCopy(src, dst, size, size);
                        }
                    }

                    // unlock destination images
                    dstImage.UnlockBits(dstData);
                }

                return dstImage;
            }
            catch (Exception)
            {
                if (dstImage != null)
                {
                    dstImage.Dispose();
                }

                throw new Exception("The unmanaged image has some invalid properties, which results in failure of converting it to managed image.");
            }
        }

        /// <summary>
        /// Reads image data as a series of bytes.
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="offset">Offset from start of image data.</param>
        /// <returns>Array of bytes read.</returns>
        public byte[] ReadBytes(int count, int offset = 0)
        {
            return this.image.ReadBytes(count, offset);
        }

        /// <summary>
        /// Creates a copy of the image cropped to the specified dimensions.
        /// </summary>
        /// <param name="left">The left of the region to crop.</param>
        /// <param name="top">The top of the region to crop.</param>
        /// <param name="width">The width of the region to crop.</param>
        /// <param name="height">The height of the region to crop.</param>
        /// <returns>The cropped image.</returns>
        public Shared<Image> Crop(int left, int top, int width, int height)
        {
            if ((left < 0) || (left > (this.width - 1)))
            {
                throw new ArgumentOutOfRangeException("left", "left is out of range");
            }

            if ((top < 0) || (top > (this.height - 1)))
            {
                throw new ArgumentOutOfRangeException("top", "top is out of range");
            }

            if ((width < 0) || ((left + width) > this.width))
            {
                throw new ArgumentOutOfRangeException("width", "width is out of range");
            }

            if ((height < 0) || ((top + height) > this.height))
            {
                throw new ArgumentOutOfRangeException("height", "height is out of range");
            }

            // Cropped image will be returned as a new image - original (this) image is not modified
            Shared<Image> croppedImage = ImagePool.GetOrCreate(width, height, this.pixelFormat);
            Debug.Assert(croppedImage.Resource.image.Data != IntPtr.Zero, "Unexpected empty image");
            unsafe
            {
                int bytesPerPixel = this.BitsPerPixel / 8;

                // Compute the number of bytes in each line of the crop region
                int copyLength = width * bytesPerPixel;

                // Start at top-left of region to crop
                byte* src = (byte*)this.image.Data.ToPointer() + (top * this.stride) + (left * bytesPerPixel);
                byte* dst = (byte*)croppedImage.Resource.image.Data.ToPointer();

                // Copy line by line
                for (int i = 0; i < height; i++)
                {
                    Buffer.MemoryCopy(src, dst, copyLength, copyLength);

                    src += this.stride;
                    dst += croppedImage.Resource.stride;
                }
            }

            return croppedImage;
        }

        private static Bitmap CreateGrayscaleImage(int width, int height)
        {
            // create new image
            Bitmap image = new Bitmap(width, height, PixelFormatHelper.ToSystemPixelFormat(PixelFormat.Gray_8bpp));

            // set palette to grayscale
            SetGrayscalePalette(image);

            // return new image
            return image;
        }

        private static int GetBytesPerPixel(PixelFormat pixelFormat)
        {
            return PixelFormatHelper.GetBytesPerPixel(pixelFormat);
        }

        private void CopyImageSlow(IntPtr srcBuffer, PixelFormat srcFormat, IntPtr dstBuffer, int dstStride, PixelFormat dstFormat)
        {
            unsafe
            {
                int srcBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(srcFormat);
                int dstBytesPerPixel = PixelFormatHelper.GetBytesPerPixel(dstFormat);
                Parallel.For(0, this.Height, i =>
                {
                    byte* srcCol = (byte*)srcBuffer.ToPointer() + (i * this.stride);
                    byte* dstCol = (byte*)dstBuffer.ToPointer() + (i * dstStride);
                    for (int j = 0; j < this.width; j++)
                    {
                        int red = 0;
                        int green = 0;
                        int blue = 0;
                        int alpha = 255;
                        switch (srcFormat)
                        {
                            case PixelFormat.Gray_8bpp:
                                red = green = blue = srcCol[0];
                                break;

                            case PixelFormat.Gray_16bpp:
                                red = green = blue = ((ushort*)srcCol)[0];
                                break;

                            case PixelFormat.BGR_24bpp:
                                blue = srcCol[0];
                                green = srcCol[1];
                                red = srcCol[2];
                                break;

                            case PixelFormat.BGRX_32bpp:
                                blue = srcCol[0];
                                green = srcCol[1];
                                red = srcCol[2];
                                break;

                            case PixelFormat.BGRA_32bpp:
                                blue = srcCol[0];
                                green = srcCol[1];
                                red = srcCol[2];
                                alpha = srcCol[3];
                                break;

                            case PixelFormat.RGBA_64bpp:
                                red = ((ushort*)srcCol)[0];
                                green = ((ushort*)srcCol)[1];
                                blue = ((ushort*)srcCol)[2];
                                alpha = ((ushort*)srcCol)[3];
                                break;
                        }

                        switch (dstFormat)
                        {
                            case PixelFormat.Gray_8bpp:
                                dstCol[0] = Rgb2Gray((byte)red, (byte)green, (byte)blue);
                                break;

                            case PixelFormat.Gray_16bpp:
                                ((ushort*)dstCol)[0] = Rgb2Gray((ushort)red, (ushort)green, (ushort)blue);
                                break;

                            case PixelFormat.BGR_24bpp:
                            case PixelFormat.BGRX_32bpp:
                                dstCol[0] = (byte)blue;
                                dstCol[1] = (byte)green;
                                dstCol[2] = (byte)red;
                                break;

                            case PixelFormat.BGRA_32bpp:
                                dstCol[0] = (byte)blue;
                                dstCol[1] = (byte)green;
                                dstCol[2] = (byte)red;
                                dstCol[3] = (byte)alpha;
                                break;

                            case PixelFormat.RGBA_64bpp:
                                ((ushort*)dstCol)[0] = (ushort)red;
                                ((ushort*)dstCol)[1] = (ushort)green;
                                ((ushort*)dstCol)[2] = (ushort)blue;
                                ((ushort*)dstCol)[3] = (ushort)alpha;
                                break;
                        }

                        srcCol += srcBytesPerPixel;
                        dstCol += dstBytesPerPixel;
                    }
                });
            }
        }

        /// <summary>
        /// Custom serializer used for reading/writing images.
        /// </summary>
        public class CustomSerializer : ISerializer<Image>
        {
            private const int Version = 4;
            private static CompressionMethod compressionMethod;
            private TypeSchema schema;
            private IImageCompressor imageCompressor;

            /// <summary>
            /// Defines type of compression to use when serializing out an Image.
            /// </summary>
            public enum CompressionMethod
            {
                /// <summary>
                /// Use JPEG compression
                /// </summary>
                JPEG,

                /// <summary>
                /// Use PNG compression
                /// </summary>
                PNG,

                /// <summary>
                /// Use no compression
                /// </summary>
                None,
            }

            /// <summary>
            /// Maybe called to initialize type of compression to use. Default is no compression.
            /// </summary>
            /// <param name="method">Type of compression to use.</param>
            public static void ConfigureCompression(CompressionMethod method)
            {
                compressionMethod = method;
            }

            /// <summary>
            /// Initialize custom serializer.
            /// </summary>
            /// <param name="serializers">Known serializers.</param>
            /// <param name="targetSchema">Target type schema.</param>
            /// <returns>Type schema.</returns>
            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                if (targetSchema == null)
                {
                    TypeMemberSchema[] schemaMembers = new TypeMemberSchema[6]
                    {
                        new TypeMemberSchema("compression", typeof(CompressionMethod).AssemblyQualifiedName, false),
                        new TypeMemberSchema("image", typeof(UnmanagedBuffer).AssemblyQualifiedName, true),
                        new TypeMemberSchema("width", typeof(int).AssemblyQualifiedName, true),
                        new TypeMemberSchema("height", typeof(int).AssemblyQualifiedName, true),
                        new TypeMemberSchema("stride", typeof(int).AssemblyQualifiedName, true),
                        new TypeMemberSchema("pixelFormat", typeof(Imaging.PixelFormat).AssemblyQualifiedName, true),
                    };
                    var type = typeof(Imaging.Image);
                    var name = TypeSchema.GetContractName(type, serializers.RuntimeVersion);
                    this.schema = new TypeSchema(name, TypeSchema.GetId(name), type.AssemblyQualifiedName, TypeFlags.IsCollection, schemaMembers, Version);
                }
                else
                {
                    this.schema = targetSchema;
                }

                // Check to see if we can retrieve the custom compression serializer
                try
                {
                    var assembly = System.Reflection.Assembly.Load(new System.Reflection.AssemblyName("Microsoft.Psi.Imaging.Windows"));
                    if (assembly != null)
                    {
                        var ic = assembly.CreateInstance("Microsoft.Psi.Imaging.ImageCompressor");
                        if (ic != null)
                        {
                            this.imageCompressor = ic as IImageCompressor;
                            this.imageCompressor.Initialize(compressionMethod);
                        }
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    this.imageCompressor = null;
                }

                return this.schema;
            }

            /// <summary>
            /// Serialize image.
            /// </summary>
            /// <param name="writer">Writer to which to serialize.</param>
            /// <param name="instance">Image instace to serialize.</param>
            /// <param name="context">Serialization context.</param>
            public void Serialize(BufferWriter writer, Image instance, SerializationContext context)
            {
                Serializer.Serialize(writer, compressionMethod, context);
                if (compressionMethod == CompressionMethod.None)
                {
                    Serializer.Serialize(writer, instance.image, context);
                    Serializer.Serialize(writer, instance.width, context);
                    Serializer.Serialize(writer, instance.height, context);
                    Serializer.Serialize(writer, instance.stride, context);
                    Serializer.Serialize(writer, instance.pixelFormat, context);
                }
                else
                {
                    if (this.imageCompressor == null)
                    {
                        throw new Exception("Unable to located compression assembly");
                    }

                    this.imageCompressor.Serialize(writer, instance, context);
                }
            }

            /// <summary>
            /// Prepare target for cloning.
            /// </summary>
            /// <remarks>Called before Clone, to ensure the target is valid.</remarks>
            /// <param name="instance">Image instance from which to clone.</param>
            /// <param name="target">Image into which to clone.</param>
            /// <param name="context">Serialization context.</param>
            public void PrepareCloningTarget(Image instance, ref Image target, SerializationContext context)
            {
                if (target == null ||
                    target.width != instance.width ||
                    target.height != instance.height ||
                    target.pixelFormat != instance.pixelFormat)
                {
                    target?.Dispose();
                    target = new Image(instance.width, instance.height, instance.pixelFormat);
                }
            }

            /// <summary>
            /// Clone image.
            /// </summary>
            /// <param name="instance">Image instance to clone.</param>
            /// <param name="target">Target image into which to clone.</param>
            /// <param name="context">Serialization context.</param>
            public void Clone(Image instance, ref Image target, SerializationContext context)
            {
                Serializer.Clone(instance.image, ref target.image, context);
                Serializer.Clone(instance.width, ref target.width, context);
                Serializer.Clone(instance.height, ref target.height, context);
                Serializer.Clone(instance.stride, ref target.stride, context);
                Serializer.Clone(instance.pixelFormat, ref target.pixelFormat, context);
            }

            /// <summary>
            /// Prepare target for deserialization.
            /// </summary>
            /// <remarks>Called before Deserialize, to ensure the target is valid.</remarks>
            /// <param name="reader">Reader being used.</param>
            /// <param name="target">Target image into which to deserialize.</param>
            /// <param name="context">Serialization context.</param>
            public void PrepareDeserializationTarget(BufferReader reader, ref Image target, SerializationContext context)
            {
                if (target == null)
                {
                    target = (Image)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Image));
                }
            }

            /// <summary>
            /// Deserialize image.
            /// </summary>
            /// <param name="reader">Buffer reader being used.</param>
            /// <param name="target">Target image into which to deserialize.</param>
            /// <param name="context">Serialization context.</param>
            public void Deserialize(BufferReader reader, ref Image target, SerializationContext context)
            {
                CompressionMethod methodOfCompression = CompressionMethod.None;
                if (this.schema.Version >= 4)
                {
                    Serializer.Deserialize(reader, ref methodOfCompression, context);
                }

                if (methodOfCompression == CompressionMethod.None)
                {
                    Serializer.Deserialize(reader, ref target.image, context);
                    Serializer.Deserialize(reader, ref target.width, context);
                    Serializer.Deserialize(reader, ref target.height, context);
                    Serializer.Deserialize(reader, ref target.stride, context);
                    if (this.schema.Version <= 2)
                    {
                        System.Drawing.Imaging.PixelFormat pixFmt = default(System.Drawing.Imaging.PixelFormat);
                        Serializer.Deserialize(reader, ref pixFmt, context);
                        target.pixelFormat = PixelFormatHelper.FromSystemPixelFormat(pixFmt);
                    }
                    else
                    {
                        Serializer.Deserialize(reader, ref target.pixelFormat, context);
                    }
                }
                else
                {
                    this.imageCompressor.Deserialize(reader, ref target, context);
                }
            }

            /// <summary>
            /// Clear image to be reused.
            /// </summary>
            /// <remarks>Called once the object becomes unused and can be reused as a cloning target.</remarks>
            /// <param name="target">Target image to clear.</param>
            /// <param name="context">Serialization context.</param>
            public void Clear(ref Image target, SerializationContext context)
            {
                Serializer.Clear(ref target.image, context);
                Serializer.Clear(ref target.width, context);
                Serializer.Clear(ref target.height, context);
                Serializer.Clear(ref target.stride, context);
                Serializer.Clear(ref target.pixelFormat, context);
            }
        }
    }
}
