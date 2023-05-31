// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.Extensions;

    /// <summary>
    /// Converts item depth to thickness.
    /// </summary>
    public class LeftMarginMultiplierConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets length.
        /// </summary>
        public double Length { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as TreeViewItem;
            if (item == null)
            {
                return default(Thickness);
            }

            return new Thickness(this.Length * item.GetDepth(), 0, 0, 0);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
