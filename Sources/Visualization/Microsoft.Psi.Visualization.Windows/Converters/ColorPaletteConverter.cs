// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    /// <summary>
    /// Implements a converter from a color palette to a color, where the converter parameter specifies an
    /// index in the palette.
    /// </summary>
    [ValueConversion(typeof(Color[]), typeof(Color))]
    public class ColorPaletteConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is Color[] colors)
            {
                if (parameter is int index)
                {
                    if (targetType == typeof(Color))
                    {
                        return colors[index % colors.Length];
                    }
                    else if (targetType == typeof(Brush))
                    {
                        return new SolidColorBrush(colors[index % colors.Length]);
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported target type", "targetType");
                    }
                }
                else
                {
                    throw new ArgumentException("Unsupported parameter type", "parameter");
                }
            }
            else
            {
                throw new ArgumentException("Unsupported value type", "value");
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
