// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a Kinect bodies 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class KinectBodies3DVisualizationObject : Instant3DVisualizationObject<List<Kinect.KinectBody>, KinectBodies3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KinectBodies3DVisualizationObject"/> class.
        /// </summary>
        public KinectBodies3DVisualizationObject()
        {
            this.Visual3D = new KinectBodiesVisual(this);
        }
    }
}
