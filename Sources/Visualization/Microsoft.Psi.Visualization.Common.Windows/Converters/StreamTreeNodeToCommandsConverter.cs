// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Converts a stream tree node item to a context menu.
    /// </summary>
    public class StreamTreeNodeToCommandsConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return VisualizationContext.Instance.GetDatasetStreamMenu(value as StreamTreeNode);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
