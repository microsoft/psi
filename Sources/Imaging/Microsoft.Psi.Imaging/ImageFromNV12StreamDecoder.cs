// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;

    /// <summary>
    /// Implements an NV12 image decoder.
    /// </summary>
    public class ImageFromNV12StreamDecoder : IImageFromStreamDecoder
    {
        /// <summary>
        /// Determine whether stream has an NV12 header.
        /// </summary>
        /// <param name="stream">Stream containing image data.</param>
        /// <returns>A value indicating whether the stream has an NV12 header.</returns>
        public bool HasNV12Header(Stream stream)
        {
            var isNV12 = stream.Length >= 4 && stream.ReadByte() == 'N' && stream.ReadByte() == 'V' && stream.ReadByte() == '1' && stream.ReadByte() == '2';
            stream.Position = 0;
            return isNV12;
        }

        /// <inheritdoc/>
        public void DecodeFromStream(Stream stream, Image image)
        {
            if (!this.HasNV12Header(stream))
            {
                throw new ArgumentException("Stream does not appear to be NV12-encoded (missing header).");
            }

            stream.Position = 4; // skip header

            // decode NV12
            var width = image.Width;
            var height = image.Height;
            int startUV = width * height;
            int strideUV = width;

            var size = (int)(width * height * 1.5 + 0.5); // 12-bit/pixel
            using var sharedData = SharedArrayPool<byte>.GetOrCreate(size);
            var data = sharedData.Resource;
            stream.Read(data, 0, size);

            unsafe
            {
                var buffer = (byte*)image.UnmanagedBuffer.Data;
                for (int i = 0; i < height; i++)
                {
                    var p = buffer + (i * 4 * width);
                    int row = i * width;
                    var startUVrow = startUV + ((i / 2) * strideUV);
                    for (int j = 0; j < width; j++)
                    {
                        var y = data[row + j] - 16;
                        var uindex = startUVrow + (2 * (j / 2));
                        var u = data[uindex] - 128;
                        var v = data[uindex + 1] - 128;

                        var yy = 1.164383 * y;
                        var b = yy + (2.017232 * u);
                        var g = yy - (0.812968 * v) - (0.391762 * u);
                        var r = yy + (1.596027 * v);

                        *p++ = (byte)Math.Max(0, Math.Min(255, b + 0.5));
                        *p++ = (byte)Math.Max(0, Math.Min(255, g + 0.5));
                        *p++ = (byte)Math.Max(0, Math.Min(255, r + 0.5));
                        *p++ = 0xff; // alpha
                    }
                }
            }
        }

        /// <inheritdoc/>
        public PixelFormat GetPixelFormat(Stream stream)
        {
            if (!this.HasNV12Header(stream))
            {
                throw new ArgumentException("Stream does not appear to be NV12-encoded (missing header).");
            }

            return PixelFormat.BGRA_32bpp;
        }
    }
}
