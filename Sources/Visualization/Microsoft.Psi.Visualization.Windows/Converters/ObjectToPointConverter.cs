// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from an <see cref="object"/> to a <see cref="Point"/> or point dimension if specified,
    /// where a <see cref="TypeConverter"/> exists for the object.
    /// </summary>
    public class ObjectToPointConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(value);
            if (converter.CanConvertTo(typeof(Point)))
            {
                Point pt = (Point)converter.ConvertTo(value, typeof(Point));

                string strParam = parameter as string;
                if (strParam == null)
                {
                    return pt;
                }
                else if (strParam.Equals("left", StringComparison.InvariantCultureIgnoreCase))
                {
                    return pt.X;
                }
                else if (strParam.Equals("top", StringComparison.InvariantCultureIgnoreCase))
                {
                    return pt.Y;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
