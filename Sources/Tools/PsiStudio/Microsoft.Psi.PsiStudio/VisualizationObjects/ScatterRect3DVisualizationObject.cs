// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows.Media.Media3D;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a scatter Rect3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterRect3DVisualizationObject : Instant3DVisualizationObject<List<(CoordinateSystem, Rect3D)>, ScatterRect3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterRect3DVisualizationObject"/> class.
        /// </summary>
        public ScatterRect3DVisualizationObject()
        {
            this.Visual3D = new ScatterRect3DVisual(this);
        }
    }
}
