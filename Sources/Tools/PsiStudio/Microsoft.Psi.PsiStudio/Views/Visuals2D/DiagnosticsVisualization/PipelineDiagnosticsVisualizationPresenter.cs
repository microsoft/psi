// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Msagl.Drawing;
    using Microsoft.Psi.Diagnostics;

    /// <summary>
    /// Interaction logic for DiagnosticsVisualizationObjectView.xaml.
    /// </summary>
    public partial class PipelineDiagnosticsVisualizationPresenter
    {
        private readonly PipelineDiagnosticsVisualizationModel model;
        private readonly PipelineDiagnosticsVisualizationObjectView view;

        private Regex prefix = new Regex(@"^\d+\.", RegexOptions.Compiled);
        private Regex suffix = new Regex(@"`\d$", RegexOptions.Compiled);

        private int selectedEdgeId = -1;
        private Edge selectedEdge = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsVisualizationPresenter"/> class.
        /// </summary>
        /// <param name="view">Diagnostics view.</param>
        /// <param name="config">Visualization configuration.</param>
        public PipelineDiagnosticsVisualizationPresenter(PipelineDiagnosticsVisualizationObjectView view, Config.DiagnosticsVisualizationObjectConfiguration config)
        {
            this.model = new PipelineDiagnosticsVisualizationModel();
            this.view = view;
            this.UpdateConfig(config);
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
        public string SelectedEdgeDetails { get; private set; }

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
        /// Gets connector color.
        /// </summary>
        public Color ConnectorColor { get; private set; }

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
        /// <param name="config">Diagnostics configuration.</param>
        public void UpdateConfig(Config.DiagnosticsVisualizationObjectConfiguration config)
        {
            // convert colors to MSAGL graph colors
            Func<System.Windows.Media.Color, Color> colorFromMediaColor = (System.Windows.Media.Color color) => new Color(color.R, color.G, color.B);
            this.model.Config = config;
            this.EdgeColor = colorFromMediaColor(config.EdgeColor);
            this.NodeColor = colorFromMediaColor(config.NodeColor);
            this.SourceNodeColor = colorFromMediaColor(config.SourceNodeColor);
            this.SubpipelineColor = colorFromMediaColor(config.SubpipelineColor);
            this.ConnectorColor = colorFromMediaColor(config.ConnectorColor);
            this.HeatmapColorBase = colorFromMediaColor(config.HeatmapColor);
            this.InfoTextSize = config.InfoTextSize;
            this.LabelColorLight = Color.White;
            this.LabelColorDark = Color.Black;
            if (this.model.Graph != null)
            {
                this.VisualGraph = this.BuildVisualGraph(this.model.Graph);
                this.view.Update();
            }
        }

        /// <summary>
        /// Update diagnostics graph.
        /// </summary>
        /// <param name="graph">Current diagnostics graph.</param>
        public void UpdateGraph(PipelineDiagnostics graph)
        {
            this.model.Graph = graph;
            foreach (var view in this.Breadcrumbs)
            {
                if (!graph.Subpipelines.TryGetValue(view, out PipelineDiagnostics subgraph))
                {
                    graph = null;
                    break;
                }

                graph = subgraph;
            }

            this.VisualGraph = graph != null ? this.BuildVisualGraph(graph) : null;
            this.view.Update();
        }

        /// <summary>
        /// Update selected edge.
        /// </summary>
        /// <param name="edge">Selected edge.</param>
        public void UpdateSelectedEdge(Edge edge)
        {
            var input = edge.UserData as PipelineDiagnostics.ReceiverDiagnostics;
            if (input != null)
            {
                this.UpdateSelectedEdge(input, edge, true);
            }
        }

        /// <summary>
        /// Navigate into subgraph.
        /// </summary>
        /// <param name="subgraphId">Subgraph ID into which to navigate.</param>
        public void NavInto(int subgraphId)
        {
            this.model.NavStack.Push(subgraphId);
            this.UpdateGraph(this.model.Graph);
        }

        /// <summary>
        /// Navigate back one graph.
        /// </summary>
        public void NavBack()
        {
            if (this.model.NavStack.Count > 0)
            {
                this.model.NavStack.Pop();
                this.UpdateGraph(this.model.Graph);
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

            this.UpdateGraph(this.model.Graph);
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

                this.UpdateGraph(this.model.Graph);
            }
        }

        private void UpdateSelectedEdge(PipelineDiagnostics.ReceiverDiagnostics input, Edge edge, bool clicked)
        {
            if (clicked && this.selectedEdgeId == input.Id)
            {
                // toggle unselected
                edge.Attr.LineWidth = this.model.Config.EdgeLineThickness; // unselect current
                this.SelectedEdgeDetails = string.Empty;
                this.selectedEdge = null;
                this.selectedEdgeId = -1;
                this.view.Update();
                return;
            }

            // new edge selected
            if (this.selectedEdge != null)
            {
                this.selectedEdge.Attr.LineWidth = this.model.Config.EdgeLineThickness; // unselect previous
            }

            edge.Attr.LineWidth = this.model.Config.EdgeLineThickness * 2; // select current
            this.selectedEdge = edge;
            this.selectedEdgeId = input.Id;
            var sb = new StringBuilder();
            sb.Append($"Type: {input.Type}" + Environment.NewLine);
            sb.Append($"Message Size (avg): {input.MessageSizeHistory.AverageSize():0}" + Environment.NewLine);
            sb.Append($"Queue Size: {input.QueueSize}" + Environment.NewLine);
            sb.Append($"Processed Count: {input.ProcessedCount}" + Environment.NewLine);
            sb.Append($"Dropped Count: {input.DroppedCount}" + Environment.NewLine);
            sb.Append($"Latency at Emitter (avg): {input.MessageLatencyAtEmitterHistory.AverageTime().TotalMilliseconds:0.###}ms" + Environment.NewLine);
            sb.Append($"Latency at Receiver (avg): {input.MessageLatencyAtReceiverHistory.AverageTime().TotalMilliseconds:0.###}ms" + Environment.NewLine);
            sb.Append($"Processing Time (avg): {input.ProcessingTimeHistory.AverageTime().TotalMilliseconds:0.###}ms" + Environment.NewLine);
            this.SelectedEdgeDetails = sb.ToString();
            this.view.Update();
        }

        private Func<PipelineDiagnostics.ReceiverDiagnostics, double> StatsSelector(bool heatmap)
        {
            switch (this.model.Config.HeatmapStatistics)
            {
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.None:
                    return null;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.LatencyAtEmitter:
                    return i => i.MessageLatencyAtEmitterHistory.AverageTime().TotalMilliseconds;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.LatencyAtReceiver:
                    return i => i.MessageLatencyAtReceiverHistory.AverageTime().TotalMilliseconds;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.Processing:
                    return i => i.ProcessingTimeHistory.AverageTime().TotalMilliseconds;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.Throughput:
                    return i => i.ProcessedCount;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.QueueSize:
                    return i => i.QueueSize;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.DroppedCount:
                    return i => i.DroppedCount;
                case Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.MessageSize:
                    return i =>
                    {
                        var avg = i.MessageSizeHistory.AverageSize();
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
            var brightness = Math.Sqrt(0.299 * r * r + 0.587 * g * g + 0.114 * b * b);
            return brightness < 0.55 ? this.LabelColorLight : this.LabelColorDark;
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

            var edgeStats = graph.Edges.Select(e => (e, statsSelector((PipelineDiagnostics.ReceiverDiagnostics)e.UserData)));
            var max = edgeStats.Select(x => x.Item2).Max();

            if (perNode)
            {
                // max edge per node
                foreach (var node in graph.Nodes)
                {
                    var inputs = node.InEdges;
                    if (inputs.Count() > 0)
                    {
                        var maxStats = node.InEdges.Select(e => statsSelector((PipelineDiagnostics.ReceiverDiagnostics)e.UserData)).Max();
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

        private void VisualizeHeatmap(Graph graph)
        {
            var selector = this.StatsSelector(true);
            if (selector != null)
            {
                var perNode = this.model.Config.HeatmapStatistics == Config.DiagnosticsVisualizationObjectConfiguration.HeatmapStats.Processing;
                this.HeatmapStats(graph, selector, perNode);
            }
        }

        private bool IsConnectorBridge(PipelineDiagnostics.PipelineElementDiagnostics node)
        {
            return node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Connector && (node.Receivers.Count == 0 || node.Emitters.Count == 0);
        }

        private Node BuildVisualNode(PipelineDiagnostics.PipelineElementDiagnostics node)
        {
            var vis = new Node($"n{node.Id}");
            var fillColor = node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Source ? this.SourceNodeColor : node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Subpipeline ? this.SubpipelineColor : this.NodeColor;
            vis.LabelText = $"{this.prefix.Replace(this.suffix.Replace(node.Name, string.Empty), string.Empty)}";
            vis.Label.FontColor = this.LabelColor(fillColor);
            vis.Attr.Color = fillColor;
            vis.Attr.FillColor = fillColor;
            return vis;
        }

        private Edge BuildVisualEdge(Node source, Node target, PipelineDiagnostics.ReceiverDiagnostics input, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            var edge = new Edge(source, target, ConnectionToGraph.Connected);
            edge.UserData = input;
            edge.Attr.Color = this.EdgeColor;
            edge.Attr.LineWidth = this.model.Config.EdgeLineThickness;
            var stats = statsSelector != null ? $" ({statsSelector(input):0.#})" : string.Empty;
            edge.LabelText = $"{input.Name}{stats}";
            edge.Label.FontColor = this.LabelColorLight;
            edge.Label.UserData = edge;
            return edge;
        }

        private bool AddVisualEdge(int sourceId, int targetId, PipelineDiagnostics.ReceiverDiagnostics input, Graph graph, Func<PipelineDiagnostics.ReceiverDiagnostics, double> statsSelector)
        {
            var source = graph.FindNode($"n{sourceId}");
            var target = graph.FindNode($"n{targetId}");
            if (source != null && target != null)
            {
                var edge = this.BuildVisualEdge(source, target, input, statsSelector);
                graph.AddPrecalculatedEdge(edge);
                if (input.Id == this.selectedEdgeId)
                {
                    this.UpdateSelectedEdge(input, edge, false);
                    return true;
                }
            }

            return false;
        }

        private Graph BuildVisualGraph(PipelineDiagnostics diagnostics)
        {
            var graph = new Graph($"{diagnostics.Name} (running={diagnostics.IsPipelineRunning})", $"g{diagnostics.Id}");
            switch (this.model.Config.LayoutDirection)
            {
                case Config.DiagnosticsVisualizationObjectConfiguration.GraphLayoutDirection.LeftToRight:
                    graph.Attr.LayerDirection = LayerDirection.LR;
                    break;
                case Config.DiagnosticsVisualizationObjectConfiguration.GraphLayoutDirection.TopToBottom:
                    graph.Attr.LayerDirection = LayerDirection.TB;
                    break;
                case Config.DiagnosticsVisualizationObjectConfiguration.GraphLayoutDirection.RightToLeft:
                    graph.Attr.LayerDirection = LayerDirection.RL;
                    break;
                case Config.DiagnosticsVisualizationObjectConfiguration.GraphLayoutDirection.BottomToTop:
                    graph.Attr.LayerDirection = LayerDirection.BT;
                    break;
            }

            graph.UserData = diagnostics.Id;
            graph.Attr.BackgroundColor = Color.Transparent;
            var subpipelineNodes = new Dictionary<int, PipelineDiagnostics.PipelineElementDiagnostics>();
            var connectorsWithinSubpipelines = new Dictionary<int, PipelineDiagnostics.PipelineElementDiagnostics>();
            var statsSelector = this.StatsSelector(false);

            // add nodes
            var nodes = diagnostics.PipelineElements.Select(n => n.Value).Where(n => !this.IsConnectorBridge(n));
            foreach (var node in nodes)
            {
                var vis = this.BuildVisualNode(node);
                if (node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Subpipeline)
                {
                    vis.UserData = node.RepresentsSubpipeline;
                    subpipelineNodes.Add(node.RepresentsSubpipeline.Id, node);
                    foreach (var n in node.RepresentsSubpipeline.PipelineElements.Values.Where(n => n.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Connector))
                    {
                        connectorsWithinSubpipelines.Add(n.Id, n);
                    }
                }
                else if (node.Kind == PipelineDiagnostics.PipelineElementDiagnostics.PipelineElementKind.Connector)
                {
                    vis.Attr.Color = this.ConnectorColor;
                    vis.Attr.FillColor = this.ConnectorColor;
                    vis.Attr.Shape = Shape.Circle;
                    vis.LabelText = string.Empty;
                }

                graph.AddNode(vis);
            }

            // add edges
            var selectedEdgeUpdated = false;
            foreach (var n in diagnostics.PipelineElements)
            {
                foreach (var i in n.Value.Receivers)
                {
                    if (i.Value.Source != null)
                    {
                        if (this.AddVisualEdge(i.Value.Source.PipelineElement.Id, n.Value.Id, i.Value, graph, statsSelector))
                        {
                            selectedEdgeUpdated = true;
                        }
                    }
                }
            }

            // add connectors
            foreach (var n in diagnostics.PipelineElements.Where(n => this.IsConnectorBridge(n.Value)))
            {
                // connector bridging to subpipeline?
                if (subpipelineNodes.TryGetValue(n.Value.ConnectorBridgeToPipelineElement.ParentPipeline.Id, out PipelineDiagnostics.PipelineElementDiagnostics subNode))
                {
                    // edges from connector source directly to bridge target (subpipeline)
                    var sub = graph.FindNode($"n{subNode.Id}");
                    if (sub != null)
                    {
                        foreach (var i in n.Value.Receivers)
                        {
                            if (i.Value.Source != null)
                            {
                                var source = graph.FindNode($"n{i.Value.Source.PipelineElement.Id}");
                                if (source != null)
                                {
                                    graph.AddPrecalculatedEdge(this.BuildVisualEdge(source, sub, i.Value, statsSelector));
                                }
                            }
                        }

                        // edges from connector bridge source (subpipeline) to connector targets
                        foreach (var o in n.Value.Emitters)
                        {
                            foreach (var t in o.Value.Targets)
                            {
                                var target = graph.FindNode($"n{t.Value.PipelineElement.Id}");
                                if (target != null)
                                {
                                    graph.AddPrecalculatedEdge(this.BuildVisualEdge(sub, target, t.Value, statsSelector));
                                }
                            }
                        }
                    }
                }
                else
                {
                    // connector bridging to parent graph
                    var connector = new Node($"n{n.Value.Id}");
                    connector.Attr.Color = this.ConnectorColor;
                    connector.Attr.FillColor = this.ConnectorColor;
                    connector.Attr.Shape = Shape.Circle;
                    connector.LabelText = string.Empty;
                    graph.AddNode(connector);

                    // edges from connector to target node
                    foreach (var o in n.Value.Emitters)
                    {
                        foreach (var t in o.Value.Targets)
                        {
                            var target = graph.FindNode($"n{t.Value.PipelineElement.Id}");
                            if (target != null)
                            {
                                graph.AddPrecalculatedEdge(this.BuildVisualEdge(connector, target, t.Value, statsSelector));
                            }
                        }
                    }

                    // edges to connector from source node
                    foreach (var i in n.Value.Receivers)
                    {
                        if (i.Value.Source != null)
                        {
                            var source = graph.FindNode($"n{i.Value.Source.PipelineElement.Id}");
                            if (source != null)
                            {
                                graph.AddPrecalculatedEdge(this.BuildVisualEdge(source, connector, i.Value, statsSelector));
                            }
                        }
                    }
                }
            }

            // add direct connections from one subpipeline (connector) to another
            foreach (var c in connectorsWithinSubpipelines.Values)
            {
                if (c.ConnectorBridgeToPipelineElement != null)
                {
                    if (c.ConnectorBridgeToPipelineElement.ParentPipeline == diagnostics && c.ConnectorBridgeToPipelineElement.Receivers.Count == 1)
                    {
                        var i = c.ConnectorBridgeToPipelineElement.Receivers.Values.First();
                        if (i.Source != null && i.Source.PipelineElement.ParentPipeline == diagnostics && i.Source.PipelineElement.ConnectorBridgeToPipelineElement != null)
                        {
                            if (subpipelineNodes.TryGetValue(i.Source.PipelineElement.ConnectorBridgeToPipelineElement.ParentPipeline.Id, out PipelineDiagnostics.PipelineElementDiagnostics source) &&
                                subpipelineNodes.TryGetValue(c.ParentPipeline.Id, out PipelineDiagnostics.PipelineElementDiagnostics target))
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

            if (!selectedEdgeUpdated && this.selectedEdgeId != -1)
            {
                // hide while in subpipeline
                this.SelectedEdgeDetails = string.Empty;
            }

            this.VisualizeHeatmap(graph);
            return graph;
        }
    }
}
