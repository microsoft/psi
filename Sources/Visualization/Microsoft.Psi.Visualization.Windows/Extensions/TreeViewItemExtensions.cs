// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Extensions
{
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Extension methods to make using <see cref="TreeViewItem"/>s easier.
    /// </summary>
    public static class TreeViewItemExtensions
    {
        /// <summary>
        /// Gets the tree depth of a given <see cref="TreeViewItem"/>.
        /// </summary>
        /// <param name="item">Tree view item.</param>
        /// <returns>Depth of the given tree view item.</returns>
        public static int GetDepth(this TreeViewItem item)
        {
            TreeViewItem parent;
            while ((parent = GetParent(item)) != null)
            {
                return GetDepth(parent) + 1;
            }

            return 0;
        }

        private static TreeViewItem GetParent(TreeViewItem item)
        {
            var parent = VisualTreeHelper.GetParent(item);

            while (!(parent is TreeViewItem || parent is TreeView))
            {
                if (parent == null)
                {
                    return null;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as TreeViewItem;
        }
    }
}
