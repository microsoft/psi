// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a discrete event visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimeIntervalHistoryVisualizationObject : TimelineVisualizationObject<Dictionary<string, List<(TimeInterval, string)>>, TimeIntervalHistoryVisualizationObjectConfiguration>
    {
        /// <summary>
        /// The names of all the tracks discovered so far.
        /// </summary>
        private List<string> trackNames = new List<string>();

        /// <summary>
        /// The value to display in the legend
        /// </summary>
        private string legendValue = string.Empty;

        /// <summary>
        /// Gets the data to de displayed in the control
        /// </summary>
        public List<TimeIntervalVisualizationObjectData> DisplayData { get; private set; } = new List<TimeIntervalVisualizationObjectData>();

        /// <summary>
        /// Gets the data to de displayed in the control
        /// </summary>
        public int TrackCount => Math.Max(1, this.trackNames.Count);

        /// <inheritdoc />
        public override string LegendValue => this.legendValue;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimeIntervalHistoryVisualizationObjectView));

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanging(nameof(this.DisplayData));

            // Rebuild the data
            this.DisplayData = new List<TimeIntervalVisualizationObjectData>();
            this.trackNames = new List<string>();

            // Since these messages are cumulative, we only need to look at the last message we've received.
            Message<Dictionary<string, List<(TimeInterval, string)>>> lastMessage = this.Data.LastOrDefault();
            if ((lastMessage != null) && (lastMessage.Data != null))
            {
                // Flatten the dictionary
                foreach (KeyValuePair<string, List<(TimeInterval, string)>> dictionaryEntry in lastMessage.Data)
                {
                    foreach ((TimeInterval timeInterval, string text) timeIntervalInfo in dictionaryEntry.Value)
                    {
                        this.DisplayData.Add(new TimeIntervalVisualizationObjectData(this.TrackNameToTrackNumber(dictionaryEntry.Key), timeIntervalInfo.timeInterval.Left, timeIntervalInfo.timeInterval.Right, timeIntervalInfo.text));
                    }
                }
            }

            this.RaisePropertyChanged(nameof(this.DisplayData));
            base.OnDataCollectionChanged(e);
        }

        private int TrackNameToTrackNumber(string trackName)
        {
            if (!this.trackNames.Contains(trackName))
            {
                this.RaisePropertyChanging(nameof(this.TrackCount));

                lock (this.trackNames)
                {
                    if (!this.trackNames.Contains(trackName))
                    {
                        this.trackNames.Add(trackName);
                    }
                }

                this.GenerateLegendValue();
                this.RaisePropertyChanged(nameof(this.TrackCount));
            }

            return this.trackNames.IndexOf(trackName);
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
        /// Time interval visualization object data
        /// </summary>
        public class TimeIntervalVisualizationObjectData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TimeIntervalVisualizationObjectData"/> class.
            /// </summary>
            /// <param name="trackNumber">The track number for this event</param>
            /// <param name="startTime">The start time of the event</param>
            /// <param name="endTime">The end time of the event</param>
            /// <param name="text">The text label for the event</param>
            public TimeIntervalVisualizationObjectData(int trackNumber, DateTime startTime, DateTime endTime, string text)
            {
                this.TrackNumber = trackNumber;
                this.StartTime = startTime;
                this.EndTime = endTime;
                this.Text = text;
            }

            /// <summary>
            /// Gets or sets the track number for the event
            /// </summary>
            public int TrackNumber { get; set; }

            /// <summary>
            /// Gets or sets the channel id
            /// </summary>
            public DateTime StartTime { get; set; }

            /// <summary>
            /// Gets or sets the channel id
            /// </summary>
            public DateTime EndTime { get; set; }

            /// <summary>
            /// Gets or sets the text
            /// </summary>
            public string Text { get; set; }
        }
    }
}
