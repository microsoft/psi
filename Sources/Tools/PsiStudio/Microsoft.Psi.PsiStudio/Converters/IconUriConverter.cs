// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.Common;

    /// <summary>
    /// Converts icon names to the equivalent pack uri icon path in visualization common.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class IconUriConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string stringParameter)
            {
                return IconSourcePath.IconPrefix + stringParameter;
            }

            return null;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
