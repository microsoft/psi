// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Defines the various pixel formats supported by the <see cref="Image"/> type.
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
        /// <remarks>
        /// Not to be confused with <see cref="RGB_24bpp"/> whose byte order is: rr gg bb.
        /// </remarks>
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

        /// <summary>
        /// Defines an color image format where each red/green/blue component is 8 bits.
        /// The byte order in memory is: rr gg bb.
        /// </summary>
        /// <remarks>
        /// Not to be confused with <see cref="BGR_24bpp"/> whose byte order is: bb gg rr.
        /// </remarks>
        RGB_24bpp,
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

        /// <summary>
        /// Returns the number of bits per channel (bit depth) for a given pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format for which to find bits per channel.</param>
        /// <returns>Number of bits per channel for the given pixel format.</returns>
        public static int GetBitsPerChannel(this PixelFormat pixelFormat)
        {
            return PixelFormatHelper.GetBitsPerChannel(pixelFormat);
        }

        /// <summary>
        /// Returns the <see cref="Microsoft.Psi.Imaging"/> pixel format correspond to the specified <see cref="System.Drawing.Imaging"/> pixel format.
        /// </summary>
        /// <param name="pixelFormat">The <see cref="System.Drawing.Imaging"/> pixel format.</param>
        /// <returns>The corresponding <see cref="Microsoft.Psi.Imaging"/> pixel format.</returns>
        public static PixelFormat ToPsiPixelFormat(this System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            return PixelFormatHelper.FromSystemPixelFormat(pixelFormat);
        }

        /// <summary>
        /// Returns the <see cref="System.Drawing.Imaging"/> pixel format correspond to the specified <see cref="Microsoft.Psi.Imaging"/> pixel format.
        /// </summary>
        /// <param name="pixelFormat">The <see cref="Microsoft.Psi.Imaging"/> pixel format.</param>
        /// <returns>The corresponding <see cref="System.Drawing.Imaging"/> pixel format.</returns>
        public static System.Drawing.Imaging.PixelFormat ToSystemPixelFormat(this PixelFormat pixelFormat)
        {
            return PixelFormatHelper.ToSystemPixelFormat(pixelFormat);
        }
    }
}
