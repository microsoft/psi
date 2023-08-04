// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an object that is capable of supplying context menu items.
    /// </summary>
    public interface IContextMenuItemsSource
    {
        /// <summary>
        /// Gets the set of context menu items.
        /// </summary>
        /// <returns>
        /// The set of context menu items.
        /// </returns>
        List<ContextMenuItemInfo> ContextMenuItemsInfo();
    }
}
