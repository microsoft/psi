// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.DataTypes;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a discrete event visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimeIntervalHistoryVisualizationObject : TimelineVisualizationObject<TimeIntervalHistory, TimeIntervalHistoryVisualizationObjectConfiguration>
    {
        /// <summary>
        /// The dictionary of brushes.
        /// </summary>
        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        /// <summary>
        /// The names of all the tracks discovered so far.
        /// </summary>
        private readonly List<string> trackNames = new List<string>();

        /// <summary>
        /// The value to display in the legend.
        /// </summary>
        private string legendValue = string.Empty;

        /// <summary>
        /// Gets the data to de displayed in the control.
        /// </summary>
        public List<TimeIntervalVisualizationObjectData> DisplayData { get; private set; } = new List<TimeIntervalVisualizationObjectData>();

        /// <summary>
        /// Gets the data to de displayed in the control.
        /// </summary>
        public int TrackCount => Math.Max(1, this.trackNames.Count);

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => this.Configuration.FillColor;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override string LegendValue => this.legendValue;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimeIntervalHistoryVisualizationObjectView));

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentValue))
            {
                // if we are not showing only the final results, listen to current value changes and update
                // the DisplayData accordingly
                if (!this.Configuration.ShowFinal)
                {
                    if (this.CurrentValue != null)
                    {
                        this.UpdateDisplayData(this.CurrentValue.Value);
                    }
                }
            }

            base.OnPropertyChanged(sender, e);
        }

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // if we are showing the final results, listen to OnDataCollectionChanged and update
            // the data accordingly
            if (this.Configuration.ShowFinal)
            {
                this.UpdateDisplayData(this.Data.LastOrDefault());
            }

            base.OnDataCollectionChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(TimeIntervalHistoryVisualizationObjectConfiguration.ShowFinal)) ||
                (e.PropertyName == nameof(TimeIntervalHistoryVisualizationObjectConfiguration.FillColor)))
            {
                if (this.Configuration.ShowFinal)
                {
                    this.UpdateDisplayData(this.Data != null ? this.Data.LastOrDefault() : default(Message<TimeIntervalHistory>));
                }
                else
                {
                    this.UpdateDisplayData(this.CurrentValue != null ? this.CurrentValue.Value : default(Message<TimeIntervalHistory>));
                }
            }

            base.OnConfigurationPropertyChanged(sender, e);
        }

        private void UpdateDisplayData(Message<TimeIntervalHistory> message)
        {
            this.RaisePropertyChanging(nameof(this.DisplayData));

            // Rebuild the data
            this.DisplayData = new List<TimeIntervalVisualizationObjectData>();
            this.RaisePropertyChanging(nameof(this.TrackCount));
            this.trackNames.Clear();
            if ((message != null) && (message.Data != null))
            {
                this.trackNames.AddRange(message.Data.Keys.OrderBy(s => s));
            }

            this.GenerateLegendValue();
            this.RaisePropertyChanged(nameof(this.TrackCount));

            if ((message != null) && (message.Data != null))
            {
                // Flatten the dictionary
                foreach (KeyValuePair<string, List<(TimeInterval, string, System.Drawing.Color?)>> dictionaryEntry in message.Data)
                {
                    foreach ((TimeInterval timeInterval, string text, System.Drawing.Color? color) in dictionaryEntry.Value)
                    {
                        this.DisplayData.Add(
                            new TimeIntervalVisualizationObjectData(
                                this.trackNames.IndexOf(dictionaryEntry.Key),
                                timeInterval,
                                text,
                                this.GetBrush(color)));
                    }
                }
            }

            this.RaisePropertyChanged(nameof(this.DisplayData));
        }

        private Brush GetBrush(System.Drawing.Color? color)
        {
            return color == null ? this.GetBrush(this.Configuration.FillColor) : this.GetBrush(Color.FromArgb(color.Value.A, color.Value.R, color.Value.B, color.Value.G));
        }

        private Brush GetBrush(Color color)
        {
            if (!this.brushes.ContainsKey(color))
            {
                this.brushes.Add(color, new SolidColorBrush(color));
            }

            return this.brushes[color];
        }

        private void GenerateLegendValue()
        {
            // For now the legend value is simmply a list of all the track names
            StringBuilder legend = new StringBuilder();
            foreach (string trackName in this.trackNames)
            {
                legend.AppendLine(trackName);
            }

            this.legendValue = legend.ToString();
        }

        /// <summary>
        /// Time interval visualization object data.
        /// </summary>
        public class TimeIntervalVisualizationObjectData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TimeIntervalVisualizationObjectData"/> class.
            /// </summary>
            /// <param name="trackNumber">The track number for this event.</param>
            /// <param name="timeInterval">The time interval for the event.</param>
            /// <param name="text">The text label for the event.</param>
            /// <param name="brush">The brush for the event.</param>
            public TimeIntervalVisualizationObjectData(int trackNumber, TimeInterval timeInterval, string text, Brush brush)
            {
                this.TrackNumber = trackNumber;
                this.TimeInterval = timeInterval;
                this.Text = text;
                this.Brush = brush;
            }

            /// <summary>
            /// Gets or sets the track number for the event.
            /// </summary>
            public int TrackNumber { get; set; }

            /// <summary>
            /// Gets or sets the time interval.
            /// </summary>
            public TimeInterval TimeInterval { get; set; }

            /// <summary>
            /// Gets or sets the text.
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// Gets or sets the brush to paint this time interval with.
            /// </summary>
            public Brush Brush { get; set; }
        }
    }
}
