// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a time interval history visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimeIntervalHistoryVisualizationObjectConfiguration : TimelineVisualizationObjectConfiguration
    {
        private Color lineColor = Colors.LightBlue;

        private double lineWidth = 2;

        private Color fillColor = Colors.LightSlateGray;

        private bool showFinal = true;

        /// <summary>
        /// Gets or sets the default line color for the visualization object.
        /// </summary>
        [DataMember]
        public Color LineColor
        {
            get { return this.lineColor; }
            set { this.Set(nameof(this.LineColor), ref this.lineColor, value); }
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
        /// Gets or sets the default fill color for the visualization object.
        /// </summary>
        [DataMember]
        public Color FillColor
        {
            get { return this.fillColor; }
            set { this.Set(nameof(this.FillColor), ref this.fillColor, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we are only showing the final results.
        /// </summary>
        [DataMember]
        public bool ShowFinal
        {
            get { return this.showFinal; }
            set { this.Set(nameof(this.ShowFinal), ref this.showFinal, value); }
        }
    }
}
