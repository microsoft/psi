// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represnts an animated model 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnimatedModel3DVisualizationObject : Instant3DVisualizationObject<CoordinateSystem, AnimatedModel3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnimatedModel3DVisualizationObject"/> class.
        /// </summary>
        public AnimatedModel3DVisualizationObject()
        {
            this.Visual3D = new AnimatedModelVisual(this);
        }
    }
}
