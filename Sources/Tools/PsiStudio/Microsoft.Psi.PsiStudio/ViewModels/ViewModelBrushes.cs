// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System.Windows.Media;

    /// <summary>
    /// Brushes used by the tree view models.
    /// </summary>
    internal static class ViewModelBrushes
    {
        /// <summary>
        /// Gets the standard color brush.
        /// </summary>
        internal static Brush StandardBrush { get; } = new SolidColorBrush(Colors.Gray);

        /// <summary>
        /// Gets the selected color brush.
        /// </summary>
        internal static Brush SelectedBrush { get; } = new SolidColorBrush(Colors.White);
    }
}
