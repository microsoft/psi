// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from numeric value to a scaled offest.
    /// </summary>
    public class PlacementConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the placement offset.
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// Gets or sets the placement scale.
        /// </summary>
        public double Scale { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            if (value is int)
            {
                d = (double)(int)value;
            }
            else
            {
                d = (double)value;
            }

            return (d + this.Offset) * this.Scale;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
