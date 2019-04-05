// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Converts stream tree node items to commands.
    /// </summary>
    public class StreamTreeNodeToCommandsConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return PsiStudioContext.Instance.GetVisualizeStreamCommands(value as IStreamTreeNode);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
