// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Represents a plot visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the data for the plot visualization object.</typeparam>
    public abstract class PlotVisualizationObject<TData> : TimelineVisualizationObject<TData>
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

            set
            {
                this.RaisePropertyChanging(nameof(this.LegendColor));
                this.Set(nameof(this.Color), ref this.color, value);
                this.RaisePropertyChanged(nameof(this.LegendColor));
            }
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

            set
            {
                this.Set(nameof(this.YAxisComputeMode), ref this.yAxisComputeMode, value);
                this.AutoComputeYAxis();
            }
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

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public override Color LegendColor => this.Color;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override string LegendValue => this.CurrentValue.HasValue ? this.GetStringValue(this.CurrentValue.Value.Data) : string.Empty;

        /// <summary>
        /// Converts the data to a double value to use for plotting.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The double value correspoding to the data.</returns>
        public abstract double GetDoubleValue(TData data);

        /// <summary>
        /// Converts the data to a string value to display.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The string value correspoding to the data.</returns>
        public abstract string GetStringValue(TData data);

        /// <inheritdoc/>
        protected override int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            return IndexHelper.GetIndexForTime(currentTime, count, timeAtIndex, this.InterpolationStyle);
        }

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnDataCollectionChanged(e);
            this.AutoComputeYAxis();
        }

        /// <inheritdoc />
        protected override void OnSummaryDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnSummaryDataCollectionChanged(e);
            this.AutoComputeYAxis();
        }

        // Auto compute the y axis limits
        private void AutoComputeYAxis()
        {
            if (this.YAxisComputeMode == AxisComputeMode.Auto)
            {
                double min = double.MaxValue;
                double max = double.MinValue;

                if ((this.Data != null) && (this.Data.Count > 0))
                {
                    foreach (var value in this.Data)
                    {
                        var doubleValue = this.GetDoubleValue(value.Data);
                        min = Math.Min(min, doubleValue);
                        max = Math.Max(max, doubleValue);
                    }
                }
                else if ((this.SummaryData != null) && (this.SummaryData.Count > 0))
                {
                    foreach (var value in this.SummaryData)
                    {
                        min = Math.Min(min, this.GetDoubleValue(value.Minimum));
                        max = Math.Max(max, this.GetDoubleValue(value.Maximum));
                    }
                }

                if (max >= min)
                {
                    this.SetYRange(min, max);
                }
            }
        }

        /// <summary>
        /// Programmatically sets the y-axis range without altering the y-axis compute mode.
        /// </summary>
        /// <param name="yMin">The new y minimum value.</param>
        /// <param name="yMax">The new y maximum value.</param>
        private void SetYRange(double yMin, double yMax)
        {
            this.Set(nameof(this.YMin), ref this.yMin, yMin);
            this.Set(nameof(this.YMax), ref this.yMax, yMax);
        }
    }
}
