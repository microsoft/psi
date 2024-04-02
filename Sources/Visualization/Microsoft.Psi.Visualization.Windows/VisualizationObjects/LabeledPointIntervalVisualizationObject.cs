// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements a visualization object for a set of points from a stream interval.
    /// </summary>
    [VisualizationObject("Labeled 2D Point (interval)", typeof(LabeledPointSamplingSummarizer))]
    public class LabeledPointIntervalVisualizationObject : XYIntervalVisualizationObject<Tuple<Point, string, string>>
    {
        private Color fillColor = Colors.Red;
        private Color labelColor = Colors.White;

        private float radius = 1.0f;
        private bool showLabels = false;

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
        public float Radius
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
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(LabeledPointIntervalVisualizationObjectView));

        /// <inheritdoc/>
        protected override void ComputeXYValueRange()
        {
            if ((this.Data != null) && (this.Data.Count > 0))
            {
                this.XValueRange = new ValueRange<double>(this.Data.Min(p => p.Data.Item1.X), this.Data.Max(p => p.Data.Item1.X));
                this.YValueRange = new ValueRange<double>(this.Data.Min(p => p.Data.Item1.Y), this.Data.Max(p => p.Data.Item1.Y));
            }
            else if ((this.SummaryData != null) && (this.SummaryData.Count > 0))
            {
                this.XValueRange = new ValueRange<double>(this.SummaryData.Min(p => p.Value.Item1.X), this.SummaryData.Max(p => p.Value.Item1.X));
                this.YValueRange = new ValueRange<double>(this.SummaryData.Min(p => p.Value.Item1.Y), this.SummaryData.Max(p => p.Value.Item1.Y));
            }
        }
    }
}
