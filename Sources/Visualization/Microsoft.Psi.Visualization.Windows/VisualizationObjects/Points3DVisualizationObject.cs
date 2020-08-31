// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a points 3D visualization object.
    /// </summary>
    [VisualizationObject("3D Points")]
    public class Points3DVisualizationObject : Instant3DVisualizationObject<List<Point3D>>
    {
        private Color color = Colors.DarkBlue;
        private double radiusCm = 2;

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
        /// Gets or sets the radius of the points in centimeters.
        /// </summary>
        [DataMember]
        [DisplayName("Radius (cm)")]
        [Description("The radius of the points in centimeters.")]
        public double RadiusCm
        {
            get { return this.radiusCm; }
            set { this.Set(nameof(this.RadiusCm), ref this.radiusCm, value); }
        }
    }
}
