// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from an <see cref="object"/> to a <see cref="PositionData"/> or point dimension if specified,
    /// where a <see cref="TypeConverter"/> exists for the object.
    /// </summary>
    public class ObjectToPositionDataConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(value);
            if (converter.CanConvertTo(typeof(PositionData)))
            {
                PositionData position = (PositionData)converter.ConvertTo(value, typeof(PositionData));
                return position;
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
