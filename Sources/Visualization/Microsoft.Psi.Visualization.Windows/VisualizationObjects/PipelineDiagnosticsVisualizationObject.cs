// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a diagnostics visualization object.
    /// </summary>
    [VisualizationObject("Diagnostics", null, IconSourcePath.Diagnostics, IconSourcePath.Diagnostics)]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class PipelineDiagnosticsVisualizationObject : StreamValueVisualizationObject<PipelineDiagnostics>
    {
        private GraphLayoutDirection layoutDirection = GraphLayoutDirection.LeftToRight;
        private bool showEmitterNames = false;
        private bool showReceiverNames = true;
        private bool showDeliveryPolicies = false;
        private bool showLossyDeliveryPoliciesAsDotted = true;
        private bool showExporterConnections = false;
        private HeatmapStats heatmapStats = HeatmapStats.None;
        private Color heatmapColor = Colors.Red;
        private HighlightCondition highlightCondition = HighlightCondition.None;
        private int highlightOpacity = 25;
        private double edgeLineThickness = 2;
        private Color edgeColor = Colors.White;
        private Color nodeColor = Colors.White;
        private Color sourceNodeColor = Colors.DarkGreen;
        private Color subpipelineColor = Colors.DarkBlue;
        private Color connectorColor = Colors.LightGray;
        private Color joinColor = Colors.LightSlateGray;
        private double infoTextSize = 12;

        private int edgeUnderCursor = -1;

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
            /// Message dropped percentage heatmap visualization.
            /// </summary>
            TotalMessageDroppedPercentage,

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
        /// Gets or sets a value indicating whether to show lossy delivery policies as dotted.
        /// </summary>
        [DataMember]
        [DisplayName("Show Lossy Delivery Policies as Dotted")]
        [Description("Shows all lossy delivery policies (everything except Unlimited, Synchronous and SynchronousOrThrottle with dotted lines.")]
        [PropertyOrder(5)]
        public bool ShowLossyDeliveryPoliciesAsDotted
        {
            get { return this.showLossyDeliveryPoliciesAsDotted; }
            set { this.Set(nameof(this.ShowLossyDeliveryPoliciesAsDotted), ref this.showLossyDeliveryPoliciesAsDotted, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show exporter connections.
        /// </summary>
        [DataMember]
        [DisplayName("Show Exporter Links")]
        [Description("Show connections to data Exporters.")]
        [PropertyOrder(6)]
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
        [PropertyOrder(7)]
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
        [PropertyOrder(8)]
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
        [PropertyOrder(9)]
        public HighlightCondition Highlight
        {
            get { return this.highlightCondition; }
            set { this.Set(nameof(this.Highlight), ref this.highlightCondition, value); }
        }

        /// <summary>
        /// Gets or sets highlight color.
        /// </summary>
        [DataMember]
        [DisplayName("Highlight Opacity")]
        [Description("Opacity level (0-100) used when rendering unhighlighed edges when a highlight is selected.")]
        [PropertyOrder(10)]
        public int HighlightOpacity
        {
            get { return this.highlightOpacity; }
            set { this.Set(nameof(this.HighlightOpacity), ref this.highlightOpacity, value); }
        }

        /// <summary>
        /// Gets or sets node color.
        /// </summary>
        [DataMember]
        [DisplayName("Node Base Color")]
        [Description("Base color used for node visualization.")]
        [PropertyOrder(11)]
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
        [PropertyOrder(12)]
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
        [PropertyOrder(13)]
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
        [PropertyOrder(14)]
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
        [PropertyOrder(15)]
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
        [PropertyOrder(16)]
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
        [PropertyOrder(17)]
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
        [PropertyOrder(18)]
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

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = new List<ContextMenuItemInfo>();

            // If the mouse is over an edge, add a menu item to expand the streams of the receiver.
            if (this.edgeUnderCursor != -1)
            {
                var addDiagnosticsCommands = new ContextMenuItemInfo($"Add derived diagnostics streams for receiver {this.edgeUnderCursor}");
                items.Add(addDiagnosticsCommands);

                foreach (var receiverDiagnosticsStatistic in PipelineDiagnostics.ReceiverDiagnostics.AllStatistics)
                {
                    addDiagnosticsCommands.SubItems.Add(
                        new ContextMenuItemInfo(
                            null,
                            receiverDiagnosticsStatistic,
                            new PsiCommand(() => this.AddReceiverDiagnosticsDerivedStream(this.edgeUnderCursor, receiverDiagnosticsStatistic))));
                }
            }

            // Add a heatmap statistics context menu
            var heatmapStatisticsItems = new ContextMenuItemInfo("Heatmap Statistics");
            items.Add(heatmapStatisticsItems);

            foreach (var heatmapStat in Enum.GetValues(typeof(HeatmapStats)))
            {
                var heatmapStatValue = (HeatmapStats)heatmapStat;
                var heatmapStatName = heatmapStatValue switch
                {
                    HeatmapStats.None => "None",
                    HeatmapStats.AvgMessageCreatedLatency => "Message Created Latency (Average)",
                    HeatmapStats.AvgMessageEmittedLatency => "Message Emitted Latency (Average)",
                    HeatmapStats.AvgMessageReceivedLatency => "Message Received Latency (Average)",
                    HeatmapStats.AvgDeliveryQueueSize => "Delivery Queue Size (Average)",
                    HeatmapStats.AvgMessageProcessTime => "Message Process Time (Average)",
                    HeatmapStats.TotalMessageEmittedCount => "Total Messages Emitted (Count)",
                    HeatmapStats.TotalMessageDroppedCount => "Total Messages Dropped (Count)",
                    HeatmapStats.TotalMessageDroppedPercentage => "Total Messages Dropped (%)",
                    HeatmapStats.TotalMessageProcessedCount => "Total Messages Processed (Count)",
                    HeatmapStats.AvgMessageSize => "Message Size (Average)",
                    _ => throw new NotImplementedException(),
                };

                heatmapStatisticsItems.SubItems.Add(
                    new ContextMenuItemInfo(
                        this.HeatmapStatistics == heatmapStatValue ? IconSourcePath.Checkmark : null,
                        heatmapStatName,
                        new PsiCommand(() => this.HeatmapStatistics = heatmapStatValue)));
            }

            items.AddRange(base.ContextMenuItemsInfo());
            return items;
        }

        /// <summary>
        /// Updates the edge currently under the cursor.
        /// </summary>
        /// <param name="edgeUnderCursor">Updates the edge under the cursor.</param>
        public void UpdateEdgeUnderCursor(int edgeUnderCursor)
        {
            this.edgeUnderCursor = edgeUnderCursor;
        }

        /// <summary>
        /// Adds a specified derived receiver diagnostics stream.
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        /// <param name="receiverDiagnosticsStatistic">The name of the receiver diagnostics to use.</param>
        public void AddReceiverDiagnosticsDerivedStream(int receiverId, string receiverDiagnosticsStatistic)
        {
            // Find the corresponding partition view model
            var partitionViewModel = VisualizationContext.Instance
                .DatasetViewModel
                .CurrentSessionViewModel
                .PartitionViewModels
                .FirstOrDefault(p => p.Name == this.StreamBinding.PartitionName);

            // Find the pipeline diagnostics node
            var diagnosticsNode = partitionViewModel.FindStreamTreeNode(this.StreamBinding.StreamName);

            // Figure out the type and extractor function for the statistic of interest
            var receiverDiagnosticsStatisticType = this.GetTypeForReceiverDiagnosticsStatistic(receiverDiagnosticsStatistic);
            var receiverDiagnosticsExtractorFunction = this.GetExtractorFunctionForReceiverDiagnosticsStatistic(receiverDiagnosticsStatistic);

            // Add the child node
            var receiverDiagnosticsNode = diagnosticsNode.AddChild(
                $"ReceiverDiagnostics.{receiverId}.{receiverDiagnosticsStatistic}",
                diagnosticsNode.SourceStreamMetadata,
                typeof(PipelineDiagnosticsToReceiverDiagnosticsMemberStreamAdapter<>).MakeGenericType(receiverDiagnosticsStatisticType),
                new object[] { receiverId, receiverDiagnosticsExtractorFunction });

            // Select it and expand the diagnostics node
            if (receiverDiagnosticsNode != null)
            {
                partitionViewModel.SelectStreamTreeNode(receiverDiagnosticsNode.FullName);
                diagnosticsNode.ExpandAll();
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Warning",
                        $"The receiver diagnostics statistic derived stream not added because derived streams with the same names already exist.",
                        "Close",
                        null).ShowDialog();
                }));
            }
        }

        /// <inheritdoc />
        protected override void OnStreamUnbound()
        {
            base.OnStreamUnbound();
            this.ModelDirty = true;
        }

        private Type GetTypeForReceiverDiagnosticsStatistic(string receiverDiagnosticsStatistic)
            => receiverDiagnosticsStatistic switch
            {
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageEmittedLatency) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageCreatedLatency) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageProcessTime) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageReceivedLatency) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageSize) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgDeliveryQueueSize) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageEmittedLatency) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageCreatedLatency) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageProcessTime) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageReceivedLatency) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageSize) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastDeliveryQueueSize) => typeof(double),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.ReceiverIsThrottled) => typeof(bool),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageDroppedCount) => typeof(int),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageEmittedCount) => typeof(int),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageProcessedCount) => typeof(int),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageDroppedCount) => typeof(int),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageEmittedCount) => typeof(int),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageProcessedCount) => typeof(int),
                _ => throw new ArgumentException($"Unknown receiver diagnostics statistic: {receiverDiagnosticsStatistic}")
            };

        private object GetExtractorFunctionForReceiverDiagnosticsStatistic(string receiverDiagnosticsStatistic)
            => receiverDiagnosticsStatistic switch
            {
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageEmittedLatency) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.AvgMessageEmittedLatency),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageCreatedLatency) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.AvgMessageCreatedLatency),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageProcessTime) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.AvgMessageProcessTime),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageReceivedLatency) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.AvgMessageReceivedLatency),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgMessageSize) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.AvgMessageSize),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.AvgDeliveryQueueSize) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.AvgDeliveryQueueSize),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageEmittedLatency) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.LastMessageEmittedLatency),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageCreatedLatency) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.LastMessageCreatedLatency),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageProcessTime) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.LastMessageProcessTime),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageReceivedLatency) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.LastMessageReceivedLatency),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastMessageSize) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.LastMessageSize),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.LastDeliveryQueueSize) => (Func<PipelineDiagnostics.ReceiverDiagnostics, double>)(rd => rd.LastDeliveryQueueSize),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.ReceiverIsThrottled) => (Func<PipelineDiagnostics.ReceiverDiagnostics, bool>)(rd => rd.ReceiverIsThrottled),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageDroppedCount) => (Func<PipelineDiagnostics.ReceiverDiagnostics, int>)(rd => rd.TotalMessageDroppedCount),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageEmittedCount) => (Func<PipelineDiagnostics.ReceiverDiagnostics, int>)(rd => rd.TotalMessageEmittedCount),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.TotalMessageProcessedCount) => (Func<PipelineDiagnostics.ReceiverDiagnostics, int>)(rd => rd.TotalMessageProcessedCount),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageDroppedCount) => (Func<PipelineDiagnostics.ReceiverDiagnostics, int>)(rd => rd.WindowMessageDroppedCount),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageEmittedCount) => (Func<PipelineDiagnostics.ReceiverDiagnostics, int>)(rd => rd.WindowMessageEmittedCount),
                nameof(PipelineDiagnostics.ReceiverDiagnostics.WindowMessageProcessedCount) => (Func<PipelineDiagnostics.ReceiverDiagnostics, int>)(rd => rd.WindowMessageProcessedCount),
                _ => throw new ArgumentException($"Unknown receiver diagnostics statistic: {receiverDiagnosticsStatistic}")
            };
    }
}
