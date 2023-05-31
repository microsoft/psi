// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Provides an abstract base class for visualization objects for plotting numerical data.
    /// </summary>
    /// <typeparam name="TData">The type of the numerical data.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.Timeline)]
    public abstract class PlotVisualizationObject<TData> : StreamIntervalVisualizationObject<TData>, IYValueRangeProvider
    {
        /// <summary>
        /// The Y value range of the current data or summary data.
        /// </summary>
        private ValueRange<double> yValueRange = null;

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

        /// <inheritdoc/>
        public event EventHandler<EventArgs> YValueRangeChanged;

        /// <summary>
        /// Gets or sets the line color.
        /// </summary>
        [DataMember]
        [DisplayName("Color")]
        [Description("The color of the plot.")]
        public Color Color
        {
            get { return this.color; }

            set
            {
                this.RaisePropertyChanging(nameof(this.LegendColor));
                this.Set(nameof(this.Color), ref this.color, value);
                this.RaisePropertyChanged(nameof(this.LegendColor));
                this.RangeColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the line width.
        /// </summary>
        [DataMember]
        [DisplayName("Line Width")]
        [Description("The width of the line (in pixels).")]
        public double LineWidth
        {
            get { return this.lineWidth; }
            set { this.Set(nameof(this.LineWidth), ref this.lineWidth, value); }
        }

        /// <summary>
        /// Gets or sets the plot interpolation style.
        /// </summary>
        [DataMember]
        [DisplayName("Interpolation Style")]
        [Description("Specifies how to interpolate between points.")]
        public InterpolationStyle InterpolationStyle
        {
            get { return this.interpolationStyle; }
            set { this.Set(nameof(this.InterpolationStyle), ref this.interpolationStyle, value); }
        }

        /// <summary>
        /// Gets or sets the marker color.
        /// </summary>
        [DataMember]
        [DisplayName("Marker Color")]
        [Description("The color of the marker.")]
        public Color MarkerColor
        {
            get { return this.markerColor; }
            set { this.Set(nameof(this.MarkerColor), ref this.markerColor, value); }
        }

        /// <summary>
        /// Gets or sets the marker size.
        /// </summary>
        [DataMember]
        [DisplayName("Marker Size")]
        [Description("The size of the marker (in pixels).")]
        public double MarkerSize
        {
            get { return this.markerSize; }
            set { this.Set(nameof(this.MarkerSize), ref this.markerSize, value); }
        }

        /// <summary>
        /// Gets or sets the marker style.
        /// </summary>
        [DataMember]
        [DisplayName("Marker Style")]
        [Description("The style of the marker.")]
        public MarkerStyle MarkerStyle
        {
            get { return this.markerStyle; }
            set { this.Set(nameof(this.MarkerStyle), ref this.markerStyle, value); }
        }

        /// <summary>
        /// Gets or sets the range color.
        /// </summary>
        [DataMember]
        [DisplayName("Range Color")]
        [Description("The color of summarized ranges.")]
        public Color RangeColor
        {
            get { return this.rangeColor; }
            set { this.Set(nameof(this.RangeColor), ref this.rangeColor, value); }
        }

        /// <summary>
        /// Gets or sets the range width.
        /// </summary>
        [DisplayName("Range Line Width")]
        [Description("The line width of summarized ranges.")]
        [DataMember]
        public double RangeWidth
        {
            get { return this.rangeWidth; }
            set { this.Set(nameof(this.RangeWidth), ref this.rangeWidth, value); }
        }

        /// <summary>
        /// Gets the current mouse position within the plot.
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Mouse Position")]
        [Description("The position of the mouse in the visualization object.")]
        public string MousePositionString => (this.Panel is TimelineVisualizationPanel timelineVisualizationPanel) ? timelineVisualizationPanel.MousePositionString : default;

        /// <summary>
        /// Gets the Y axis.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Axis YAxis => (this.Panel as TimelineVisualizationPanel)?.YAxis;

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public ValueRange<double> YValueRange
        {
            get { return this.yValueRange; }

            protected set
            {
                if (this.yValueRange != value)
                {
                    this.yValueRange = value;
                    this.YValueRangeChanged?.Invoke(this, EventArgs.Empty);
                }
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
        public abstract double GetNumericValue(TData data);

        /// <summary>
        /// Converts the data to a string value to display.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The string value correspoding to the data.</returns>
        public abstract string GetStringValue(TData data);

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = base.ContextMenuItemsInfo();

            if (this.MarkerStyle == MarkerStyle.None)
            {
                items.Add(new ContextMenuItemInfo(
                    null,
                    "Show Markers",
                    new VisualizationCommand(() => this.MarkerStyle = MarkerStyle.Circle)));
            }
            else
            {
                items.Add(new ContextMenuItemInfo(
                    null,
                    "Hide Markers",
                    new VisualizationCommand(() => this.MarkerStyle = MarkerStyle.None)));
            }

            return items;
        }

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnDataCollectionChanged(e);
            this.ComputeYValueRange();
        }

        /// <inheritdoc />
        protected override void OnSummaryDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnSummaryDataCollectionChanged(e);
            this.ComputeYValueRange();
        }

        /// <inheritdoc/>
        protected override void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimelineVisualizationPanel.YAxis))
            {
                this.RaisePropertyChanged(nameof(this.YAxis));
            }
            else if (e.PropertyName == nameof(TimelineVisualizationPanel.MousePositionString))
            {
                this.RaisePropertyChanged(nameof(this.MousePositionString));
            }

            base.OnPanelPropertyChanged(sender, e);
        }

        // Compute the rangwe of Y values in the dataset
        private void ComputeYValueRange()
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            if ((this.Data != null) && (this.Data.Count > 0))
            {
                foreach (var value in this.Data)
                {
                    var doubleValue = this.GetNumericValue(value.Data);
                    if (!double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue))
                    {
                        min = Math.Min(min, doubleValue);
                        max = Math.Max(max, doubleValue);
                    }
                }
            }
            else if ((this.SummaryData != null) && (this.SummaryData.Count > 0))
            {
                foreach (var value in this.SummaryData)
                {
                    var doubleMinimum = this.GetNumericValue(value.Minimum);
                    if (!double.IsNaN(doubleMinimum) && !double.IsInfinity(doubleMinimum))
                    {
                        min = Math.Min(min, doubleMinimum);
                    }

                    var doubleMaximum = this.GetNumericValue(value.Maximum);
                    if (!double.IsNaN(doubleMaximum) && !double.IsInfinity(doubleMaximum))
                    {
                        max = Math.Max(max, doubleMaximum);
                    }
                }
            }

            if (max >= min)
            {
                if (this.YValueRange == null || this.YValueRange.Minimum != min || this.YAxis.Maximum != max)
                {
                    this.YValueRange = new ValueRange<double>(min, max);
                }
            }
            else if (this.YValueRange != null)
            {
                this.YValueRange = null;
            }
        }
    }
}
