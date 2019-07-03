// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Common
{
    /// <summary>
    /// Enumerates the names of Drag and Drop data objects.
    /// </summary>
    internal class DragDropDataName
    {
        /// <summary>
        /// The type of drag operation being performed.
        /// </summary>
        public const string DragDropOperation = "DragOperation";

        /// <summary>
        /// The Visualization Panel.
        /// </summary>
        public const string VisualizationPanel = "VisualizationPanel";

        /// <summary>
        /// The Visualization Panel View.
        /// </summary>
        public const string VisualizationPanelView = "VisualizationPanelView";

        /// <summary>
        /// The Mouse's offset from the top of the panel.
        /// </summary>
        public const string MouseOffsetFromTop = "MouseOffsetFromTop";

        /// <summary>
        /// The size of the panel.
        /// </summary>
        public const string PanelSize = "PanelSize";

        /// <summary>
        /// The stream tree node being dragged.
        /// </summary>
        public const string StreamTreeNode = "StreamTreeNode";
    }
}
