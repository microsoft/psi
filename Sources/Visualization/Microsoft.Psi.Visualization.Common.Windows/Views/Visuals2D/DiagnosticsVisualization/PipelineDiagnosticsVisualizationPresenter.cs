// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Msagl.Drawing;
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
        /// Gets edge color.
        /// </summary>
        public Color HighlightColor { get; private set; }

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
            Func<System.Windows.Media.Color, Color> colorFromMediaColor = (System.Windows.Media.Color color) => new Color(color.R, color.G, color.B);
            this.model.VisualizationObject = visualizationObject;
            this.EdgeColor = colorFromMediaColor(visualizationObject.EdgeColor);
            this.HighlightColor = colorFromMediaColor(visualizationObject.HighlightColor);
            this.NodeColor = colorFromMediaColor(visualizationObject.NodeColor);
            this.SourceNodeColor = colorFromMediaColor(visualizationObject.SourceNodeColor);
            this.SubpipelineColor = colorFromMediaColor(visualizationObject.SubpipelineColor);
            this.ConnectorColor = colorFromMediaColor(visualizationObject.ConnectorColor);
            this.JoinColor = colorFromMediaColor(visualizationObject.JoinColor);
            this.HeatmapColorBase = colorFromMediaColor(visualizationObject.HeatmapColor);
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
        /// Update selected edge.
        /// </summary>
        /// <param name="edge">Selected edge.</param>
        public void UpdateSelectedEdge(Edge edge)
        {
            if (edge == null)
            {
                // clear selected edge (if any)
                this.model.SelectedEdgeId = -1;
                this.view.Update(true);
                return;
            }

            var input = edge.UserData as PipelineDiagnostics.ReceiverDiagnostics;
            if (input != null)
            {
                this.UpdateSelectedEdge(input, this.VisualGraph, true);
            }
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
        /// Navigate back to givin graph.
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
            foreach (var n in graph.Nodes)
            {
                foreach (var e in n.Edges)
                {
                    if (e.UserData != null && ((PipelineDiagnostics.ReceiverDiagnostics)e.UserData).Id == id)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        private static bool IsBridgeToExporter(PipelineDiagnostics.PipelineElementDiagnostics node)
        {
            var bridgeEmitters = node.ConnectorBridgeToPipelineElement.Emitters;
            var typeName = bridgeEmitters.Length == 1 ? bridgeEmitters[0].PipelineElement.TypeName : string.Empty;
            return typeName == "MessageConnector`1" || typeName == "MessageEnvelopeConnector`1";
        }

        private void UpdateSelectedEdge(PipelineDiagnostics.ReceiverDiagnostics input, Graph graph, bool clicked)
        {
            var edge = GetEdgeById(input.Id, graph);
            if (clicked && this.model.SelectedEdgeId == input.Id)
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
            this.model.SelectedEdgeId = input.Id;
            var sb = new StringBuilder();
            sb.Append($"Type: {TypeSpec.Simplify(input.TypeName)}" + Environment.NewLine);
            sb.Append($"Message Size (avg): {input.MessageSize:0}" + Environment.NewLine);
            sb.Append($"Queue Size: {input.QueueSize:0.###}" + Environment.NewLine);
            sb.Append($"Processed Count: {input.ProcessedCount}" + Environment.NewLine);
            sb.Append($"Processed/Time: {input.ProcessedPerTimeSpan:0.###}" + Environment.NewLine);
            sb.Append($"Dropped Count: {input.DroppedCount}" + Environment.NewLine);
            sb.Append($"Dropped/Time: {input.DroppedPerTimeSpan:0.###}" + Environment.NewLine);
            sb.Append($"Latency at Emitter (avg): {input.MessageLatencyAtEmitter:0.###}ms" + Environment.NewLine);
            sb.Append($"Latency at Receiver (avg): {input.MessageLatencyAtReceiver:0.###}ms" + Environment.NewLine);
            sb.Append($"Processing Time (avg): {input.ProcessingTime:0.###}ms" + Environment.NewLine);
            sb.Append($"Delivery Policy: {input.DeliveryPolicyName}" + Environment.NewLine);
            this.model.SelectedEdgeDetails = sb.ToString();
            this.view.Update(clicked);
        }

        private Func<PipelineDiagnostics.ReceiverDiagnostics, double> StatsSelector(bool heatmap)
        {
            switch (this.model.VisualizationObject.HeatmapStatistics)
            {
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.None:
                    return null;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.LatencyAtEmitter:
                    return i => i.MessageLatencyAtEmitter;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.LatencyAtReceiver:
                    return i => i.MessageLatencyAtReceiver;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.Processing:
                    return i => i.ProcessingTime;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.Throughput:
                    return i => i.ProcessedCount;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.QueueSize:
                    return i => i.QueueSize;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.DroppedCount:
                    return i => i.DroppedCount;
                case PipelineDiagnosticsVisualizationObject.HeatmapStats.MessageSize:
                    return i =>
                    {
                        var avg = i.MessageSize;
                        return heatmap && avg > 0 ? Math.Log(avg) : avg;
                    };
                default:
                    throw new ArgumentException($"Unknown visualization selector type.");
            }
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
            switch (this.model.VisualizationObject.Highlight)
            {
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.None:
                    return false;
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.UnlimitedDeliveryPolicy:
                    return receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.Unlimited));
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.LatestMessageDeliveryPolicy:
                    return receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.LatestMessage));
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.ThrottleDeliveryPolicy:
                    return receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.Throttle));
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.SynchronousOrThrottleDeliveryPolicy:
                    return receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.SynchronousOrThrottle));
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.LatencyConstrainedDeliveryPolicy:
                    return receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.LatencyConstrained));
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.QueueSizeConstrainedDeliveryPolicy:
                    return receiverDiagnostics.DeliveryPolicyName.StartsWith(nameof(DeliveryPolicy.QueueSizeConstrained));
                case PipelineDiagnosticsVisualizationObject.HighlightCondition.ThrottledReceivers:
                    return receiverDiagnostics.Throttled;
                default:
                    throw new ArgumentException($"Unknown hilight condition: {this.model.VisualizationObject.Highlight}");
            }
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

        private void HeatmapStats(Graph graph, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector, bool perNode)
        {
            if (graph.Edges.Count() == 0)
            {
                return;
            }

            var edgeStats = graph.Edges.Where(e => e.UserData != null).Select(e => (e, statsSelector((PipelineDiagnostics.ReceiverDiagnostics)e.UserData)));
            var max = edgeStats.Select(x => x.Item2).Max();

            if (perNode)
            {
                // max edge per node
                foreach (var node in graph.Nodes)
                {
                    var inputs = node.InEdges;
                    if (inputs.Count() > 0)
                    {
                        var maxStats = node.InEdges.Where(e => e.UserData != null).Select(e => statsSelector((PipelineDiagnostics.ReceiverDiagnostics)e.UserData)).Max();
                        var color = this.HeatmapColor(max > 0 ? maxStats / max : 0, this.NodeColor);
                        node.Attr.Color = color;
                        node.Attr.FillColor = color;
                        node.Label.FontColor = this.LabelColor(color);
                    }
                }
            }
            else
            {
                // per edge
                foreach (var (edge, stat) in edgeStats)
                {
                    edge.Attr.Color = this.HeatmapColor(max > 0 ? stat / max : 0, this.EdgeColor);
                }
            }
        }

        private void VisualizeEdgeColoring(Graph graph)
        {
            var selector = this.StatsSelector(true);
            if (selector != null)
            {
                // visualize heatmap
                var perNode = this.model.VisualizationObject.HeatmapStatistics == PipelineDiagnosticsVisualizationObject.HeatmapStats.Processing;
                this.HeatmapStats(graph, selector, perNode);
            }

            // overlay highlights
            if (this.model.VisualizationObject.Highlight != PipelineDiagnosticsVisualizationObject.HighlightCondition.None)
            {
                foreach (var edge in graph.Edges)
                {
                    if (edge.UserData != null && this.HilightEdge((PipelineDiagnostics.ReceiverDiagnostics)edge.UserData))
                    {
                        edge.Attr.Color = this.HighlightColor;
                    }
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
            vis.LabelText = node.Kind == PipelineElementKind.Subpipeline ? $"{node.Name}{stopped}|{typ}" : typ;
            vis.Label.FontColor = this.LabelColor(fillColor);
            vis.Attr.Color = fillColor;
            vis.Attr.FillColor = fillColor;
            if (vis.LabelText == "Join")
            {
                this.SetJoinVisualAttributes(vis, node.Name);
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

        private Edge BuildVisualEdge(Node source, Node target, PipelineDiagnostics.ReceiverDiagnostics input, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            var edge = new Edge(source, target, ConnectionToGraph.Connected);
            edge.UserData = input;
            edge.Attr.Color = this.EdgeColor;
            edge.Attr.LineWidth = this.model.VisualizationObject.EdgeLineThickness;
            var stats = statsSelector != null ? $" ({statsSelector(input):0.#})" : string.Empty;
            edge.LabelText = this.BuildVisualEdgeLabelText(input.Source.Name, input.ReceiverName, stats, input.DeliveryPolicyName);
            edge.Label.FontColor = this.LabelColorLight;
            edge.Label.UserData = edge;
            return edge;
        }

        private bool AddVisualEdge(Node source, Node target, PipelineDiagnostics.ReceiverDiagnostics input, Graph graph, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            if (source != null && target != null)
            {
                var edge = this.BuildVisualEdge(source, target, input, statsSelector);
                graph.AddPrecalculatedEdge(edge);
                if (input.Id == this.model.SelectedEdgeId)
                {
                    this.UpdateSelectedEdge(input, graph, false);
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

        private void SetJoinVisualAttributes(Node vis, string label)
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
                            var edge = new Edge(connector, targetNode, ConnectionToGraph.Connected);
                            edge.Attr.Color = this.EdgeColor;
                            edge.Attr.LineWidth = this.model.VisualizationObject.EdgeLineThickness;
                            edge.Attr.AddStyle(Style.Dotted);
                            edge.LabelText = this.BuildVisualEdgeLabelText(n.Name, bridgedPipeline.Name, string.Empty, string.Empty);
                            edge.Label.FontColor = this.LabelColorLight;
                            graph.AddPrecalculatedEdge(edge);
                            break;
                        }

                        // walk up ancestor chain until we're at a direct child subpipeline
                        targetPipeline = targetPipeline.ParentPipelineDiagnostics;
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
                                if (this.AddVisualEdge(source.Id, target.Id, i, graph, statsSelector))
                                {
                                    selectedEdgeUpdated = true;
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

            this.VisualizeEdgeColoring(graph);
            return graph;
        }
    }
}
