// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Provides a way to select the appropriate datatemplate based on the data object provided.
    /// </summary>
    public class PsiStudioTemplateSelector : DataTemplateSelector
    {
        /// <inheritdoc />
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            if (element != null && item != null)
            {
                if (item is DatasetViewModel)
                {
                    return element.FindResource("DatasetTemplate") as DataTemplate;
                }
                else if (item is SessionViewModel)
                {
                    return element.FindResource("SessionTemplate") as DataTemplate;
                }
                else if (item is PartitionViewModel)
                {
                    return element.FindResource("PartitionTemplate") as DataTemplate;
                }
                else if (item is StreamTreeNode)
                {
                    return element.FindResource("StreamTreeNodeTemplate") as DataTemplate;
                }
            }

            return null;
        }
    }
}