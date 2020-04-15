// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    /// <summary>
    /// Enumerates the names of Drag and Drop data objects.
    /// </summary>
    public class DragDropDataName
    {
        /// <summary>
        /// The type of drag operation being performed.
        /// </summary>
        public const string DragDropOperation = nameof(DragDropOperation);

        /// <summary>
        /// The Visualization Panel.
        /// </summary>
        public const string VisualizationPanel = nameof(VisualizationPanel);

        /// <summary>
        /// The Visualization Panel View.
        /// </summary>
        public const string VisualizationPanelView = nameof(VisualizationPanelView);

        /// <summary>
        /// The Mouse's offset from the top of the panel.
        /// </summary>
        public const string MouseOffsetFromTop = nameof(MouseOffsetFromTop);

        /// <summary>
        /// The size of the panel.
        /// </summary>
        public const string PanelSize = nameof(PanelSize);

        /// <summary>
        /// The stream tree node being dragged.
        /// </summary>
        public const string StreamTreeNode = nameof(StreamTreeNode);
    }
}
