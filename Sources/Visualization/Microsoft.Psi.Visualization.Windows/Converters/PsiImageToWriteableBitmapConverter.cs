// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from a <see cref="Image"/> to a <see cref="WriteableBitmap"/>.
    /// </summary>
    public class PsiImageToWriteableBitmapConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            WriteableBitmap bitmap = null;
            if (value is Image psiImage)
            {
                System.Windows.Media.PixelFormat pixelFormat;
                switch (psiImage.PixelFormat)
                {
                    case Imaging.PixelFormat.Gray_8bpp:
                        pixelFormat = PixelFormats.Gray8;
                        break;

                    case Imaging.PixelFormat.Gray_16bpp:
                        pixelFormat = PixelFormats.Gray16;
                        break;

                    case Imaging.PixelFormat.BGR_24bpp:
                        pixelFormat = PixelFormats.Bgr24;
                        break;

                    case Imaging.PixelFormat.BGRX_32bpp:
                        pixelFormat = PixelFormats.Bgr32;
                        break;

                    case Imaging.PixelFormat.BGRA_32bpp:
                        pixelFormat = PixelFormats.Bgra32;
                        break;

                    case Imaging.PixelFormat.RGBA_64bpp:
                        pixelFormat = PixelFormats.Rgba64;
                        break;

                    default:
                        throw new Exception("Unexpected PixelFormat in DisplayImage");
                }

                bitmap = new WriteableBitmap(psiImage.Width, psiImage.Height, 300, 300, pixelFormat, null);
                bitmap.WritePixels(new Int32Rect(0, 0, psiImage.Width, psiImage.Height), psiImage.ImageData, psiImage.Stride * psiImage.Height, psiImage.Stride);
            }

            return bitmap;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
