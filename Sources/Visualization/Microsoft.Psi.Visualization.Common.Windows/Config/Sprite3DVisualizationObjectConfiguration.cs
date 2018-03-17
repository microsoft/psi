// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a sprite 3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Sprite3DVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private string source;
        private Point3D[] vertexPositions;

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
