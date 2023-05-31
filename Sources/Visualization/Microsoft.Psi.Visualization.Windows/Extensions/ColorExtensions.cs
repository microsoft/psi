// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Extensions
{
    /// <summary>
    /// Extension methods for use with <see cref="System.Drawing.Color"/> instances.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Converts System.Drawing.Color to System.Windows.Media.Color.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The converted color.</returns>
        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts System.Drawing.Color to System.Windows.Media.Brush.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The converted brush.</returns>
        public static System.Windows.Media.Brush ToMediaBrush(this System.Drawing.Color color)
        {
            return new System.Windows.Media.SolidColorBrush(color.ToMediaColor());
        }
    }
}