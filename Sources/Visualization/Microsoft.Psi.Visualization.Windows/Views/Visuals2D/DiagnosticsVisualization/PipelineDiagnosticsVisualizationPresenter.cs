// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Msagl.Drawing;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DiagnosticsVisualizationObjectView.xaml.
    /// </summary>
    public partial class PipelineDiagnosticsVisualizationPresenter
    {
        private readonly PipelineDiagnosticsVisualizationModel model;
        private readonly PipelineDiagnosticsVisualizationObjectView view;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsVisualizationPresenter"/> class.
        /// </summary>
        /// <param name="view">Diagnostics view.</param>
        /// <param name="visualizationObject">Visualization object for this presenter.</param>
        public PipelineDiagnosticsVisualizationPresenter(PipelineDiagnosticsVisualizationObjectView view, PipelineDiagnosticsVisualizationObject visualizationObject)
        {
            this.model = new PipelineDiagnosticsVisualizationModel();
            this.view = view;
            this.UpdateSettings(visualizationObject);
        }

        /// <summary>
        /// Gets diagnostics graph.
        /// </summary>
        public PipelineDiagnostics DiagnosticsGraph => this.model.Graph;

        /// <summary>
        /// Gets visual graph. TODO: arg to update.
        /// </summary>
        public Graph VisualGraph { get; private set; }

        /// <summary>
        /// Gets details of selected edge.
        /// </summary>
        public string SelectedEdgeDetails => this.model.SelectedEdgeDetails;

        /// <summary>
        /// Gets the alpha to be used for unhighlighted edges.
        /// </summary>
        public byte UnhighlightedEdgeAlpha { get; private set; }

        /// <summary>
        /// Gets edge color.
        /// </summary>
        public Color EdgeColor { get; private set; } //// TODO: private

        /// <summary>
        /// Gets node color.
        /// </summary>
        public Color NodeColor { get; private set; }

        /// <summary>
        /// Gets source node color.
        /// </summary>
        public Color SourceNodeColor { get; private set; }

        /// <summary>
        /// Gets subpipeline color.
        /// </summary>
        public Color SubpipelineColor { get; private set; }

        /// <summary>
        /// Gets connector node color.
        /// </summary>
        public Color ConnectorColor { get; private set; }

        /// <summary>
        /// Gets join node color.
        /// </summary>
        public Color JoinColor { get; private set; }

        /// <summary>
        /// Gets label color (light).
        /// </summary>
        public Color LabelColorLight { get; private set; }

        /// <summary>
        /// Gets label color (dark).
        /// </summary>
        public Color LabelColorDark { get; private set; }

        /// <summary>
        /// Gets heatmap color (base).
        /// </summary>
        public Color HeatmapColorBase { get; private set; }

        /// <summary>
        /// Gets info text size.
        /// </summary>
        public double InfoTextSize { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to show exporter connections.
        /// </summary>
        public bool ShowExporterConnections { get; private set; }

        /// <summary>
        /// Gets breadcrumb graph IDs.
        /// </summary>
        public IEnumerable<int> Breadcrumbs
        {
            get
            {
                return this.model.NavStack.Reverse();
            }
        }

        /// <summary>
        /// Update diagnostics configuration.
        /// </summary>
        /// <param name="visualizationObject">Diagnostics visualization object.</param>
        public void UpdateSettings(PipelineDiagnosticsVisualizationObject visualizationObject)
        {
            // convert colors to MSAGL graph colors
            static Color ColorFromMediaColor(System.Windows.Media.Color color) => new (color.R, color.G, color.B);

            this.model.VisualizationObject = visualizationObject;
            this.EdgeColor = ColorFromMediaColor(visualizationObject.EdgeColor);
            this.UnhighlightedEdgeAlpha = (byte)(Math.Max(0, Math.Min(100, visualizationObject.HighlightOpacity)) * 255.0 / 100.0);
            this.NodeColor = ColorFromMediaColor(visualizationObject.NodeColor);
            this.SourceNodeColor = ColorFromMediaColor(visualizationObject.SourceNodeColor);
            this.SubpipelineColor = ColorFromMediaColor(visualizationObject.SubpipelineColor);
            this.ConnectorColor = ColorFromMediaColor(visualizationObject.ConnectorColor);
            this.JoinColor = ColorFromMediaColor(visualizationObject.JoinColor);
            this.HeatmapColorBase = ColorFromMediaColor(visualizationObject.HeatmapColor);
            this.InfoTextSize = visualizationObject.InfoTextSize;
            this.ShowExporterConnections = visualizationObject.ShowExporterConnections;
            this.LabelColorLight = Color.White;
            this.LabelColorDark = Color.Black;
            if (visualizationObject.ModelDirty)
            {
                this.model.Reset();
                visualizationObject.ModelDirty = false;
                this.VisualGraph = null;
                this.view.Update(true);
            }

            if (this.model.Graph != null)
            {
                this.UpdateGraph(this.model.Graph, true);
            }
        }

        /// <summary>
        /// Update diagnostics graph.
        /// </summary>
        /// <param name="graph">Current diagnostics graph.</param>
        /// <param name="forceRelayout">Force re-layout of graph (otherwise, updates labels, colors, etc. in place).</param>
        public void UpdateGraph(PipelineDiagnostics graph, bool forceRelayout)
        {
            // If the model visualization object is dirty, rebuild
            if (this.model.VisualizationObject.ModelDirty)
            {
                this.model.Reset();
                this.model.VisualizationObject.ModelDirty = false;
                this.VisualGraph = null;
                this.view.Update(true);
            }

            this.model.Graph = graph;
            if (graph == null)
            {
                this.VisualGraph = null;
            }
            else
            {
                var pipelineIdToPipelineDiagnostics = graph.GetAllPipelineDiagnostics().ToDictionary(p => p.Id);
                var currentGraph = this.Breadcrumbs.Count() > 0 ? pipelineIdToPipelineDiagnostics[this.Breadcrumbs.Last()] : graph;
                this.VisualGraph = this.BuildVisualGraph(currentGraph, pipelineIdToPipelineDiagnostics);
            }

            this.view.Update(forceRelayout);
        }

        /// <summary>
        /// Clear the selected edge.
        /// </summary>
        public void ClearSelectedEdge()
        {
            this.model.SelectedEdgeId = -1;
            this.view.Update(true);
        }

        /// <summary>
        /// Update selected edge.
        /// </summary>
        /// <param name="edge">The edge to update.</param>
        public void UpdateReceiverDiagnostics(Edge edge)
        {
            var receiverDiagnostics = this.GetReceiverDiagnostics(edge);
            if (receiverDiagnostics != null)
            {
                this.UpdateReceiverDiagnostics(receiverDiagnostics, this.VisualGraph, true);
            }
            else
            {
                this.model.SelectedEdgeDetails = string.Empty;
                this.model.SelectedEdgeId = -1;
                this.view.Update(true);
                return;
            }
        }

        /// <summary>
        /// Adds a derived receiver diagnostics streams for a specified receiver id and receiver statistic.
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        /// <param name="receiverDiagnosticsStatistic">The receiver diagnostics statistic.</param>
        public void AddDerivedReceiverDiagnosticsStreams(int receiverId, string receiverDiagnosticsStatistic)
        {
            this.model.VisualizationObject.AddReceiverDiagnosticsDerivedStream(receiverId, receiverDiagnosticsStatistic);
        }

        /// <summary>
        /// Navigate into subgraph.
        /// </summary>
        /// <param name="subgraphId">Subgraph ID into which to navigate.</param>
        public void NavInto(int subgraphId)
        {
            this.model.NavStack.Push(subgraphId);
            this.UpdateGraph(this.model.Graph, true);
        }

        /// <summary>
        /// Navigate back one graph.
        /// </summary>
        public void NavBack()
        {
            if (this.model.NavStack.Count > 0)
            {
                this.model.NavStack.Pop();
                this.UpdateGraph(this.model.Graph, true);
            }
        }

        /// <summary>
        /// Navigate back to given graph.
        /// </summary>
        /// <param name="id">Graph Id.</param>
        public void NavBackTo(int id)
        {
            while (this.model.NavStack.Count > 0 && this.model.NavStack.Peek() != id)
            {
                this.model.NavStack.Pop();
            }

            this.UpdateGraph(this.model.Graph, true);
        }

        /// <summary>
        /// Navigate back to root graph.
        /// </summary>
        public void NavHome()
        {
            if (this.model.NavStack.Count > 0)
            {
                while (this.model.NavStack.Count > 0)
                {
                    this.model.NavStack.Pop();
                }

                this.UpdateGraph(this.model.Graph, true);
            }
        }

        private static Edge GetEdgeById(int id, Graph graph)
        {
            if (id < 0)
            {
                throw new InvalidOperationException("Cannot get edge for negative id.");
            }

            foreach (var n in graph.Nodes)
            {
                foreach (var e in n.Edges)
                {
                    if ((int)e.UserData == id)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        private static bool IsBridgeToExporter(PipelineDiagnostics.PipelineElementDiagnostics node)
        {
            if (node.TypeName == nameof(PsiExporter))
            {
                return true;
            }

            if (node.ConnectorBridgeToPipelineElement == null)
            {
                return false;
            }

            var bridgeEmitters = node.ConnectorBridgeToPipelineElement.Emitters;
            var typeName = bridgeEmitters.Length == 1 ? bridgeEmitters[0].PipelineElement.TypeName : string.Empty;
            return typeName == "MessageConnector`1" || typeName == "MessageEnvelopeConnector`1";
        }

        private void UpdateReceiverDiagnostics(PipelineDiagnostics.ReceiverDiagnostics receiverDiagnostics, Graph graph, bool clicked)
        {
            var edge = GetEdgeById(receiverDiagnostics.Id, graph);
            if (clicked && this.model.SelectedEdgeId == receiverDiagnostics.Id)
            {
                // toggle unselected
                edge.Attr.LineWidth = this.model.VisualizationObject.EdgeLineThickness; // unselect current
                this.model.SelectedEdgeDetails = string.Empty;
                this.model.SelectedEdgeId = -1;
                this.view.Update(true);
                return;
            }

            // new edge selected
            if (this.model.SelectedEdgeId != -1)
            {
                var previousEdge = GetEdgeById(this.model.SelectedEdgeId, graph);
                if (previousEdge != null)
                {
                    previousEdge.Attr.LineWidth = this.model.VisualizationObject.EdgeLineThickness; // unselect previous
                }
            }

            edge.Attr.LineWidth = this.model.VisualizationObject.EdgeLineThickness * 2; // select current
            this.model.SelectedEdgeId = receiverDiagnostics.Id;
            var sb = new StringBuilder();
            sb.Append($"Id: {receiverDiagnostics.Id}" + Environment.NewLine);
            sb.Append($"Type: {TypeSpec.Simplify(receiverDiagnostics.TypeName)}" + Environment.NewLine);
            sb.Append($"Delivery Policy: {receiverDiagnostics.DeliveryPolicyName}" + Environment.NewLine);
            sb.Append($"Delivery Queue Size (avg): {receiverDiagnostics.AvgDeliveryQueueSize:0.###}" + Environment.NewLine);
            sb.Append($"Delivery Queue Size (last): {receiverDiagnostics.LastDeliveryQueueSize:0.###}" + Environment.NewLine);
            sb.Append($"# Emitted (total): {receiverDiagnostics.TotalMessageEmittedCount}" + Environment.NewLine);
            sb.Append($"# Emitted (window): {receiverDiagnostics.WindowMessageEmittedCount:0.###}" + Environment.NewLine);
            sb.Append($"# Processed (total): {receiverDiagnostics.TotalMessageProcessedCount}" + Environment.NewLine);
            sb.Append($"# Processed (window): {receiverDiagnostics.WindowMessageProcessedCount:0.###}" + Environment.NewLine);
            sb.Append($"# Dropped (total): {receiverDiagnostics.TotalMessageDroppedCount}" + Environment.NewLine);
            sb.Append($"# Dropped (window): {receiverDiagnostics.WindowMessageDroppedCount:0.###}" + Environment.NewLine);
            sb.Append($"Message Size (avg): {receiverDiagnostics.AvgMessageSize:0}" + Environment.NewLine);
            sb.Append($"Message Size (last): {receiverDiagnostics.LastMessageSize:0}" + Environment.NewLine);
            sb.Append($"Message Created Latency (avg): {receiverDiagnostics.AvgMessageCreatedLatency:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Created Latency (last): {receiverDiagnostics.LastMessageCreatedLatency:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Emitted Latency (avg): {receiverDiagnostics.AvgMessageEmittedLatency:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Emitted Latency (last): {receiverDiagnostics.LastMessageEmittedLatency:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Received Latency (avg): {receiverDiagnostics.AvgMessageReceivedLatency:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Received Latency (last): {receiverDiagnostics.LastMessageReceivedLatency:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Process Time (avg): {receiverDiagnostics.AvgMessageProcessTime:0.###}ms" + Environment.NewLine);
            sb.Append($"Message Process Time (last): {receiverDiagnostics.LastMessageProcessTime:0.###}ms" + Environment.NewLine);
            this.model.SelectedEdgeDetails = sb.ToString();
            this.view.Update(clicked);
        }

        private Func<PipelineDiagnostics.ReceiverDiagnostics, double> StatsSelector(bool heatmap)
        {
            return this.model.VisualizationObject.HeatmapStatistics switch
            {
                PipelineDiagnosticsVisualizationObject.HeatmapStats.None => null,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgDeliveryQueueSize => i => i.AvgDeliveryQueueSize,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.TotalMessageEmittedCount => i => i.TotalMessageEmittedCount,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.TotalMessageProcessedCount => i => i.TotalMessageProcessedCount,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.TotalMessageDroppedCount => i => i.TotalMessageDroppedCount,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.TotalMessageDroppedPercentage => i =>
                    i.TotalMessageEmittedCount > 0 ? 100 * i.TotalMessageDroppedCount / (double)i.TotalMessageEmittedCount : double.NaN,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgMessageSize => i =>
                {
                    var avg = i.AvgMessageSize;
                    return heatmap && avg > 0 ? Math.Log(avg) : avg;
                },
                PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgMessageCreatedLatency => i => i.AvgMessageCreatedLatency,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgMessageEmittedLatency => i => i.AvgMessageEmittedLatency,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgMessageReceivedLatency => i => i.AvgMessageReceivedLatency,
                PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgMessageProcessTime => i => i.AvgMessageProcessTime,
                _ => throw new ArgumentException($"Unknown visualization selector type."),
            };
        }

        private Color LabelColor(Color background)
        {
            var r = background.R / 255.0;
            var g = background.G / 255.0;
            var b = background.B / 255.0;
            var brightness = Math.Sqrt((0.299 * r * r) + (0.587 * g * g) + (0.114 * b * b));
            return brightness < 0.55 ? this.LabelColorLight : this.LabelColorDark;
        }

        private bool HilightEdge(PipelineDiagnostics.ReceiverDiagnostics receiverDiagnostics)
        {
            return this.model.VisualizationObject.Highlight switch
            {
                PipelineDiagnosticsVisualizationObject.HighlightCondition.None => true,
                PipelineDiagnosticsVisualizationObject.HighlightCondition.UnlimitedDeliveryPolicy => receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.Unlimited)),
                PipelineDiagnosticsVisualizationObject.HighlightCondition.LatestMessageDeliveryPolicy => receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.LatestMessage)),
                PipelineDiagnosticsVisualizationObject.HighlightCondition.ThrottleDeliveryPolicy => receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.Throttle)),
                PipelineDiagnosticsVisualizationObject.HighlightCondition.SynchronousOrThrottleDeliveryPolicy => receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.SynchronousOrThrottle)),
                PipelineDiagnosticsVisualizationObject.HighlightCondition.LatencyConstrainedDeliveryPolicy => receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.LatencyConstrained)),
                PipelineDiagnosticsVisualizationObject.HighlightCondition.QueueSizeConstrainedDeliveryPolicy => receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.QueueSizeConstrained)),
                PipelineDiagnosticsVisualizationObject.HighlightCondition.ThrottledReceivers => receiverDiagnostics.ReceiverIsThrottled,
                _ => throw new ArgumentException($"Unknown highlight condition: {this.model.VisualizationObject.Highlight}"),
            };
        }

        private Color HeatmapColor(double stats, Color baseColor)
        {
            var baseR = baseColor.R * (1.0 - stats);
            var baseG = baseColor.G * (1.0 - stats);
            var baseB = baseColor.B * (1.0 - stats);
            var heatR = this.HeatmapColorBase.R * stats;
            var heatG = this.HeatmapColorBase.G * stats;
            var heatB = this.HeatmapColorBase.B * stats;
            return new Color((byte)(baseR + heatR), (byte)(baseG + heatG), (byte)(baseB + heatB)); // blend from base to heat color
        }

        private PipelineDiagnostics.ReceiverDiagnostics GetReceiverDiagnostics(Edge edge) =>
            this.DiagnosticsGraph.GetAllReceiverDiagnostics().FirstOrDefault(rd => rd.Id == (int)edge.UserData);

        private void UpdateColoring(Graph graph)
        {
            // visualize heatmap
            if (graph.Edges.Count() == 0)
            {
                return;
            }

            var statsSelector = this.StatsSelector(true);
            var edgeStats = graph.Edges.Where(e => (int)e.UserData != -1).Select(e => (e, statsSelector != null ? statsSelector(this.GetReceiverDiagnostics(e)) : 0));
            var max = edgeStats.Select(x => x.Item2).Max();
            var colorNodes = this.model.VisualizationObject.HeatmapStatistics == PipelineDiagnosticsVisualizationObject.HeatmapStats.AvgMessageProcessTime;

            // Color all nodes
            foreach (var node in graph.Nodes)
            {
                var inputs = node.InEdges;
                if (inputs.Count() > 0)
                {
                    if (colorNodes)
                    {
                        var maxStats = node.InEdges.Where(e => (int)e.UserData != -1).Select(e => statsSelector(this.GetReceiverDiagnostics(e))).Max();
                        var color = this.HeatmapColor(max > 0 ? maxStats / max : 0, this.NodeColor);
                        node.Attr.Color = color;
                        node.Attr.FillColor = color;
                        node.Label.FontColor = this.LabelColor(color);
                    }
                    else
                    {
                        node.Attr.Color = this.NodeColor;
                        node.Attr.FillColor = this.NodeColor;
                        node.Label.FontColor = this.LabelColor(this.NodeColor);
                    }
                }
            }

            // Color the edges
            foreach (var (edge, stat) in edgeStats)
            {
                var edgeColor = this.HeatmapColor(max > 0 ? stat / max : 0, this.EdgeColor);
                if ((int)edge.UserData != -1 && this.HilightEdge(this.GetReceiverDiagnostics(edge)))
                {
                    edge.Attr.Color = edgeColor;
                    edge.Label.FontColor = edgeColor;
                }
                else
                {
                    var fadedEdgeColor = new Color(this.UnhighlightedEdgeAlpha, edgeColor.R, edgeColor.G, edgeColor.B);
                    edge.Attr.Color = fadedEdgeColor;
                    edge.Label.FontColor = fadedEdgeColor;
                }
            }
        }

        private bool IsConnectorBridge(PipelineDiagnostics.PipelineElementDiagnostics node)
        {
            return node.Kind == PipelineElementKind.Connector && (node.Receivers.Length == 0 || node.Emitters.Length == 0) && node.ConnectorBridgeToPipelineElement != null;
        }

        private Node BuildVisualNode(PipelineDiagnostics.PipelineElementDiagnostics node)
        {
            var vis = new Node($"n{node.Id}");
            var fillColor = node.Kind == PipelineElementKind.Source ? this.SourceNodeColor : node.Kind == PipelineElementKind.Subpipeline ? this.SubpipelineColor : this.NodeColor;
            var typ = TypeSpec.Simplify(node.TypeName);
            var isStoppedSubpipeline = node.RepresentsSubpipeline != null && !node.RepresentsSubpipeline.IsPipelineRunning;
            var stopped = isStoppedSubpipeline || !node.IsRunning ? " (stopped)" : string.Empty;
            vis.LabelText = node.Kind == PipelineElementKind.Subpipeline ? $"{node.Name}{stopped}|{typ}" : (node.DiagnosticState ?? typ);
            vis.Label.FontColor = this.LabelColor(fillColor);
            vis.Attr.Color = fillColor;
            vis.Attr.FillColor = fillColor;
            if ((vis.LabelText.StartsWith("Join(") && vis.LabelText.EndsWith(")")) ||
                (vis.LabelText.StartsWith("Fuse(") && vis.LabelText.EndsWith(")")))
            {
                this.SetFuseVisualAttributes(vis, node.DiagnosticState);
            }

            return vis;
        }

        private string BuildVisualEdgeLabelText(string emitterName, string receiverName, string stats, string deliveryPolicyName)
        {
            var showEmitter = this.model.VisualizationObject.ShowEmitterNames;
            var showReceiver = this.model.VisualizationObject.ShowReceiverNames;
            var showDeliveryPolicy = this.model.VisualizationObject.ShowDeliveryPolicies;
            emitterName = showEmitter ? emitterName : string.Empty;
            receiverName = showReceiver ? receiverName : string.Empty;
            deliveryPolicyName = showDeliveryPolicy && deliveryPolicyName.Length > 0 ? $" [{deliveryPolicyName}]" : string.Empty;
            var arrow = showEmitter && showReceiver ? "→" : string.Empty;
            return $"     {emitterName}{arrow}{receiverName}{stats}{deliveryPolicyName}     "; // extra padding to allow for stats changes without re-layout
        }

        private Edge BuildVisualEdge(Node source, Node target, string emitterName, string receiverName, int receiverId, string stats, string deliveryPolicyName, bool crossPipelines)
        {
            static bool IsLossy(string deliveryPolicyName)
                => deliveryPolicyName != DeliveryPolicy.Unlimited.Name &&
                   deliveryPolicyName != DeliveryPolicy.SynchronousOrThrottle.Name &&
                   !deliveryPolicyName.StartsWith(nameof(DeliveryPolicy.Throttle));

            var edge = new Edge(source, target, ConnectionToGraph.Connected);
            edge.Attr.Color = this.EdgeColor;
            edge.Attr.LineWidth = this.model.VisualizationObject.EdgeLineThickness;
            edge.Attr.AddStyle(crossPipelines ? Style.Dashed : (this.model.VisualizationObject.ShowLossyDeliveryPoliciesAsDotted && IsLossy(deliveryPolicyName) ? Style.Dotted : Style.Solid));
            edge.LabelText = this.BuildVisualEdgeLabelText(emitterName, receiverName, stats, deliveryPolicyName);
            edge.Label.FontColor = this.LabelColorLight;
            edge.Label.UserData = edge;
            edge.UserData = receiverId;
            return edge;
        }

        private Edge BuildVisualEdge(Node source, Node target, PipelineDiagnostics.ReceiverDiagnostics input, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            var stats = statsSelector != null ? $" ({statsSelector(input):0.#})" : string.Empty;
            return this.BuildVisualEdge(source, target, input.Source.Name, input.ReceiverName, input.Id, stats, input.DeliveryPolicyName, false);
        }

        private bool AddVisualEdge(Node source, Node target, PipelineDiagnostics.ReceiverDiagnostics input, Graph graph, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            if (source != null && target != null)
            {
                var edge = this.BuildVisualEdge(source, target, input, statsSelector);
                graph.AddPrecalculatedEdge(edge);
                if (input.Id == this.model.SelectedEdgeId)
                {
                    this.UpdateReceiverDiagnostics(input, graph, false);
                    return true;
                }
            }

            return false;
        }

        private bool AddVisualEdge(int sourceId, int targetId, PipelineDiagnostics.ReceiverDiagnostics input, Graph graph, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            return this.AddVisualEdge(graph.FindNode($"n{sourceId}"), graph.FindNode($"n{targetId}"), input, graph, statsSelector);
        }

        private void SetVisualAttributes(Node vis, Shape shape, Color color, string symbol, string name)
        {
            vis.Attr.Color = color;
            vis.Attr.FillColor = color;
            vis.Label.FontColor = this.LabelColor(color);
            vis.Attr.Shape = shape;
            vis.LabelText = $"{symbol}|{name}";
        }

        private void SetConnectorVisualAttributes(Node vis, string label)
        {
            this.SetVisualAttributes(vis, Shape.Circle, this.ConnectorColor, "☍", label);
        }

        private void SetFuseVisualAttributes(Node vis, string label)
        {
            this.SetVisualAttributes(vis, Shape.Circle, this.JoinColor, "+", label);
        }

        private Graph BuildVisualGraph(PipelineDiagnostics diagnostics, Dictionary<int, PipelineDiagnostics> pipelineIdToPipelineDiagnostics)
        {
            var subpipelineIdToPipelineDiagnostics = diagnostics.SubpipelineDiagnostics.ToDictionary(p => p.Id);
            var graph = new Graph($"{diagnostics.Name} (running={diagnostics.IsPipelineRunning})", $"g{diagnostics.Id}");
            switch (this.model.VisualizationObject.LayoutDirection)
            {
                case PipelineDiagnosticsVisualizationObject.GraphLayoutDirection.LeftToRight:
                    graph.Attr.LayerDirection = LayerDirection.LR;
                    break;
                case PipelineDiagnosticsVisualizationObject.GraphLayoutDirection.TopToBottom:
                    graph.Attr.LayerDirection = LayerDirection.TB;
                    break;
                case PipelineDiagnosticsVisualizationObject.GraphLayoutDirection.RightToLeft:
                    graph.Attr.LayerDirection = LayerDirection.RL;
                    break;
                case PipelineDiagnosticsVisualizationObject.GraphLayoutDirection.BottomToTop:
                    graph.Attr.LayerDirection = LayerDirection.BT;
                    break;
            }

            graph.UserData = diagnostics.Id;
            graph.Attr.BackgroundColor = Color.Transparent;
            var subpipelineNodes = new Dictionary<int, PipelineDiagnostics.PipelineElementDiagnostics>();
            var connectorsWithinSubpipelines = new Dictionary<int, PipelineDiagnostics.PipelineElementDiagnostics>();
            var statsSelector = this.StatsSelector(false);

            // add nodes
            foreach (var node in diagnostics.PipelineElements.Where(n => !this.IsConnectorBridge(n)))
            {
                var vis = this.BuildVisualNode(node);
                if (node.Kind == PipelineElementKind.Subpipeline && node.RepresentsSubpipeline != null)
                {
                    vis.UserData = node.RepresentsSubpipeline;
                    subpipelineNodes.Add(node.RepresentsSubpipeline.Id, node);
                    foreach (var n in node.RepresentsSubpipeline.PipelineElements.Where(n => n.Kind == PipelineElementKind.Connector))
                    {
                        connectorsWithinSubpipelines.Add(n.Id, n);
                    }
                }
                else if (node.Kind == PipelineElementKind.Connector)
                {
                    this.SetConnectorVisualAttributes(vis, node.Name);
                }

                graph.AddNode(vis);
            }

            // add connectors
            foreach (var node in diagnostics.PipelineElements.Where(this.IsConnectorBridge))
            {
                var connectsToSubpipeline = subpipelineNodes.ContainsKey(node.ConnectorBridgeToPipelineElement.PipelineId);
                if (!connectsToSubpipeline)
                {
                    if (!this.ShowExporterConnections && IsBridgeToExporter(node))
                    {
                        continue;
                    }

                    var connector = new Node($"n{node.Id}");
                    var bridgedPipeline = pipelineIdToPipelineDiagnostics[node.ConnectorBridgeToPipelineElement.PipelineId];
                    this.SetConnectorVisualAttributes(connector, $"{node.Name} ({bridgedPipeline.Name})");
                    graph.AddNode(connector);
                }
            }

            // add edges
            var selectedEdgeUpdated = false;
            foreach (var n in diagnostics.PipelineElements)
            {
                foreach (var i in n.Receivers)
                {
                    if (i.Source != null)
                    {
                        if (this.AddVisualEdge(i.Source.PipelineElement.Id, n.Id, i, graph, statsSelector))
                        {
                            selectedEdgeUpdated = true;
                        }
                    }
                }
            }

            // add connector bridge edges
            foreach (var n in diagnostics.PipelineElements.Where(this.IsConnectorBridge))
            {
                if (!this.ShowExporterConnections && IsBridgeToExporter(n))
                {
                    continue;
                }

                // connector bridging to subpipeline?
                if (subpipelineNodes.TryGetValue(n.ConnectorBridgeToPipelineElement.PipelineId, out PipelineDiagnostics.PipelineElementDiagnostics subNode))
                {
                    // edges from connector source directly to bridge target (subpipeline)
                    var sub = graph.FindNode($"n{subNode.Id}");
                    if (sub != null)
                    {
                        foreach (var i in n.Receivers)
                        {
                            if (i.Source != null)
                            {
                                var source = graph.FindNode($"n{i.Source.PipelineElement.Id}");
                                if (source != null)
                                {
                                    if (this.AddVisualEdge(source, sub, i, graph, statsSelector))
                                    {
                                        selectedEdgeUpdated = true;
                                    }
                                }
                            }
                        }

                        // edges from connector bridge source (subpipeline) to connector targets
                        foreach (var o in n.Emitters)
                        {
                            foreach (var t in o.Targets)
                            {
                                var target = graph.FindNode($"n{t.PipelineElement.Id}");
                                if (target != null)
                                {
                                    if (this.AddVisualEdge(sub, target, t, graph, statsSelector))
                                    {
                                        selectedEdgeUpdated = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // connector bridging graphs
                    var bridgedPipeline = pipelineIdToPipelineDiagnostics[n.ConnectorBridgeToPipelineElement.PipelineId];
                    var connector = graph.FindNode($"n{n.Id}");

                    // add dotted line edge representing connector bridged to descendant pipeline
                    var targetPipeline = bridgedPipeline;
                    while (targetPipeline != null)
                    {
                        if (subpipelineIdToPipelineDiagnostics.ContainsKey(targetPipeline.Id))
                        {
                            var targetNode = graph.FindNode($"n{subpipelineNodes[targetPipeline.Id].Id}");
                            graph.AddPrecalculatedEdge(this.BuildVisualEdge(connector, targetNode, n.Name, bridgedPipeline.Name, -1, string.Empty, string.Empty, true));
                            break;
                        }

                        // walk up ancestor chain until we're at a direct child subpipeline
                        targetPipeline = targetPipeline.ParentPipelineDiagnostics;
                    }
                }
            }

            // add connector bridge edges between descendants (shown between current-level subpiplines)
            int? TryFindCurrentLevelAncestorSubpipelineId(int id)
            {
                foreach (var ancestor in pipelineIdToPipelineDiagnostics[id].AncestorPipelines)
                {
                    if (subpipelineNodes.TryGetValue(ancestor.Id, out PipelineDiagnostics.PipelineElementDiagnostics subpipeline))
                    {
                        return subpipeline.Id;
                    }
                }

                return null;
            }

            foreach (var descendantConnector in diagnostics.GetAllPipelineElementDiagnostics().Where(this.IsConnectorBridge))
            {
                if (descendantConnector.Emitters.Length == 0 /* source-side of connector pair */)
                {
                    var sourceId = descendantConnector.PipelineId;
                    var targetId = descendantConnector.ConnectorBridgeToPipelineElement.PipelineId;
                    var sourceCurrentLevelId = TryFindCurrentLevelAncestorSubpipelineId(sourceId);
                    var targetCurrentLevelId = TryFindCurrentLevelAncestorSubpipelineId(targetId);
                    if (sourceCurrentLevelId != null && targetCurrentLevelId != null && sourceCurrentLevelId != targetCurrentLevelId)
                    {
                        var sourceNode = graph.FindNode($"n{sourceCurrentLevelId}");
                        var targetNode = graph.FindNode($"n{targetCurrentLevelId}");
                        graph.AddPrecalculatedEdge(this.BuildVisualEdge(sourceNode, targetNode, string.Empty, descendantConnector.Name, -1, string.Empty, string.Empty, true));
                    }
                }
            }

            // add direct connections from one subpipeline (connector) to another
            foreach (var c in connectorsWithinSubpipelines.Values)
            {
                if (c.ConnectorBridgeToPipelineElement != null)
                {
                    if (c.ConnectorBridgeToPipelineElement.PipelineId == diagnostics.Id && c.ConnectorBridgeToPipelineElement.Receivers.Length == 1)
                    {
                        var i = c.ConnectorBridgeToPipelineElement.Receivers[0];
                        if (i.Source != null && i.Source.PipelineElement.PipelineId == diagnostics.Id && i.Source.PipelineElement.ConnectorBridgeToPipelineElement != null)
                        {
                            if (subpipelineNodes.TryGetValue(i.Source.PipelineElement.ConnectorBridgeToPipelineElement.PipelineId, out PipelineDiagnostics.PipelineElementDiagnostics source) &&
                                subpipelineNodes.TryGetValue(c.PipelineId, out PipelineDiagnostics.PipelineElementDiagnostics target))
                            {
                                if (this.ShowExporterConnections || !IsBridgeToExporter(target))
                                {
                                    if (this.AddVisualEdge(source.Id, target.Id, i, graph, statsSelector))
                                    {
                                        selectedEdgeUpdated = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!selectedEdgeUpdated && this.model.SelectedEdgeId != -1)
            {
                // hide while in subpipeline
                this.model.SelectedEdgeDetails = string.Empty;
            }

            this.UpdateColoring(graph);
            return graph;
        }
    }
}
