// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;

    /// <summary>
    /// Set of static functions for manipulating pixel formats.
    /// </summary>
    internal static class PixelFormatHelper
    {
        /// <summary>
        /// Converts from a system pixel format into a Psi.Imaging pixel format.
        /// </summary>
        /// <param name="pixelFormat">System pixel format to be converted.</param>
        /// <returns>Psi.Imaging pixel format that matches the specified system pixel format.</returns>
        internal static PixelFormat FromSystemPixelFormat(System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                // Note that System.Drawing.Imaging.PixelFormat specifies the colors in reverse order from how they
                // are actually laid out in memory, so:
                //
                // System.Drawing.Imaging.PixelFormat.Format24bppRgb => Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp,
                //
                // and not Microsoft.Psi.Imaging.PixelFormat.RGB_24bpp.
                return PixelFormat.BGR_24bpp;
            }

            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppRgb)
            {
                return PixelFormat.BGRX_32bpp;
            }

            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                return PixelFormat.Gray_8bpp;
            }

            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)
            {
                return PixelFormat.Gray_16bpp;
            }

            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                return PixelFormat.BGRA_32bpp;
            }

            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format64bppArgb)
            {
                return PixelFormat.RGBA_64bpp;
            }

            throw new NotSupportedException($"The {pixelFormat} pixel format is not currently supported by {nameof(Microsoft.Psi.Imaging)}.");
        }

        /// <summary>
        /// Converts from a Psi.Imaging PixelFormat to a System.Drawing.Imaging.PixelFormat.
        /// </summary>
        /// <param name="pixelFormat">Pixel format to convert.</param>
        /// <returns>The system pixel format that corresponds to the Psi.Imaging pixel format.</returns>
        internal static System.Drawing.Imaging.PixelFormat ToSystemPixelFormat(PixelFormat pixelFormat)
        {
            return pixelFormat switch
            {
                PixelFormat.BGR_24bpp => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                PixelFormat.BGRX_32bpp => System.Drawing.Imaging.PixelFormat.Format32bppRgb,
                PixelFormat.Gray_8bpp => System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                PixelFormat.Gray_16bpp => System.Drawing.Imaging.PixelFormat.Format16bppGrayScale,
                PixelFormat.BGRA_32bpp => System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                PixelFormat.RGBA_64bpp => System.Drawing.Imaging.PixelFormat.Format64bppArgb,

                // Note that System.Drawing.Imaging.PixelFormat specifies the colors in reverse order from how they
                // are actually laid out in memory, so while
                //
                // Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                //
                // there is no equivalent System.Drawing.Imaging.PixelFormat for Microsoft.Psi.Imaging.PixelFormat.RGB_24bpp.
                PixelFormat.RGB_24bpp =>
                    throw new InvalidOperationException(
                        $"Cannot convert {nameof(PixelFormat.RGB_24bpp)} pixel format to {nameof(System.Drawing.Imaging.PixelFormat)} " +
                        $"as there is no corresponding value for 24-bit pixels in (rr gg bb) format."),

                PixelFormat.Undefined =>
                    throw new InvalidOperationException(
                        $"Cannot convert {nameof(PixelFormat.Undefined)} pixel format to {nameof(System.Drawing.Imaging.PixelFormat)}."),

                _ => throw new Exception("Unknown pixel format."),
            };
        }

        /// <summary>
        /// Returns number of bytes/pixel for the specified pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to determine number of bytes.</param>
        /// <returns>
        /// Number of bytes in each pixel of the specified format. If the pixel format is undefined,
        /// this method returns 0.
        /// </returns>
        internal static int GetBytesPerPixel(PixelFormat pixelFormat)
        {
            return pixelFormat switch
            {
                PixelFormat.Gray_8bpp => 1,
                PixelFormat.Gray_16bpp => 2,
                PixelFormat.BGR_24bpp or PixelFormat.RGB_24bpp => 3,
                PixelFormat.BGRX_32bpp or PixelFormat.BGRA_32bpp => 4,
                PixelFormat.RGBA_64bpp => 8,
                PixelFormat.Undefined => 0,
                _ => throw new ArgumentException("Unknown pixel format"),
            };
        }

        /// <summary>
        /// Returns number of bits per channel (the bit depth) for the specified pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to determine the bits per channel.</param>
        /// <returns>
        /// Number of bits per channel for the specified format. If the pixel format is undefined,
        /// this method returns 0.
        /// </returns>
        internal static int GetBitsPerChannel(PixelFormat pixelFormat)
        {
            return pixelFormat switch
            {
                PixelFormat.Gray_8bpp or PixelFormat.BGR_24bpp or PixelFormat.BGRX_32bpp or PixelFormat.BGRA_32bpp or PixelFormat.RGB_24bpp => 8,
                PixelFormat.Gray_16bpp or PixelFormat.RGBA_64bpp => 16,
                PixelFormat.Undefined => 0,
                _ => throw new ArgumentException("Unknown pixel format"),
            };
        }
    }
}
