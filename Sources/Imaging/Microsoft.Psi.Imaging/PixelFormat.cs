// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

namespace Microsoft.Psi.Imaging
{
    using System;

    /// <summary>
    /// PixelFormat defines.
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>
        /// Used when the pixel format isn't defined.
        /// </summary>
        Undefined,

        /// <summary>
        /// Defines an grayscale image where each pixel is 8 bits.
        /// </summary>
        Gray_8bpp,

        /// <summary>
        /// Defines an grayscale image where each pixel is 16 bits.
        /// </summary>
        Gray_16bpp,

        /// <summary>
        /// Defines an color image format where each red/green/blue component is 8 bits.
        /// The byte order in memory is: bb gg rr.
        /// </summary>
        BGR_24bpp,

        /// <summary>
        /// Defines an color image format where each red/green/blue component is 8 bits.
        /// The byte order in memory is: bb gg rr xx.
        /// </summary>
        BGRX_32bpp,

        /// <summary>
        /// Defines an color image format where each red/green/blue/alpha component is 8 bits.
        /// The byte order in memory is: bb gg rr aa.
        /// </summary>
        BGRA_32bpp,

        /// <summary>
        /// Defines an color image format where each red/green/blue/alpha component is 16 bits.
        /// The byte order in memory is: rrrr gggg bbbb aaaa.
        /// </summary>
        RGBA_64bpp,
    }

    /// <summary>
    /// Defines a set of extensions for getting info about a PixelFormat.
    /// </summary>
    public static class PixelFormatExtensions
    {
        /// <summary>
        /// Returns the number of bits per pixel for a given pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to find bits per pixel.</param>
        /// <returns>Number of bits per pixel for the given pixel format.</returns>
        public static int GetBitsPerPixel(this PixelFormat pixelFormat)
        {
            return PixelFormatHelper.GetBytesPerPixel(pixelFormat) * 8;
        }

        /// <summary>
        /// Returns the number of bytes per pixel for a given pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to find bytes per pixel.</param>
        /// <returns>Number of bytes per pixel for the given pixel format.</returns>
        public static int GetBytesPerPixel(this PixelFormat pixelFormat)
        {
            return PixelFormatHelper.GetBytesPerPixel(pixelFormat);
        }
    }

    /// <summary>
    /// Set of static functions for manipulating pixel formats.
    /// </summary>
    public static class PixelFormatHelper
    {
        /// <summary>
        /// Converts from a system pixel format into a Psi.Imaging pixel format.
        /// </summary>
        /// <param name="pf">System pixel format to be converted.</param>
        /// <returns>Psi.Imaging pixel format that matches the specified system pixel format.</returns>
        public static PixelFormat FromSystemPixelFormat(System.Drawing.Imaging.PixelFormat pf)
        {
            if (pf == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                return PixelFormat.BGR_24bpp;
            }

            if (pf == System.Drawing.Imaging.PixelFormat.Format32bppRgb)
            {
                return PixelFormat.BGRX_32bpp;
            }

            if (pf == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                return PixelFormat.Gray_8bpp;
            }

            if (pf == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)
            {
                return PixelFormat.Gray_16bpp;
            }

            if (pf == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                return PixelFormat.BGRA_32bpp;
            }

            if (pf == System.Drawing.Imaging.PixelFormat.Format64bppArgb)
            {
                return PixelFormat.RGBA_64bpp;
            }

            throw new Exception("Unsupported pixel format");
        }

        /// <summary>
        /// Converts from a Psi.Imaging PixelFormat to a System.Drawing.Imaging.PixelFormat.
        /// </summary>
        /// <param name="pf">Pixel format to convert.</param>
        /// <returns>The system pixel format that corresponds to the Psi.Imaging pixel format.</returns>
        public static System.Drawing.Imaging.PixelFormat ToSystemPixelFormat(PixelFormat pf)
        {
            switch (pf)
            {
                case PixelFormat.BGR_24bpp:
                    return System.Drawing.Imaging.PixelFormat.Format24bppRgb;

                case PixelFormat.BGRX_32bpp:
                    return System.Drawing.Imaging.PixelFormat.Format32bppRgb;

                case PixelFormat.Gray_8bpp:
                    return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

                case PixelFormat.Gray_16bpp:
                    return System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;

                case PixelFormat.BGRA_32bpp:
                    return System.Drawing.Imaging.PixelFormat.Format32bppArgb;

                case PixelFormat.RGBA_64bpp:
                    return System.Drawing.Imaging.PixelFormat.Format64bppArgb;

                default:
                    throw new Exception("Unknown pixel format?!");
            }
        }

        /// <summary>
        /// Returns number of bits/pixel for the specified pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to detemine number of bits/pxiel.</param>
        /// <returns>Number of bits per pixel in specified format.</returns>
        public static int GetBitsPerPixel(PixelFormat pixelFormat)
        {
            return GetBytesPerPixel(pixelFormat) * 8;
        }

        /// <summary>
        /// Returns number of bytes/pixel for the specified pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to determine number of bytes.</param>
        /// <returns>Number of bytes in each pixel of the specified format.</returns>
        public static int GetBytesPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Gray_8bpp:
                    return 1;

                case PixelFormat.Gray_16bpp:
                    return 2;

                case PixelFormat.BGR_24bpp:
                    return 3;

                case PixelFormat.BGRX_32bpp:
                case PixelFormat.BGRA_32bpp:
                    return 4;

                case PixelFormat.RGBA_64bpp:
                    return 8;

                case PixelFormat.Undefined:
                    return 0;

                default:
                    throw new Exception("Unknown pixel format");
            }
        }
    }
}
