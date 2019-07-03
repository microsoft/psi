// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    /// <summary>
    /// Defines various plot types.
    /// </summary>
    public enum InterpolationStyle
    {
        /// <summary>
        /// Adjacent points in the plot are joined directly by lines
        /// </summary>
        Direct,

        /// <summary>
        /// Adjacent points in the plot are joined by horizontal and vertical lines
        /// </summary>
        Step,

        /// <summary>
        /// No lines are rendered between adjacent points
        /// </summary>
        None,
    }
}
