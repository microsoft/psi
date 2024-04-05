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
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements a visualization object for a set of line segments from a stream interval.
    /// </summary>
    [VisualizationObject("2D Line (interval)", typeof(Line2DSamplingSummarizer))]
    public class Line2DIntervalVisualizationObject : XYIntervalVisualizationObject<Line2D?>
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
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(Line2DIntervalVisualizationObjectView));

        /// <inheritdoc/>
        protected override void ComputeXYValueRange()
        {
            if ((this.Data != null) && this.Data.Any(m => m.Data.HasValue))
            {
                this.XValueRange = new ValueRange<double>(
                    this.Data.Where(m => m.Data.HasValue).Min(p => Math.Min(p.Data.Value.StartPoint.X, p.Data.Value.EndPoint.X)),
                    this.Data.Where(m => m.Data.HasValue).Max(p => Math.Max(p.Data.Value.StartPoint.X, p.Data.Value.EndPoint.X)));
                this.YValueRange = new ValueRange<double>(
                    this.Data.Where(m => m.Data.HasValue).Min(p => Math.Min(p.Data.Value.StartPoint.Y, p.Data.Value.EndPoint.Y)),
                    this.Data.Where(m => m.Data.HasValue).Max(p => Math.Max(p.Data.Value.StartPoint.Y, p.Data.Value.EndPoint.Y)));
            }
            else if ((this.SummaryData != null) && (this.SummaryData.Count > 0))
            {
                this.XValueRange = new ValueRange<double>(
                    this.SummaryData.Where(m => m.Value.HasValue).Min(p => Math.Min(p.Value.Value.StartPoint.X, p.Value.Value.EndPoint.X)),
                    this.SummaryData.Where(m => m.Value.HasValue).Max(p => Math.Max(p.Value.Value.StartPoint.X, p.Value.Value.EndPoint.X)));
                this.YValueRange = new ValueRange<double>(
                    this.SummaryData.Where(m => m.Value.HasValue).Min(p => Math.Min(p.Value.Value.StartPoint.Y, p.Value.Value.EndPoint.Y)),
                    this.SummaryData.Where(m => m.Value.HasValue).Max(p => Math.Max(p.Value.Value.StartPoint.Y, p.Value.Value.EndPoint.Y)));
            }
        }
    }
}
