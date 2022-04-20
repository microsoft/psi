// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.Visualization
{
    using System.Windows.Media;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for <see cref="Hand"/>.
    /// </summary>
    [VisualizationObject("Hand")]
    public class HandVisualizationObject : Point3DGraphVisualizationObject<HandJointIndex>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HandVisualizationObject"/> class.
        /// </summary>
        public HandVisualizationObject()
        {
            this.EdgeDiameterMm = 10;
            this.NodeRadiusMm = 7;
            this.NodeColor = Colors.Silver;
            this.EdgeColor = Colors.Gray;
        }
    }
}
