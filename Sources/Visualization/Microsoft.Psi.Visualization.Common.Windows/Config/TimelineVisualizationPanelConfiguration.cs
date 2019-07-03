// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yAxis)

namespace Microsoft.Psi.Visualization.Config
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Common;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a timeline visualization panel object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationPanelConfiguration : VisualizationPanelConfiguration
    {
        private AxisInfo yAxis = new AxisInfo() { Label = string.Empty, Color = Colors.Black, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        private AxisInfo timeAxis = new AxisInfo() { Label = string.Empty, Color = Colors.Black, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        private bool showLegend = false;
        private bool showTimeTicks = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanelConfiguration"/> class.
        /// </summary>
        public TimelineVisualizationPanelConfiguration()
        {
            this.Name = "Timeline Panel";
            this.Height = 70;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the legend should be shown.
        /// </summary>
        [DataMember]
        public bool ShowLegend
        {
            get { return this.showLegend; }
            set { this.Set(nameof(this.ShowLegend), ref this.showLegend, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the time ticks should be shown.
        /// </summary>
        [DataMember]
        public bool ShowTimeTicks
        {
            get { return this.showTimeTicks; }
            set { this.Set(nameof(this.ShowTimeTicks), ref this.showTimeTicks, value); }
        }

        /// <summary>
        /// Gets or sets the time axis.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public AxisInfo TimeAxis
        {
            get { return this.timeAxis; }
            set { this.Set(nameof(this.TimeAxis), ref this.timeAxis, value); }
        }

        /// <summary>
        /// Gets or sets the Y axis.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        public AxisInfo YAxis
        {
            get { return this.yAxis; }
            set { this.Set(nameof(this.YAxis), ref this.yAxis, value); }
        }
    }
}