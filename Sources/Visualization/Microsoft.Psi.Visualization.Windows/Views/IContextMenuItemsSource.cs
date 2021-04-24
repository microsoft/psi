// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents an object that is capable of supplying context menu items.
    /// </summary>
    public interface IContextMenuItemsSource
    {
        /// <summary>
        /// Gets the context menu items source type.
        /// </summary>
        ContextMenuItemsSourceType ContextMenuItemsSourceType { get; }

        /// <summary>
        /// Gets the name of the context menu items source.
        /// </summary>
        string ContextMenuObjectName { get; }

        /// <summary>
        /// Gets the context menu items the context menu source wishes to display.
        /// </summary>
        /// <param name="menuItems">A collection of menu items to append to.</param>
        void AppendContextMenuItems(List<MenuItem> menuItems);
    }
}
