// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Implements a converter from a boolean to a double that represents the
    /// UI opacity for an item in the layouts view.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(double))]
    public class BoolToLayoutsOpacityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && parameter == null)
            {
                return (bool)value ? 1 : 0.5;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                throw new ArgumentException();
            }

            return (double)value == 1;
        }
    }
}
