// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.Datasets;

    /// <summary>
    /// Converts stream tree node to visibility.
    /// If stream tree node is a) an actual stream and b) has commands then return Visibility.Visible, else return Visibility.Hidden.
    /// </summary>
    public class StreamTreeNodeToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IStreamTreeNode)
            {
                IStreamTreeNode streamTreeNode = value as IStreamTreeNode;
                if (streamTreeNode.IsStream)
                {
                    var commands = PsiStudioContext.Instance.GetVisualizeStreamCommands(streamTreeNode);
                    if (commands != null && commands.Count > 0)
                    {
                        return Visibility.Visible;
                    }
                }
            }

            return Visibility.Hidden;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
