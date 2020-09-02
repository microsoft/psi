// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System.Windows;

    /// <summary>
    /// Helper methods for doing drag and drop.
    /// </summary>
    internal static class DragDropHelper
    {
        /// <summary>
        /// Determines if the mouse pointer is near the bottom edge of a Visualization Panel
        /// Useful when deciding if we should initiate a drag and drop operation or let the
        /// panel's bottom edge thum perform a resize operation.
        /// </summary>
        /// <param name="mousePosition">The current mouse position relative to the panel.</param>
        /// <param name="panelHeight">The actual height of the panel.</param>
        /// <returns>TRUE if the mouse is near the bottom edge of the panel.</returns>
        internal static bool MouseNearPanelBottomEdge(Point mousePosition, double panelHeight)
        {
            return mousePosition.Y > panelHeight - 4;
        }
    }
}
