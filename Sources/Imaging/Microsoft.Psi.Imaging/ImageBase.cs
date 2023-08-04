// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading.Tasks;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents an image, stored in unmanaged memory.
    /// </summary>
    /// <remarks>Using this class it is possible as to allocate a new image in unmanaged memory,
    /// as to just wrap provided pointer to unmanaged memory, where an image is stored.</remarks>
    public abstract class ImageBase : IImage, IDisposable
    {
        /// <summary>
        /// Exception message when unexpected pixel format is encountered.
        /// </summary>
        public const string ExceptionDescriptionUnexpectedPixelFormat = "Unexpected pixel format";

        /// <summary>
        /// Exception message when source and destination image sizes don't match.
        /// </summary>
        public const string ExceptionDescriptionSourceDestImageMismatch = "Source and destination images must be the same size";

        private UnmanagedBuffer image;
        private int width;
        private int height;
        private int stride;
        private PixelFormat pixelFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBase"/> class.
        /// </summary>
        /// <param name="image">The unmanaged array containing the image.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public ImageBase(UnmanagedBuffer image, int width, int height, int stride, PixelFormat pixelFormat)
        {
            if (pixelFormat == PixelFormat.Undefined)
            {
                throw new ArgumentException("Cannot create an image with an Undefined pixel format.");
            }

            this.image = image;
            this.width = width;
            this.height = height;
            this.stride = stride;
            this.pixelFormat = pixelFormat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBase"/> class.
        /// </summary>
        /// <param name="imageData">Pointer to image data in unmanaged memory.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public ImageBase(IntPtr imageData, int width, int height, int stride, PixelFormat pixelFormat)
            : this(UnmanagedBuffer.WrapIntPtr(imageData, height * stride), width, height, stride, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBase"/> class.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="stride">Image stride (line size in bytes).</param>
        /// <param name="pixelFormat">Image pixel format.</param>
        /// <remarks><para><note>Using this constructor, make sure all specified image attributes are correct
        /// and correspond to unmanaged memory buffer. If some attributes are specified incorrectly,
        /// this may lead to exceptions working with the unmanaged memory.</note></para></remarks>
        public ImageBase(int width, int height, int stride, PixelFormat pixelFormat)
            : this(UnmanagedBuffer.Allocate(height * stride), width, height, stride, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBase"/> class.
        /// </summary>
        /// <param name="width">Width of image in pixels.</param>
        /// <param name="height">Height of image in pixels.</param>
        /// <param name="pixelFormat">Pixel format.</param>
        public ImageBase(int width, int height, PixelFormat pixelFormat)
            : this(UnmanagedBuffer.Allocate(height * 4 * ((width * pixelFormat.GetBytesPerPixel() + 3) / 4)), width, height, 4 * ((width * pixelFormat.GetBytesPerPixel() + 3) / 4), pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBase"/> class.
        /// </summary>
        /// <param name="bitmapData">Locked bitmap data.</param>
        /// <param name="makeCopy">Indicates whether a copy is made (default is false).</param>
        /// <remarks>
        /// <para>When the <paramref name="makeCopy"/> parameter is false (default), the image simply wraps
        /// the bitmap data. As such, the bitmap data must stay locked for the duration of using the <see cref="ImageBase"/> object.
        /// </para>
        /// <para>If the <paramref name="makeCopy"/> parameter is set to true, a copy of the bitmap
        /// data is made, and the bitmap data can be released right after the <see cref="ImageBase"/> has been constructed.
        /// </para>
        /// </remarks>
        public ImageBase(BitmapData bitmapData, bool makeCopy = false)
        {
            this.image = makeCopy ?
                UnmanagedBuffer.CreateCopyFrom(bitmapData.Scan0, bitmapData.Stride * bitmapData.Height) :
                UnmanagedBuffer.WrapIntPtr(bitmapData.Scan0, bitmapData.Height * bitmapData.Stride);
            this.width = bitmapData.Width;
            this.height = bitmapData.Height;
            this.stride = bitmapData.Stride;
            this.pixelFormat = PixelFormatHelper.FromSystemPixelFormat(bitmapData.PixelFormat);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBase"/> class.
        /// </summary>
        /// <param name="unmanagedBufferSize">The size of the unmanaged buffer that holds the image.</param>
        internal ImageBase(int unmanagedBufferSize)
        {
            this.image = UnmanagedBuffer.Allocate(unmanagedBufferSize);
            this.width = 0;
            this.height = 0;
            this.stride = 0;
            this.pixelFormat = PixelFormat.Undefined;
        }

        /// <summary>
        /// Gets a pointer to unmanaged buffer that wraps the image data in unmanaged memory.
        /// </summary>
        public UnmanagedBuffer UnmanagedBuffer => this.image;

        /// <summary>
        /// Gets a pointer to the image data in unmanaged memory.
        /// </summary>
        public IntPtr ImageData => this.image.Data;

        /// <inheritdoc />
        public int Width => this.width;

        /// <inheritdoc />
        public int Height => this.height;

        /// <inheritdoc />
        public PixelFormat PixelFormat => this.pixelFormat;

        /// <summary>
        /// Gets the size of the image in bytes (stride times height).
        /// </summary>
        public int Size => this.stride * this.height;

        /// <summary>
        /// Gets image stride (line size in bytes).
        /// </summary>
        public int Stride => this.stride;

        /// <summary>
        /// Gets the bits per pixel in the image.
        /// </summary>
        public int BitsPerPixel => this.pixelFormat.GetBitsPerPixel();

        /// <summary>
        /// Disposes the image.
        /// </summary>
        /// <remarks>Frees unmanaged resources used by the object. The image becomes unusable after that.</remarks>
        public void Dispose()
        {
            this.image.Dispose();
            this.image = null;
        }

        /// <summary>
        /// Copies the image to a destination byte array buffer.
        /// </summary>
        /// <param name="destinationBuffer">The destination buffer to copy the image to.</param>
        /// <remarks>The method copies current unmanaged image to the specified buffer.
        /// The buffer must be allocated and must have the same size.</remarks>
        public void CopyTo(byte[] destinationBuffer)
        {
            this.image.CopyTo(destinationBuffer, destinationBuffer.Length);
        }

        /// <summary>
        /// Copies the image to a destination pointer.
        /// </summary>
        /// <param name="destination">The destination pointer to copy the image to.</param>
        /// <param name="width">The destination width.</param>
        /// <param name="height">The destination height.</param>
        /// <param name="stride">The destination stride.</param>
        /// <param name="pixelFormat">The destination pixel format.</param>
        public void CopyTo(IntPtr destination, int width, int height, int stride, PixelFormat pixelFormat)
        {
            if ((this.width != width) || (this.height != height))
            {
                throw new InvalidOperationException("Destination image has different size.");
            }

            // Check if pixel formats are the same. If so, do a straight up copy
            if (this.PixelFormat == pixelFormat)
            {
                if (this.stride == stride)
                {
                    this.image.CopyTo(destination, stride * height);
                }
                else
                {
                    unsafe
                    {
                        int copyLength = (this.stride < stride) ? this.stride : stride;

                        byte* src = (byte*)this.image.Data.ToPointer();
                        byte* dst = (byte*)destination.ToPointer();

                        // copy line by line
                        for (int i = 0; i < this.height; i++)
                        {
                            Buffer.MemoryCopy(src, dst, copyLength, copyLength);

                            dst += stride;
                            src += this.stride;
                        }
                    }
                }
            }
            else if ((this.pixelFormat == PixelFormat.BGR_24bpp) &&
                     (pixelFormat == PixelFormat.BGRX_32bpp ||
                     pixelFormat == PixelFormat.BGRA_32bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();
                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (stride * i);
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
                     (pixelFormat == PixelFormat.BGR_24bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();
                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (stride * i);
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
                     (pixelFormat == PixelFormat.Gray_8bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();

                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (stride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            *dstCopy++ = Operators.Rgb2Gray(srcCopy[2], srcCopy[1], srcCopy[0]);
                            srcCopy += 3;
                        }
                    });
                }
            }
            else if ((this.pixelFormat == PixelFormat.Gray_16bpp) &&
                     (pixelFormat == PixelFormat.Gray_8bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();

                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (stride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            // copy msb only
                            *dstCopy++ = *(srcCopy + 1);
                            srcCopy += 2;
                        }
                    });
                }
            }
            else if ((this.pixelFormat == PixelFormat.Gray_8bpp) &&
                     (pixelFormat == PixelFormat.Gray_16bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();

                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (stride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            // dest = (src << 8) | src
                            *dstCopy++ = *srcCopy;
                            *dstCopy++ = *srcCopy++;
                        }
                    });
                }
            }
            else if ((this.pixelFormat == PixelFormat.Gray_8bpp) &&
                     (pixelFormat == PixelFormat.BGRA_32bpp))
            {
                unsafe
                {
                    byte* src = (byte*)this.image.Data.ToPointer();
                    byte* dst = (byte*)destination.ToPointer();

                    Parallel.For(0, this.Height, i =>
                    {
                        byte* srcCopy = src + (this.stride * i);
                        byte* dstCopy = dst + (stride * i);
                        for (int j = 0; j < this.width; j++)
                        {
                            // dest = (src << 24) | (src << 16) | (src << 8) | 0xff
                            *dstCopy++ = *srcCopy;
                            *dstCopy++ = *srcCopy;
                            *dstCopy++ = *srcCopy++;
                            *dstCopy++ = 0xff; // alpha
                        }
                    });
                }
            }
            else
            {
                this.CopyImageSlow(this.image.Data, this.pixelFormat, destination, stride, pixelFormat);
            }
        }

        /// <summary>
        /// Copies data from a source byte array buffer into the image.
        /// </summary>
        /// <param name="sourceBuffer">The buffer to copy the image from.</param>
        /// <remarks>The method copies data from the specified buffer into the image.
        /// The image must be allocated and must have the same size.</remarks>
        public void CopyFrom(byte[] sourceBuffer)
        {
            this.image.CopyFrom(sourceBuffer);
        }

        /// <summary>
        /// Copies data from a source byte array buffer into the image.
        /// </summary>
        /// <param name="sourceBuffer">The buffer to copy the image from.</param>
        /// <param name="offset">The zero-based index in the buffer from which to start copying.</param>
        /// <param name="length">The number of bytes to copy.</param>
        /// <remarks>The method copies data from the specified range in the buffer into
        /// the image. The image must be allocated and must have the same size.</remarks>
        public void CopyFrom(byte[] sourceBuffer, int offset, int length)
        {
            this.image.CopyFrom(sourceBuffer, offset, length);
        }

        /// <summary>
        /// Copies data from a source pointer into the image.
        /// </summary>
        /// <param name="source">A source pointer to copy the image data from.</param>
        /// <remarks><para>The method copies data from the specified buffer into the unmanaged image
        /// The image must be allocated and must have the same size.</para></remarks>
        public void CopyFrom(IntPtr source)
        {
            this.image.CopyFrom(source, this.Size);
        }

        /// <summary>
        /// Creates a <see cref="Bitmap"/> from the image.
        /// </summary>
        /// <param name="makeCopy">Indicates whether to make a copy of the image data or not.</param>
        /// <returns>A corresponding <see cref="Bitmap"/> image.</returns>
        /// <remarks>If the <paramref name="makeCopy"/> parameter is set to <see langword="true"/>, then the method
        /// creates a copy of the image, so the <see cref="Bitmap"/> stays valid even when the
        /// image gets disposed. However, setting this parameter to <see langword="false"/> creates a <see cref="Bitmap"/> image which is just a wrapper around the image data. In this case, if the image is disposed, the
        /// will no longer be valid and accessing it will generate an exception.</remarks>
        public Bitmap ToBitmap(bool makeCopy = true)
        {
            Bitmap bitmap = null;

            try
            {
                if (!makeCopy)
                {
                    bitmap = new Bitmap(this.width, this.height, this.stride, PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat), this.image.Data);
                    if (this.pixelFormat == PixelFormat.Gray_8bpp)
                    {
                        Operators.SetGrayscalePalette(bitmap);
                    }
                }
                else
                {
                    // create new image of required format
                    bitmap = new Bitmap(this.width, this.height, PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat));
                    if (this.pixelFormat == PixelFormat.Gray_8bpp)
                    {
                        // set palette to grayscale
                        Operators.SetGrayscalePalette(bitmap);
                    }

                    // lock destination bitmap data
                    BitmapData bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, this.width, this.height),
                        ImageLockMode.ReadWrite,
                        PixelFormatHelper.ToSystemPixelFormat(this.pixelFormat));

                    int bitmapStride = bitmapData.Stride;
                    int lineSize = Math.Min(this.stride, bitmapStride);

                    unsafe
                    {
                        byte* dst = (byte*)bitmapData.Scan0.ToPointer();
                        byte* src = (byte*)this.image.Data.ToPointer();

                        if (this.stride != bitmapStride)
                        {
                            // copy image
                            for (int y = 0; y < this.height; y++)
                            {
                                Buffer.MemoryCopy(src, dst, lineSize, lineSize);
                                dst += bitmapStride;
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
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            catch (Exception)
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }

                throw new Exception("The image has some invalid properties, which caused a failure while converting it to managed image.");
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
        /// Creates an empty image of the same size.
        /// </summary>
        /// <returns>An empty image of the same size.</returns>
        public abstract ImageBase CreateEmptyOfSameSize();

        /// <summary>
        /// Initialize an image that has been constructed from just a buffer.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="pixelFormat">The pixel format for the image.</param>
        internal void Initialize(int width, int height, PixelFormat pixelFormat)
        {
            this.width = width;
            this.height = height;
            this.stride = 4 * ((width * pixelFormat.GetBytesPerPixel() + 3) / 4);
            this.pixelFormat = pixelFormat;
        }

        private void CopyImageSlow(IntPtr sourceIntPtr, PixelFormat sourceFormat, IntPtr destinationIntPtr, int destinationStride, PixelFormat destinationFormat)
        {
            unsafe
            {
                int srcBytesPerPixel = sourceFormat.GetBytesPerPixel();
                int dstBytesPerPixel = destinationFormat.GetBytesPerPixel();
                Parallel.For(
                    0,
                    this.Height,
                    i =>
                    {
                        byte* srcCol = (byte*)sourceIntPtr.ToPointer() + (i * this.stride);
                        byte* dstCol = (byte*)destinationIntPtr.ToPointer() + (i * destinationStride);
                        for (int j = 0; j < this.width; j++)
                        {
                            int red;
                            int green;
                            int blue;
                            int alpha;
                            int bits;
                            switch (sourceFormat)
                            {
                                case PixelFormat.Gray_8bpp:
                                    red = green = blue = srcCol[0];
                                    alpha = 255;
                                    bits = 8;
                                    break;

                                case PixelFormat.Gray_16bpp:
                                    red = green = blue = ((ushort*)srcCol)[0];
                                    alpha = 65535;
                                    bits = 16;
                                    break;

                                case PixelFormat.BGR_24bpp:
                                case PixelFormat.BGRX_32bpp:
                                    blue = srcCol[0];
                                    green = srcCol[1];
                                    red = srcCol[2];
                                    alpha = 255;
                                    bits = 8;
                                    break;

                                case PixelFormat.BGRA_32bpp:
                                    blue = srcCol[0];
                                    green = srcCol[1];
                                    red = srcCol[2];
                                    alpha = srcCol[3];
                                    bits = 8;
                                    break;

                                case PixelFormat.RGB_24bpp:
                                    red = srcCol[0];
                                    green = srcCol[1];
                                    blue = srcCol[2];
                                    alpha = 255;
                                    bits = 8;
                                    break;

                                case PixelFormat.RGBA_64bpp:
                                    red = ((ushort*)srcCol)[0];
                                    green = ((ushort*)srcCol)[1];
                                    blue = ((ushort*)srcCol)[2];
                                    alpha = ((ushort*)srcCol)[3];
                                    bits = 16;
                                    break;

                                case PixelFormat.Undefined:
                                default:
                                    throw new ArgumentException(ExceptionDescriptionUnexpectedPixelFormat);
                            }

                            // Convert from 8-bits-per-channel (0-255) to 16-bits-per-channel (0-65535) and visa versa when needed.
                            switch (destinationFormat)
                            {
                                case PixelFormat.Gray_8bpp:
                                case PixelFormat.BGR_24bpp:
                                case PixelFormat.BGRX_32bpp:
                                case PixelFormat.BGRA_32bpp:
                                case PixelFormat.RGB_24bpp:
                                    if (bits == 16)
                                    {
                                        red >>= 8;
                                        green >>= 8;
                                        blue >>= 8;
                                        alpha >>= 8;
                                    }

                                    break;

                                case PixelFormat.Gray_16bpp:
                                case PixelFormat.RGBA_64bpp:
                                    if (bits == 8)
                                    {
                                        red = (red << 8) | red;
                                        green = (green << 8) | green;
                                        blue = (blue << 8) | blue;
                                        alpha = (alpha << 8) | alpha;
                                    }

                                    break;
                            }

                            switch (destinationFormat)
                            {
                                case PixelFormat.Gray_8bpp:
                                    dstCol[0] = Operators.Rgb2Gray((byte)red, (byte)green, (byte)blue);
                                    break;

                                case PixelFormat.Gray_16bpp:
                                    ((ushort*)dstCol)[0] = Operators.Rgb2Gray((ushort)red, (ushort)green, (ushort)blue);
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

                                case PixelFormat.RGB_24bpp:
                                    dstCol[0] = (byte)red;
                                    dstCol[1] = (byte)green;
                                    dstCol[2] = (byte)blue;
                                    break;

                                case PixelFormat.RGBA_64bpp:
                                    ((ushort*)dstCol)[0] = (ushort)red;
                                    ((ushort*)dstCol)[1] = (ushort)green;
                                    ((ushort*)dstCol)[2] = (ushort)blue;
                                    ((ushort*)dstCol)[3] = (ushort)alpha;
                                    break;

                                case PixelFormat.Undefined:
                                default:
                                    throw new ArgumentException(ExceptionDescriptionUnexpectedPixelFormat);
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
        /// <typeparam name="TImage">The type of image to custom serialize.</typeparam>
        public abstract class CustomSerializer<TImage> : ISerializer<TImage>
            where TImage : ImageBase
        {
            /// <summary>
            /// Gets the schema version for custom image serialization.
            /// </summary>
            protected const int LatestSchemaVersion = 5;

            /// <inheritdoc />
            public bool? IsClearRequired => true;

            /// <summary>
            /// Gets or sets the type schema.
            /// </summary>
            protected TypeSchema Schema { get; set; }

            /// <summary>
            /// Initialize custom serializer.
            /// </summary>
            /// <param name="serializers">Known serializers.</param>
            /// <param name="targetSchema">Target type schema.</param>
            /// <returns>Type schema.</returns>
            public virtual TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
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
                        new TypeMemberSchema("pixelFormat", typeof(PixelFormat).AssemblyQualifiedName, true),
                    };
                    var type = typeof(TImage);
                    var name = TypeSchema.GetContractName(type, serializers.RuntimeInfo.SerializationSystemVersion);
                    this.Schema = new TypeSchema(
                        type.AssemblyQualifiedName,
                        TypeFlags.IsClass,
                        schemaMembers,
                        name,
                        TypeSchema.GetId(name),
                        LatestSchemaVersion,
                        this.GetType().AssemblyQualifiedName,
                        serializers.RuntimeInfo.SerializationSystemVersion);
                }
                else
                {
                    this.Schema = targetSchema;
                }

                return this.Schema;
            }

            /// <summary>
            /// Serialize image.
            /// </summary>
            /// <param name="writer">Writer to which to serialize.</param>
            /// <param name="instance">Image instance to serialize.</param>
            /// <param name="context">Serialization context.</param>
            public virtual void Serialize(BufferWriter writer, TImage instance, SerializationContext context)
            {
                Serializer.Serialize(writer, instance.image, context);
                Serializer.Serialize(writer, instance.width, context);
                Serializer.Serialize(writer, instance.height, context);
                Serializer.Serialize(writer, instance.stride, context);
                Serializer.Serialize(writer, instance.pixelFormat, context);
            }

            /// <summary>
            /// Prepare target for cloning.
            /// </summary>
            /// <remarks>Called before Clone, to ensure the target is valid.</remarks>
            /// <param name="instance">Image instance from which to clone.</param>
            /// <param name="target">Image into which to clone.</param>
            /// <param name="context">Serialization context.</param>
            public virtual void PrepareCloningTarget(TImage instance, ref TImage target, SerializationContext context)
            {
                if (target == null ||
                    target.width != instance.width ||
                    target.height != instance.height ||
                    target.pixelFormat != instance.pixelFormat)
                {
                    target?.Dispose();
                    target = (TImage)instance.CreateEmptyOfSameSize();
                }
            }

            /// <summary>
            /// Clone image.
            /// </summary>
            /// <param name="instance">Image instance to clone.</param>
            /// <param name="target">Target image into which to clone.</param>
            /// <param name="context">Serialization context.</param>
            public virtual void Clone(TImage instance, ref TImage target, SerializationContext context)
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
            public virtual void PrepareDeserializationTarget(BufferReader reader, ref TImage target, SerializationContext context)
            {
                target ??= (TImage)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TImage));
            }

            /// <summary>
            /// Deserialize image.
            /// </summary>
            /// <param name="reader">Buffer reader being used.</param>
            /// <param name="target">Target image into which to deserialize.</param>
            /// <param name="context">Serialization context.</param>
            public virtual void Deserialize(BufferReader reader, ref TImage target, SerializationContext context)
            {
                Serializer.Deserialize(reader, ref target.image, context);
                Serializer.Deserialize(reader, ref target.width, context);
                Serializer.Deserialize(reader, ref target.height, context);
                Serializer.Deserialize(reader, ref target.stride, context);
                if (this.Schema.Version <= 2)
                {
                    System.Drawing.Imaging.PixelFormat pixFmt = default;
                    Serializer.Deserialize(reader, ref pixFmt, context);
                    target.pixelFormat = PixelFormatHelper.FromSystemPixelFormat(pixFmt);
                }
                else
                {
                    Serializer.Deserialize(reader, ref target.pixelFormat, context);
                }
            }

            /// <summary>
            /// Clear image to be reused.
            /// </summary>
            /// <remarks>Called once the object becomes unused and can be reused as a cloning target.</remarks>
            /// <param name="target">Target image to clear.</param>
            /// <param name="context">Serialization context.</param>
            public virtual void Clear(ref TImage target, SerializationContext context)
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
