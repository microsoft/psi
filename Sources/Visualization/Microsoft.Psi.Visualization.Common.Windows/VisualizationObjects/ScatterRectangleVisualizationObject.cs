// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a scatter rectangle visualization object.
    /// </summary>
    [VisualizationObject("Visualize Scatter Rectangles")]
    public class ScatterRectangleVisualizationObject : Instant2DVisualizationObject<List<Tuple<System.Drawing.Rectangle, string>>>
    {
        /// <summary>
        /// Height of rectangle.
        /// </summary>
        private float height = 1080;

        /// <summary>
        /// The color of the line to draw.
        /// </summary>
        private Color color = Color.FromArgb(255, 70, 85, 198);

        /// <summary>
        /// Stroke thickness to draw data stream with.
        /// </summary>
        private double lineWidth = 1;

        /// <summary>
        /// Width of rectangle.
        /// </summary>
        private float width = 1920;

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
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
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

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(ScatterRectangleVisualizationObjectView));
    }
}
