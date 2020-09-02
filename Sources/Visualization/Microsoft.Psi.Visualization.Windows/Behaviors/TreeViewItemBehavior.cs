// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Behaviors
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Behavior for a tree view item.
    /// </summary>
    public static class TreeViewItemBehavior
    {
        /// <summary>
        /// The ensure visible when selected property.
        /// </summary>
        public static readonly DependencyProperty EnsureVisibleWhenSelectedProperty =
            DependencyProperty.RegisterAttached(
                "EnsureVisibleWhenSelected",
                typeof(bool),
                typeof(TreeViewItemBehavior),
                new UIPropertyMetadata(false, OnEnsureVisibleWhenSelectedPropertyChanged));

        /// <summary>
        /// Gets the ensure visible when selected flag.
        /// </summary>
        /// <param name="treeViewItem">The tree view item.</param>
        /// <returns>True if the value is set, otherwise false.</returns>
        public static bool GetEnsureVisibleWhenSelected(TreeViewItem treeViewItem)
        {
            return (bool)treeViewItem.GetValue(EnsureVisibleWhenSelectedProperty);
        }

        /// <summary>
        /// Sets the ensure visible when selected flag.
        /// </summary>
        /// <param name="treeViewItem">The tree view item.</param>
        /// <param name="value">The value to set the flag to.</param>
        public static void SetEnsureVisibleWhenSelected(TreeViewItem treeViewItem, bool value)
        {
            treeViewItem.SetValue(EnsureVisibleWhenSelectedProperty, value);
        }

        /// <summary>
        /// Called when the item's selected property changes.
        /// </summary>
        /// <param name="obj">The object that fired the event.</param>
        /// <param name="e">The args for the event.</param>
        public static void OnEnsureVisibleWhenSelectedPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem treeViewItem = obj as TreeViewItem;
            if (treeViewItem != null)
            {
                if (e.NewValue is bool && (bool)e.NewValue)
                {
                    treeViewItem.BringIntoView();
                }
            }
        }
    }
}
