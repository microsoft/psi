// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a time interval visualization object.
    /// </summary>
    public abstract class TimeIntervalVisualizationObject : TimelineVisualizationObject<Tuple<DateTime, DateTime>>
    {
        /// <summary>
        /// The color of the marker to draw.
        /// </summary>
        private Color color = Colors.Gray;

        /// <summary>
        /// The color of the marker to draw.
        /// </summary>
        private Color thresholdColor = Colors.Orange;

        /// <summary>
        /// The size of the marker to draw.
        /// </summary>
        private double markerSize = 3;

        /// <summary>
        /// The threshold for the latency visualizer.
        /// </summary>
        private double threshold = 50;

        /// <summary>
        /// The total number of tracks on which time intervals are shown in the current panel.
        /// </summary>
        private int trackCount = 1;

        /// <summary>
        /// The index in the track at which to show the time interval.
        /// </summary>
        private int trackIndex = 0;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimeIntervalVisualizationObjectView));

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
        /// Gets or sets the threshold color.
        /// </summary>
        [DataMember]
        public Color ThresholdColor
        {
            get { return this.thresholdColor; }
            set { this.Set(nameof(this.ThresholdColor), ref this.thresholdColor, value); }
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
        /// Gets or sets the threshold in milliseconds.
        /// </summary>
        [DataMember]
        public double Threshold
        {
            get { return this.threshold; }
            set { this.Set(nameof(this.Threshold), ref this.threshold, value); }
        }

        /// <summary>
        /// Gets or sets the track count.
        /// </summary>
        [DataMember]
        public int TrackCount
        {
            get { return this.trackCount; }
            set { this.Set(nameof(this.TrackCount), ref this.trackCount, value); }
        }

        /// <summary>
        /// Gets or sets the track index.
        /// </summary>
        [DataMember]
        public int TrackIndex
        {
            get { return this.trackIndex; }
            set { this.Set(nameof(this.TrackIndex), ref this.trackIndex, value); }
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => this.Color;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override string LegendValue => this.CurrentValue.HasValue ? (this.CurrentValue.Value.Data.Item2 - this.CurrentValue.Value.Data.Item1).ToString() : string.Empty;
    }
}
