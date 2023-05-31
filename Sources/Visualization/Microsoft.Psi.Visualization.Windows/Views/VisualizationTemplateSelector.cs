// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Provides a way to choose datatemplate for visualization objects.
    /// </summary>
    public class VisualizationTemplateSelector : DataTemplateSelector
    {
        /// <inheritdoc />
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item is VisualizationPanel)
            {
                return ((VisualizationPanel)item).DefaultViewTemplate;
            }

            return null;
        }
    }
}
