// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a scatter planar direction visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterPlanarDirectionVisualizationObject : Instant3DVisualizationObject<List<CoordinateSystem>, ScatterPlanarDirectionVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterPlanarDirectionVisualizationObject"/> class.
        /// </summary>
        public ScatterPlanarDirectionVisualizationObject()
        {
            this.Visual3D = new ScatterPlanarDirectionVisual(this);
        }
    }
}
