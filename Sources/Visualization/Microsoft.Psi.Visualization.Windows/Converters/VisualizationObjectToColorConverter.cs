// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides a way to apply custom logic to a binding.
    /// Specifically, converting from a <see cref="ScatterRectangleVisualizationObject"/> to a <see cref="SolidColorBrush"/>.
    /// </summary>
    public class VisualizationObjectToColorConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var itemsControl = value as ItemsControl;
            if (itemsControl == null)
            {
                throw new ArgumentException("Expected type ItemsControl for parameter: value");
            }

            var visualizationObject = itemsControl.DataContext as ScatterRectangleVisualizationObject;
            if (visualizationObject == null)
            {
                throw new ArgumentException("Unexpected value for control's DataContext.");
            }

            return new SolidColorBrush(visualizationObject.Color);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
