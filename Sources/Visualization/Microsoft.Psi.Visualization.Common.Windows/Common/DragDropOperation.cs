// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    /// <summary>
    /// The various drag operations that can be performed.
    /// </summary>
    public class DragDropOperation
    {
        /// <summary>
        /// A panel in the Visualization Container View is being dragged to reorder it.
        /// </summary>
        public const string ReorderPanel = "ReorderPanel";

        /// <summary>
        /// A stream is being dragged from the tree view into the Visualization Container.
        /// </summary>
        public const string DragDropStream = "DragDropStream";
    }
}
