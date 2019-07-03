// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (xMax, xMin, etc.)

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a scatter plot visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class DiagnosticsVisualizationObjectConfiguration : InstantVisualizationObjectConfiguration
    {
        private GraphLayoutDirection layoutDirection = GraphLayoutDirection.LeftToRight;
        private HeatmapStats heatmapStats = HeatmapStats.LatencyAtEmitter;
        private Color heatmapColor = Colors.Red;
        private double edgeLineThickness = 2;
        private Color edgeColor = Colors.White;
        private Color nodeColor = Colors.White;
        private Color sourceNodeColor = Colors.DarkGreen;
        private Color subpipelineColor = Colors.DarkBlue;
        private Color connectorColor = Colors.LightGray;
        private double infoTextSize = 12;

        /// <summary>
        /// Enumeration of statistics available for heatmap visualization.
        /// </summary>
        public enum HeatmapStats
        {
            /// <summary>
            /// No heatmap visualization.
            /// </summary>
            None,

            /// <summary>
            /// Latency at emitter heatmap visualization (average).
            /// </summary>
            LatencyAtEmitter,

            /// <summary>
            /// Latency at receiver heatmap visualization (average).
            /// </summary>
            LatencyAtReceiver,

            /// <summary>
            /// Receiver queue size heatmap visualization.
            /// </summary>
            QueueSize,

            /// <summary>
            /// Processing time heatmap visualization (average).
            /// </summary>
            Processing,

            /// <summary>
            /// Message throughput heatmap visualization.
            /// </summary>
            Throughput,

            /// <summary>
            /// Dropped message count heatmap visualization.
            /// </summary>
            DroppedCount,

            /// <summary>
            /// Message size heatmap visualization (logarithmic).
            /// </summary>
            MessageSize,
        }

        /// <summary>
        /// Graph layout direction to use.
        /// </summary>
        public enum GraphLayoutDirection
        {
            /// <summary>
            /// Layout graph nodes left-to-right.
            /// </summary>
            LeftToRight,

            /// <summary>
            /// Layout graph nodes top-to-bottom.
            /// </summary>
            TopToBottom,

            /// <summary>
            /// Layout graph nodes right-to-left.
            /// </summary>
            RightToLeft,

            /// <summary>
            /// Layout graph nodes bottom-to-top.
            /// </summary>
            BottomToTop,
        }

        /// <summary>
        /// Gets or sets edge line thickness.
        /// </summary>
        [DataMember]
        public GraphLayoutDirection LayoutDirection
        {
            get { return this.layoutDirection; }
            set { this.Set(nameof(this.LayoutDirection), ref this.layoutDirection, value); }
        }

        /// <summary>
        /// Gets or sets the heatmap statistics.
        /// </summary>
        [DataMember]
        public HeatmapStats HeatmapStatistics
        {
            get { return this.heatmapStats; }
            set { this.Set(nameof(this.HeatmapStatistics), ref this.heatmapStats, value); }
        }

        /// <summary>
        /// Gets or sets heatmap color.
        /// </summary>
        [DataMember]
        public Color HeatmapColor
        {
            get { return this.heatmapColor; }
            set { this.Set(nameof(this.HeatmapColor), ref this.heatmapColor, value); }
        }

        /// <summary>
        /// Gets or sets edge line thickness.
        /// </summary>
        [DataMember]
        public double EdgeLineThickness
        {
            get { return this.edgeLineThickness; }
            set { this.Set(nameof(this.EdgeLineThickness), ref this.edgeLineThickness, value); }
        }

        /// <summary>
        /// Gets or sets edge color.
        /// </summary>
        [DataMember]
        public Color EdgeColor
        {
            get { return this.edgeColor; }
            set { this.Set(nameof(this.EdgeColor), ref this.edgeColor, value); }
        }

        /// <summary>
        /// Gets or sets node color.
        /// </summary>
        [DataMember]
        public Color NodeColor
        {
            get { return this.nodeColor; }
            set { this.Set(nameof(this.NodeColor), ref this.nodeColor, value); }
        }

        /// <summary>
        /// Gets or sets source node color.
        /// </summary>
        [DataMember]
        public Color SourceNodeColor
        {
            get { return this.sourceNodeColor; }
            set { this.Set(nameof(this.SourceNodeColor), ref this.sourceNodeColor, value); }
        }

        /// <summary>
        /// Gets or sets subpipeline color.
        /// </summary>
        [DataMember]
        public Color SubpipelineColor
        {
            get { return this.subpipelineColor; }
            set { this.Set(nameof(this.SubpipelineColor), ref this.subpipelineColor, value); }
        }

        /// <summary>
        /// Gets or sets connector color.
        /// </summary>
        [DataMember]
        public Color ConnectorColor
        {
            get { return this.connectorColor; }
            set { this.Set(nameof(this.ConnectorColor), ref this.connectorColor, value); }
        }

        /// <summary>
        /// Gets or sets info text size.
        /// </summary>
        [DataMember]
        public double InfoTextSize
        {
            get { return this.infoTextSize; }
            set { this.Set(nameof(this.InfoTextSize), ref this.infoTextSize, value); }
        }
    }
}
