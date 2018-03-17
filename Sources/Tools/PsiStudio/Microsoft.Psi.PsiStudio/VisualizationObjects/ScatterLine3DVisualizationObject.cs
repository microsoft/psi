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
    /// Represents a scatter line 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterLine3DVisualizationObject : Instant3DVisualizationObject<List<Line3D>, ScatterLine3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterLine3DVisualizationObject"/> class.
        /// </summary>
        public ScatterLine3DVisualizationObject()
        {
            this.Visual3D = new ScatterLine3DVisual(this);
        }
    }
}
