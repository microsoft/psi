// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    using System;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a position within a <see cref="TimelineVisualizationPanel"/>.
    /// </summary>
    public struct TimelinePanelMousePosition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimelinePanelMousePosition"/> struct.
        /// </summary>
        /// <param name="x">The position along the x (time) axis.</param>
        /// <param name="y">The position along the y (value) axis.</param>
        public TimelinePanelMousePosition(DateTime x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Gets or sets the position along the X (time) axis.
        /// </summary>
        public DateTime X { get; set; }

        /// <summary>
        /// Gets or sets the position along the Y (value) axis.
        /// </summary>
        public double Y { get; set; }
    }
}
