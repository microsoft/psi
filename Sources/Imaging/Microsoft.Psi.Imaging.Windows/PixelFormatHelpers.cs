// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using MediaPixelFormat = System.Windows.Media.PixelFormat;

    /// <summary>
    /// Implements helper functions for manipulating <see cref="PixelFormat"/> instances.
    /// </summary>
    public static class PixelFormatHelpers
    {
        /// <summary>
        /// Converts from \psi imaging pixel format to Windows.Media pixel format.
        /// </summary>
        /// <param name="pixelFormat">The \psi imaging pixel format.</param>
        /// <returns>The corresponding Windows.Media pixel format.</returns>
        public static MediaPixelFormat ToWindowsMediaPixelFormat(this PixelFormat pixelFormat)
        {
            if (pixelFormat == PixelFormat.Undefined)
            {
                throw new InvalidOperationException("Cannot convert the Undefined pixel format to a Windows.Media format.");
            }
            else if (pixelFormat == PixelFormat.BGR_24bpp)
            {
                return System.Windows.Media.PixelFormats.Bgr24;
            }
            else if (pixelFormat == PixelFormat.Gray_16bpp)
            {
                return System.Windows.Media.PixelFormats.Gray16;
            }
            else if (pixelFormat == PixelFormat.Gray_8bpp)
            {
                return System.Windows.Media.PixelFormats.Gray8;
            }
            else
            {
                return System.Windows.Media.PixelFormats.Bgr32;
            }
        }

        /// <summary>
        /// Converts Windows.Media pixel format to \psi imaging pixel format.
        /// </summary>
        /// <param name="mediaPixelFormat">The Windows.Media pixel format.</param>
        /// <returns>The corresponding \psi imaging pixel format.</returns>
        public static PixelFormat ToPixelFormat(this MediaPixelFormat mediaPixelFormat)
        {
            if (mediaPixelFormat == System.Windows.Media.PixelFormats.Bgr24)
            {
                return PixelFormat.BGR_24bpp;
            }
            else if (mediaPixelFormat == System.Windows.Media.PixelFormats.Gray16)
            {
                return PixelFormat.Gray_16bpp;
            }
            else if (mediaPixelFormat == System.Windows.Media.PixelFormats.Gray8)
            {
                return PixelFormat.Gray_8bpp;
            }
            else if (mediaPixelFormat == System.Windows.Media.PixelFormats.Bgr32)
            {
                return PixelFormat.BGRX_32bpp;
            }
            else if (mediaPixelFormat == System.Windows.Media.PixelFormats.Bgra32)
            {
                return PixelFormat.BGRA_32bpp;
            }
            else if (mediaPixelFormat == System.Windows.Media.PixelFormats.Rgba64)
            {
                return PixelFormat.RGBA_64bpp;
            }
            else
            {
                throw new NotSupportedException($"The {mediaPixelFormat} pixel format is not supported for Microsoft.Psi.Imaging");
            }
        }
    }
}
