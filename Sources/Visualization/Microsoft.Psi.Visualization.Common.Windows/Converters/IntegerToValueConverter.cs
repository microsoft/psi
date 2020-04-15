// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from an integer to a boolean.
    /// </summary>
    /// <typeparam name="T">The type of to convert an integer to.</typeparam>
    public class IntegerToValueConverter<T> : IValueConverter
    {
        /// <summary>
        /// Gets or sets the negative conversion value.
        /// </summary>
        public T NegativeValue { get; set; }

        /// <summary>
        /// Gets or sets the zero conversion value.
        /// </summary>
        public T ZeroValue { get; set; }

        /// <summary>
        /// Gets or sets the positive conversion value.
        /// </summary>
        public T PositiveValue { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return DependencyProperty.UnsetValue;
            }

            int val = (int)value;

            if (val < 0)
            {
                return this.NegativeValue;
            }
            else if (val > 0)
            {
                return this.PositiveValue;
            }
            else
            {
                return this.ZeroValue;
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
