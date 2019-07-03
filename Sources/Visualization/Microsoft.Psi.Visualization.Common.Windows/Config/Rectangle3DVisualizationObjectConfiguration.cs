// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a scatter Rect3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Rectangle3DVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private Color color = Colors.White;
        private double thickness = 15;
        private double opacity = 100;

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
        /// Gets or sets the thickness.
        /// </summary>
        [DataMember]
        public double Thickness
        {
            get { return this.thickness; }
            set { this.Set(nameof(this.Thickness), ref this.thickness, value); }
        }

        /// <summary>
        /// Gets or sets the line opacity.
        /// </summary>
        [DataMember]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }
    }
}
