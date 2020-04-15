// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Provides a way to apply custom logic to a <see cref="MultiBinding"/>.
    /// Specifically, converting from two <see cref="DateTime"/> values to a <see cref="TimeSpan"/> string.
    /// </summary>
    public class TimeSpanConverter : IMultiValueConverter
    {
        /// <inheritdoc />
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is DateTime && values[1] is DateTime)
            {
                DateTime start = (DateTime)values[0];
                DateTime end = (DateTime)values[1];
                TimeSpan timespan = end - start;
                return timespan.ToString("hh\\:mm\\:ss\\.ffff");
            }

            return "Invaid TimeSpan";
        }

        /// <inheritdoc />
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
