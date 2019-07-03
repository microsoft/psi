// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Common;

    /// <summary>
    /// Represents a plot visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class PlotVisualizationObjectConfiguration : TimelineVisualizationObjectConfiguration
    {
        /// <summary>
        /// The color of the line to draw.
        /// </summary>
        private Color color = Colors.LightGray;

        /// <summary>
        /// Stroke thickness to draw data stream with.
        /// </summary>
        private double lineWidth = 1;

        /// <summary>
        /// How lines in the plot are interpolated from the points.
        /// </summary>
        private InterpolationStyle interpolationStyle = InterpolationStyle.Direct;

        /// <summary>
        /// The color of the marker to draw.
        /// </summary>
        private Color markerColor = Colors.LightGray;

        /// <summary>
        /// The size of the marker to draw.
        /// </summary>
        private double markerSize = 4;

        /// <summary>
        /// The style of the marker to draw.
        /// </summary>
        private MarkerStyle markerStyle = MarkerStyle.None;

        /// <summary>
        /// The color in which to draw the data range.
        /// </summary>
        private Color rangeColor = Colors.LightGray;

        /// <summary>
        /// Stroke thickness with which to draw the data range.
        /// </summary>
        private double rangeWidth = 1;

        /// <summary>
        /// The mode for the y axis.
        /// </summary>
        private AxisComputeMode yAxisComputeMode = AxisComputeMode.Auto;

        /// <summary>
        /// The max value for the y axis.
        /// </summary>
        [DataMember]
        private double yMax = 0;

        /// <summary>
        /// The min value for the y axis.
        /// </summary>
        [DataMember]
        private double yMin = 0;

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
        /// Gets or sets the plot interpolation style.
        /// </summary>
        [DataMember]
        public InterpolationStyle InterpolationStyle
        {
            get { return this.interpolationStyle; }
            set { this.Set(nameof(this.InterpolationStyle), ref this.interpolationStyle, value); }
        }

        /// <summary>
        /// Gets or sets the marker color.
        /// </summary>
        [DataMember]
        public Color MarkerColor
        {
            get { return this.markerColor; }
            set { this.Set(nameof(this.MarkerColor), ref this.markerColor, value); }
        }

        /// <summary>
        /// Gets or sets the marker size.
        /// </summary>
        [DataMember]
        public double MarkerSize
        {
            get { return this.markerSize; }
            set { this.Set(nameof(this.MarkerSize), ref this.markerSize, value); }
        }

        /// <summary>
        /// Gets or sets the marker style.
        /// </summary>
        [DataMember]
        public MarkerStyle MarkerStyle
        {
            get { return this.markerStyle; }
            set { this.Set(nameof(this.MarkerStyle), ref this.markerStyle, value); }
        }

        /// <summary>
        /// Gets or sets the range color.
        /// </summary>
        [DataMember]
        public Color RangeColor
        {
            get { return this.rangeColor; }
            set { this.Set(nameof(this.RangeColor), ref this.rangeColor, value); }
        }

        /// <summary>
        /// Gets or sets the range width.
        /// </summary>
        [DataMember]
        public double RangeWidth
        {
            get { return this.rangeWidth; }
            set { this.Set(nameof(this.RangeWidth), ref this.rangeWidth, value); }
        }

        /// <summary>
        /// Gets or sets the y axis compute mode.
        /// </summary>
        [DataMember]
        public AxisComputeMode YAxisComputeMode
        {
            get { return this.yAxisComputeMode; }
            set { this.Set(nameof(this.YAxisComputeMode), ref this.yAxisComputeMode, value); }
        }

        /// <summary>
        /// Gets or sets the y max value.
        /// </summary>
        [IgnoreDataMember] // property has side effects so serialize its backing field instead
        public double YMax
        {
            get => this.yMax;
            set
            {
                this.YAxisComputeMode = AxisComputeMode.Manual;
                this.Set(nameof(this.YMax), ref this.yMax, value);
            }
        }

        /// <summary>
        /// Gets or sets the y min value.
        /// </summary>
        [IgnoreDataMember] // property has side effects so serialize its backing field instead
        public double YMin
        {
            get => this.yMin;
            set
            {
                this.YAxisComputeMode = AxisComputeMode.Manual;
                this.Set(nameof(this.YMin), ref this.yMin, value);
            }
        }

        /// <summary>
        /// Programmatically sets the y-axis range without altering the y-axis compute mode.
        /// </summary>
        /// <param name="yMin">The new y minimum value.</param>
        /// <param name="yMax">The new y maximum value.</param>
        public void SetYRange(double yMin, double yMax)
        {
            this.Set(nameof(this.YMin), ref this.yMin, yMin);
            this.Set(nameof(this.YMax), ref this.yMax, yMax);
        }
    }
}
