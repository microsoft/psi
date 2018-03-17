// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a points 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Points3DVisualizationObject : Instant3DVisualizationObject<List<Point3D>, Points3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Points3DVisualizationObject"/> class.
        /// </summary>
        public Points3DVisualizationObject()
        {
            this.Visual3D = new PointsVisual(this);
        }
    }
}
