// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Extensions.Data;

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
                if (item is Dataset)
                {
                    return element.FindResource("DatasetTemplate") as DataTemplate;
                }
                else if (item is Session)
                {
                    return element.FindResource("SessionTemplate") as DataTemplate;
                }
                else if (item is IPartition)
                {
                    return element.FindResource("PartitionTemplate") as DataTemplate;
                }
                else if (item is IStreamTreeNode)
                {
                    return element.FindResource("StreamTreeNodeTemplate") as DataTemplate;
                }
            }

            return null;
        }
    }
}