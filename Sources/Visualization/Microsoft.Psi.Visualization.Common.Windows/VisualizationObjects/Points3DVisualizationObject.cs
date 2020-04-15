// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a points 3D visualization object.
    /// </summary>
    [VisualizationObject("Visualize 3D Points")]
    public class Points3DVisualizationObject : Instant3DVisualizationObject<List<Point3D>>
    {
        private Color color = Colors.DarkBlue;
        private double size = 0.05;

        /// <summary>
        /// Initializes a new instance of the <see cref="Points3DVisualizationObject"/> class.
        /// </summary>
        public Points3DVisualizationObject()
        {
            this.Visual3D = new PointsVisual(this);
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

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [DataMember]
        public double Size
        {
            get { return this.size; }
            set { this.Set(nameof(this.Size), ref this.size, value); }
        }
    }
}
