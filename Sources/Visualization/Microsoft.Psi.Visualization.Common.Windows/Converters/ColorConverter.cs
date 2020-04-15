// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from a color to a <see cref="SolidColorBrush"/>.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class ColorConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Color color;
            if (value is System.Drawing.Color)
            {
                var colorIn = (System.Drawing.Color)value;
                color = Color.FromArgb(colorIn.A, colorIn.R, colorIn.G, colorIn.B);
            }
            else
            {
                color = (Color)value;
            }

            if (targetType == typeof(Color))
            {
                return color;
            }
            else if (targetType == typeof(Brush))
            {
                return new SolidColorBrush(color);
            }
            else
            {
                throw new ArgumentException("Unsupported target type", "targetType");
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
