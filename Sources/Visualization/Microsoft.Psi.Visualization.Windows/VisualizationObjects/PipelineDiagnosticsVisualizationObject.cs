// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a diagnostics visualization object.
    /// </summary>
    [VisualizationObject("Diagnostics", null, IconSourcePath.Diagnostics, IconSourcePath.Diagnostics)]
    [VisualizationPanelType(VisualizationPanelType.XY)]
    public class PipelineDiagnosticsVisualizationObject : StreamValueVisualizationObject<PipelineDiagnostics>
    {
        private GraphLayoutDirection layoutDirection = GraphLayoutDirection.LeftToRight;
        private bool showEmitterNames = false;
        private bool showReceiverNames = true;
        private bool showDeliveryPolicies = false;
        private bool showExporterConnections = false;
        private HeatmapStats heatmapStats = HeatmapStats.AvgMessageReceivedLatency;
        private Color heatmapColor = Colors.Red;
        private HighlightCondition highlightCondition = HighlightCondition.None;
        private Color highlightColor = Colors.Yellow;
        private double edgeLineThickness = 2;
        private Color edgeColor = Colors.White;
        private Color nodeColor = Colors.White;
        private Color sourceNodeColor = Colors.DarkGreen;
        private Color subpipelineColor = Colors.DarkBlue;
        private Color connectorColor = Colors.LightGray;
        private Color joinColor = Colors.LightSlateGray;
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
            /// Message created latency heatmap visualization (average).
            /// </summary>
            AvgMessageCreatedLatency,

            /// <summary>
            /// Message emitted latency heatmap visualization (average).
            /// </summary>
            AvgMessageEmittedLatency,

            /// <summary>
            /// Message received latency heatmap visualization (average).
            /// </summary>
            AvgMessageReceivedLatency,

            /// <summary>
            /// Delivery queue size heatmap visualization.
            /// </summary>
            AvgDeliveryQueueSize,

            /// <summary>
            /// Message process time heatmap visualization (average).
            /// </summary>
            AvgMessageProcessTime,

            /// <summary>
            /// Message emitted count heatmap visualization.
            /// </summary>
            TotalMessageEmittedCount,

            /// <summary>
            /// Message dropped count heatmap visualization.
            /// </summary>
            TotalMessageDroppedCount,

            /// <summary>
            /// Message processed count heatmap visualization.
            /// </summary>
            TotalMessageProcessedCount,

            /// <summary>
            /// Message size heatmap visualization (logarithmic).
            /// </summary>
            AvgMessageSize,
        }

        /// <summary>
        /// Enumeration of conditions available for highlight visualization.
        /// </summary>
        public enum HighlightCondition
        {
            /// <summary>
            /// No highlight visualization.
            /// </summary>
            None,

            /// <summary>
            /// Unlimited delivery policy.
            /// </summary>
            UnlimitedDeliveryPolicy,

            /// <summary>
            /// Latest message delivery policy.
            /// </summary>
            LatestMessageDeliveryPolicy,

            /// <summary>
            /// Throttle delivery policy.
            /// </summary>
            ThrottleDeliveryPolicy,

            /// <summary>
            /// Synchronous or throttle delivery policy.
            /// </summary>
            SynchronousOrThrottleDeliveryPolicy,

            /// <summary>
            /// Latency constrained delivery policy.
            /// </summary>
            LatencyConstrainedDeliveryPolicy,

            /// <summary>
            /// Queue size constrained delivery policy.
            /// </summary>
            QueueSizeConstrainedDeliveryPolicy,

            /// <summary>
            /// Throttled receivers.
            /// </summary>
            ThrottledReceivers,
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
        [DisplayName("Layout Direction")]
        [Description("Change direction of graph node layout.")]
        [PropertyOrder(1)]
        public GraphLayoutDirection LayoutDirection
        {
            get { return this.layoutDirection; }
            set { this.Set(nameof(this.LayoutDirection), ref this.layoutDirection, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show emitter names.
        /// </summary>
        [DataMember]
        [DisplayName("Show Emitter Names")]
        [Description("Show emitter names on edge labels.")]
        [PropertyOrder(2)]
        public bool ShowEmitterNames
        {
            get { return this.showEmitterNames; }
            set { this.Set(nameof(this.ShowEmitterNames), ref this.showEmitterNames, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show receiver names.
        /// </summary>
        [DataMember]
        [DisplayName("Show Receiver Names")]
        [Description("Show receiver names on edge labels.")]
        [PropertyOrder(3)]
        public bool ShowReceiverNames
        {
            get { return this.showReceiverNames; }
            set { this.Set(nameof(this.ShowReceiverNames), ref this.showReceiverNames, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show receiver names.
        /// </summary>
        [DataMember]
        [DisplayName("Show Delivery Policies")]
        [Description("Show delivery policy used by receiver on edge labels.")]
        [PropertyOrder(4)]
        public bool ShowDeliveryPolicies
        {
            get { return this.showDeliveryPolicies; }
            set { this.Set(nameof(this.ShowDeliveryPolicies), ref this.showDeliveryPolicies, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show exporter connections.
        /// </summary>
        [DataMember]
        [DisplayName("Show Exporter Links")]
        [Description("Show connections to data Exporters.")]
        [PropertyOrder(5)]
        public bool ShowExporterConnections
        {
            get { return this.showExporterConnections; }
            set { this.Set(nameof(this.ShowExporterConnections), ref this.showExporterConnections, value); }
        }

        /// <summary>
        /// Gets or sets the heatmap statistics.
        /// </summary>
        [DataMember]
        [DisplayName("Heatmap Statistics")]
        [Description("Select statistic used for heatmap visualization.")]
        [PropertyOrder(6)]
        public HeatmapStats HeatmapStatistics
        {
            get { return this.heatmapStats; }
            set { this.Set(nameof(this.HeatmapStatistics), ref this.heatmapStats, value); }
        }

        /// <summary>
        /// Gets or sets heatmap color.
        /// </summary>
        [DataMember]
        [DisplayName("Heatmap Base Color")]
        [Description("Base color used for heatmap visualization.")]
        [PropertyOrder(7)]
        public Color HeatmapColor
        {
            get { return this.heatmapColor; }
            set { this.Set(nameof(this.HeatmapColor), ref this.heatmapColor, value); }
        }

        /// <summary>
        /// Gets or sets the heatmap statistics.
        /// </summary>
        [DataMember]
        [DisplayName("Highlight")]
        [Description("Select condition to highlight (overrides heatmap).")]
        [PropertyOrder(8)]
        public HighlightCondition Highlight
        {
            get { return this.highlightCondition; }
            set { this.Set(nameof(this.Highlight), ref this.highlightCondition, value); }
        }

        /// <summary>
        /// Gets or sets highlight color.
        /// </summary>
        [DataMember]
        [DisplayName("Highlight Color")]
        [Description("Color used for highlighting.")]
        [PropertyOrder(9)]
        public Color HighlightColor
        {
            get { return this.highlightColor; }
            set { this.Set(nameof(this.HighlightColor), ref this.highlightColor, value); }
        }

        /// <summary>
        /// Gets or sets node color.
        /// </summary>
        [DataMember]
        [DisplayName("Node Base Color")]
        [Description("Base color used for node visualization.")]
        [PropertyOrder(10)]
        public Color NodeColor
        {
            get { return this.nodeColor; }
            set { this.Set(nameof(this.NodeColor), ref this.nodeColor, value); }
        }

        /// <summary>
        /// Gets or sets source node color.
        /// </summary>
        [DataMember]
        [DisplayName("Source Node Color")]
        [Description("Color used for source node visualization.")]
        [PropertyOrder(11)]
        public Color SourceNodeColor
        {
            get { return this.sourceNodeColor; }
            set { this.Set(nameof(this.SourceNodeColor), ref this.sourceNodeColor, value); }
        }

        /// <summary>
        /// Gets or sets subpipeline color.
        /// </summary>
        [DataMember]
        [DisplayName("Subpipeline Color")]
        [Description("Color used for subpipeline node visualization.")]
        [PropertyOrder(12)]
        public Color SubpipelineColor
        {
            get { return this.subpipelineColor; }
            set { this.Set(nameof(this.SubpipelineColor), ref this.subpipelineColor, value); }
        }

        /// <summary>
        /// Gets or sets connector node color.
        /// </summary>
        [DataMember]
        [DisplayName("Connector Color")]
        [Description("Color used for connector node visualization.")]
        [PropertyOrder(13)]
        public Color ConnectorColor
        {
            get { return this.connectorColor; }
            set { this.Set(nameof(this.ConnectorColor), ref this.connectorColor, value); }
        }

        /// <summary>
        /// Gets or sets join node color.
        /// </summary>
        [DataMember]
        [DisplayName("Join Color")]
        [Description("Color used for join node visualization.")]
        [PropertyOrder(14)]
        public Color JoinColor
        {
            get { return this.joinColor; }
            set { this.Set(nameof(this.JoinColor), ref this.joinColor, value); }
        }

        /// <summary>
        /// Gets or sets edge color.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Color")]
        [Description("Color used for graph edges.")]
        [PropertyOrder(15)]
        public Color EdgeColor
        {
            get { return this.edgeColor; }
            set { this.Set(nameof(this.EdgeColor), ref this.edgeColor, value); }
        }

        /// <summary>
        /// Gets or sets edge line thickness.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Line Thickness")]
        [Description("Thickness used for graph edge line visualization.")]
        [PropertyOrder(16)]
        public double EdgeLineThickness
        {
            get { return this.edgeLineThickness; }
            set { this.Set(nameof(this.EdgeLineThickness), ref this.edgeLineThickness, value); }
        }

        /// <summary>
        /// Gets or sets info text size.
        /// </summary>
        [DataMember]
        [DisplayName("Info Text Size")]
        [Description("Text size used for info text visualization.")]
        [PropertyOrder(17)]
        public double InfoTextSize
        {
            get { return this.infoTextSize; }
            set { this.Set(nameof(this.InfoTextSize), ref this.infoTextSize, value); }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PipelineDiagnosticsVisualizationObjectView));

        /// <inheritdoc />
        [IgnoreDataMember]
        public override string IconSource => this.IsBound ? this.IsLive ? IconSourcePath.DiagnosticsLive : IconSourcePath.Diagnostics : IconSourcePath.StreamUnbound;

        /// <summary>
        /// Gets or sets a value indicating whether visualization object has been bound.
        /// </summary>
        [IgnoreDataMember]
        public bool ModelDirty { get; set; } = false;

        /// <summary>
        /// Adds the derived receiver diagnostics streams for a specified receiver id.
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        public void AddDerivedReceiverDiagnosticsStreams(int receiverId)
        {
            var partition = VisualizationContext.Instance
                .DatasetViewModel
                .CurrentSessionViewModel
                .PartitionViewModels
                .FirstOrDefault(p => p.Name == this.StreamBinding.PartitionName);

            var pipelineDiagnosticsStreamTreeNode = partition
                .FindStreamTreeNode(this.StreamBinding.StreamName) as PipelineDiagnosticsStreamTreeNode;

            var receiver = pipelineDiagnosticsStreamTreeNode.AddDerivedReceiverDiagnosticsChildren(receiverId);
            partition.SelectNode(receiver.Path);
            receiver.ExpandAll();
        }

        /// <inheritdoc />
        protected override void OnStreamUnbound()
        {
            base.OnStreamUnbound();
            this.ModelDirty = true;
        }
    }
}
