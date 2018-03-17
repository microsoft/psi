// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    /// <summary>
    /// Denotes the range of the data to be visualized
    /// </summary>
    public enum VisualizationRange
    {
        /// <summary>
        /// The visualization range corresponds to the data at the cursor
        /// </summary>
        Cursor,

        /// <summary>
        /// The visualization range corresponds to the data in the selection
        /// </summary>
        Selection,

        /// <summary>
        /// The visualization range corresponds to the data in the view
        /// </summary>
        View,

        /// <summary>
        /// The visualization range corresponds to the entire stream
        /// </summary>
        Stream
    }
}
