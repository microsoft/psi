// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements a visualization object for lists of <see cref="Line2D"/>.
    /// </summary>
    [VisualizationObject("2D Lines")]
    public class Line2DListVisualizationObject : XYValueEnumerableVisualizationObject<Line2D?, List<Line2D?>>
    {
        /// <summary>
        /// The color of the line to draw.
        /// </summary>
        private Color color = Colors.DodgerBlue;

        /// <summary>
        /// Stroke thickness to draw data stream with.
        /// </summary>
        private double lineWidth = 1;

        /// <summary>
        /// Gets or sets the line color.
        /// </summary>
        [DataMember]
        [DisplayName("Color")]
        [Description("The line color.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the line width.
        /// </summary>
        [DataMember]
        [DisplayName("Line Width")]
        [Description("The width of the line in pixels.")]
        public double LineWidth
        {
            get { return this.lineWidth; }
            set { this.Set(nameof(this.LineWidth), ref this.lineWidth, value); }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(Line2DListVisualizationObjectView));
    }
}
