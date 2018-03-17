// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a sprite 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Sprite3DVisualizationObject : Instant3DVisualizationObject<CoordinateSystem, Sprite3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sprite3DVisualizationObject"/> class.
        /// </summary>
        public Sprite3DVisualizationObject()
        {
            this.Visual3D = new SpriteVisual(this);
        }
    }
}
