// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a scatter planar direction visualization object.
    /// </summary>
    [VisualizationObject("Visualize Scatter Planar Data")]
    public class ScatterPlanarDirectionVisualizationObject : Instant3DVisualizationObject<List<CoordinateSystem>>
    {
        private Color color = Colors.DarkGray;
        private double size = 0.5;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterPlanarDirectionVisualizationObject"/> class.
        /// </summary>
        public ScatterPlanarDirectionVisualizationObject()
        {
            this.Visual3D = new ScatterPlanarDirectionVisual(this);
        }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [DataMember]
        public double Size
        {
            get { return this.size; }
            set { this.Set(nameof(this.Size), ref this.size, value); }
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }
    }
}
