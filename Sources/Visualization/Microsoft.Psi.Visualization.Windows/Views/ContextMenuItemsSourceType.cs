// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    /// <summary>
    /// The type of a context menu source.
    /// </summary>
    public enum ContextMenuItemsSourceType
    {
        /// <summary>
        /// The context menu source is a visualization object.
        /// </summary>
        VisualizationObject = 1,

        /// <summary>
        /// The context menu source is a visualization panel.
        /// </summary>
        VisualizationPanel = 2,

        /// <summary>
        /// The context menu source is a visualization panel matrix container.
        /// </summary>
        VisualizationPanelMatrixContainer = 3,

        /// <summary>
        /// The context menu source is a visualization container.
        /// </summary>
        VisualizationContainer = 4,
    }
}
