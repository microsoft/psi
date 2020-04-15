// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a sprite 3D visualization object.
    /// </summary>
    [VisualizationObject("Visualize 3D Sprites")]
    public class Sprite3DVisualizationObject : Instant3DVisualizationObject<CoordinateSystem>
    {
        private string source;
        private Point3D[] vertexPositions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sprite3DVisualizationObject"/> class.
        /// </summary>
        public Sprite3DVisualizationObject()
        {
            this.Visual3D = new SpriteVisual(this);
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        [DataMember]
        public string Source
        {
            get { return this.source; }
            set { this.Set(nameof(this.Source), ref this.source, value); }
        }

        /// <summary>
        /// Gets or sets the vertex positions.
        /// </summary>
        [DataMember]
        public Point3D[] VertexPositions
        {
            get { return this.vertexPositions; }
            set { this.Set(nameof(this.VertexPositions), ref this.vertexPositions, value); }
        }
    }
}
