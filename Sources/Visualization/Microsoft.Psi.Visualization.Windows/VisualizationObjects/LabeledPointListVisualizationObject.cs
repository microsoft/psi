// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements a visualization object for a list of labeled points.
    /// </summary>
    [VisualizationObject("Labeled Points")]
    public class LabeledPointListVisualizationObject : XYValueEnumerableVisualizationObject<Tuple<Point, string, string>, List<Tuple<Point, string, string>>>
    {
        private Color fillColor = Colors.Red;
        private Color labelColor = Colors.White;

        private int radius = 3;
        private bool showLabels = true;

        /// <summary>
        /// Gets or sets the fill color.
        /// </summary>
        [DataMember]
        public Color FillColor
        {
            get { return this.fillColor; }
            set { this.Set(nameof(this.FillColor), ref this.fillColor, value); }
        }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        [DataMember]
        public Color LabelColor
        {
            get { return this.labelColor; }
            set { this.Set(nameof(this.LabelColor), ref this.labelColor, value); }
        }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        [DataMember]
        public int Radius
        {
            get { return this.radius; }
            set { this.Set(nameof(this.Radius), ref this.radius, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we're showing the labels.
        /// </summary>
        [DataMember]
        public bool ShowLabels
        {
            get { return this.showLabels; }
            set { this.Set(nameof(this.ShowLabels), ref this.showLabels, value); }
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(LabeledPointListVisualizationObjectView));
    }
}
