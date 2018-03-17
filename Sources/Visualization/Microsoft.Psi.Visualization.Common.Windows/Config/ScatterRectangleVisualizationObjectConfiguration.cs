// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a scatter rectangle visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterRectangleVisualizationObjectConfiguration : InstantVisualizationObjectConfiguration
    {
        /// <summary>
        /// Height of rectangle.
        /// </summary>
        private float height;

        /// <summary>
        /// The color of the line to draw.
        /// </summary>
        private System.Windows.Media.Color lineColor;

        /// <summary>
        /// Stroke thickness to draw data stream with.
        /// </summary>
        private double lineWidth;

        /// <summary>
        /// Width of rectangle.
        /// </summary>
        private float width;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [DataMember]
        public float Height
        {
            get { return this.height; }
            set { this.Set(nameof(this.Height), ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the line color.
        /// </summary>
        [DataMember]
        public System.Windows.Media.Color LineColor
        {
            get { return this.lineColor; }
            set { this.Set(nameof(this.LineColor), ref this.lineColor, value); }
        }

        /// <summary>
        /// Gets or sets the line width.
        /// </summary>
        [DataMember]
        public double LineWidth
        {
            get { return this.lineWidth; }
            set { this.Set(nameof(this.LineWidth), ref this.lineWidth, value); }
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [DataMember]
        public float Width
        {
            get { return this.width; }
            set { this.Set(nameof(this.Width), ref this.width, value); }
        }
    }
}
