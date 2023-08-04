// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Msagl.Drawing;
    using Microsoft.Msagl.WpfGraphControl;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Brushes = System.Windows.Media.Brushes;
    using Transform = System.Windows.Media.Transform;

    /// <summary>
    /// Interaction logic for PipelineDiagnosticsVisualizationObjectView.xaml.
    /// </summary>
    public partial class PipelineDiagnosticsVisualizationObjectView : VisualizationObjectView, IDisposable
    {
        private readonly GraphViewer graphViewer = new () { LayoutEditingEnabled = false };
        private Dictionary<int, (Transform, Node)> graphVisualPanZoom = new ();
        private Transform lastRenderTransform = Transform.Identity;
        private Node lastCenteredNode = null;
        private PipelineDiagnosticsVisualizationPresenter presenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsVisualizationObjectView"/> class.
        /// </summary>
        public PipelineDiagnosticsVisualizationObjectView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the pipeline diagnostics visualization object.
        /// </summary>
        public PipelineDiagnosticsVisualizationObject PipelineDiagnosticsVisualizationObject
            => this.VisualizationObject as PipelineDiagnosticsVisualizationObject;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.graphViewer.Dispose();
        }

        /// <summary>
        /// Update view.
        /// </summary>
        /// <param name="forceRelayout">Force re-layout of graph (otherwise, updates labels, colors, etc. in place).</param>
        public void Update(bool forceRelayout)
        {
            this.UpdateViewerGraph(forceRelayout);
            this.infoText.FontSize = this.presenter.InfoTextSize;
            this.infoText.Text = this.presenter.SelectedEdgeDetails;
        }

        /// <inheritdoc/>
        protected override void OnLoaded(object sender, RoutedEventArgs e)
        {
            base.OnLoaded(sender, e);

            this.graphViewer.BindToPanel(this.dockPanel);
            this.graphViewer.MouseDown += this.GraphViewer_MouseDown;
            this.graphViewer.MouseUp += this.GraphViewer_MouseUp;
            this.graphViewer.MouseWheel += this.GraphViewer_MouseWheel;
            this.graphViewer.ObjectUnderMouseCursorChanged += this.GraphViewer_ObjectUnderMouseCursorChanged;
            this.SizeChanged += this.DiagnosticsVisualizationObjectView_SizeChanged;
            this.presenter = new PipelineDiagnosticsVisualizationPresenter(this, this.PipelineDiagnosticsVisualizationObject);
            this.presenter.UpdateGraph(this.PipelineDiagnosticsVisualizationObject.CurrentData, true);
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationObjects.PipelineDiagnosticsVisualizationObject.LayoutDirection))
            {
                // forget and refit all transforms upon changing layout direction
                this.graphVisualPanZoom = new Dictionary<int, (Transform, Node)>();
                this.FitGraphView();
                this.presenter.UpdateSettings(this.PipelineDiagnosticsVisualizationObject);
            }
            else if (e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.CurrentData) && this.PipelineDiagnosticsVisualizationObject.CurrentData != null)
            {
                this.presenter.UpdateGraph(this.PipelineDiagnosticsVisualizationObject.CurrentData, false);
            }
            else if (
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.ConnectorColor) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.EdgeColor) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.EdgeLineThickness) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.HeatmapColor) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.HeatmapStatistics) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.Highlight) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.HighlightOpacity) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.InfoTextSize) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.JoinColor) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.NodeColor) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.ShowDeliveryPolicies) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.ShowLossyDeliveryPoliciesAsDotted) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.ShowEmitterNames) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.ShowExporterConnections) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.ShowReceiverNames) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.SourceNodeColor) ||
                e.PropertyName == nameof(this.PipelineDiagnosticsVisualizationObject.SubpipelineColor))
            {
                this.presenter.UpdateSettings(this.PipelineDiagnosticsVisualizationObject);
            }
        }

        private void GraphViewer_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e)
        {
            Mouse.OverrideCursor = e.NewObject != null ? Cursors.Hand : Cursors.Arrow;

            if (e.NewObject is VEdge edge)
            {
                this.PipelineDiagnosticsVisualizationObject.UpdateEdgeUnderCursor(Convert.ToInt32(edge.Edge.UserData));
            }
            else
            {
                this.PipelineDiagnosticsVisualizationObject.UpdateEdgeUnderCursor(-1);
            }
        }

        private void DiagnosticsVisualizationObjectView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.FitGraphView();
        }

        private void GraphViewer_MouseDown(object sender, MsaglMouseEventArgs e)
        {
            var ctrlHeld = Keyboard.IsKeyDown(Key.LeftCtrl);
            if (!ctrlHeld && e.RightButtonIsPressed)
            {
                this.lastCenteredNode = null; // prevent recentering after pan elsewhere
            }

            var obj = this.graphViewer.ObjectUnderMouseCursor;
            if (obj != null)
            {
                if (obj is VNode vnode)
                {
                    if (e.RightButtonIsPressed)
                    {
                        if (ctrlHeld)
                        {
                            this.lastCenteredNode = vnode.Node;
                            this.CenterOnNode(vnode.Node);
                        }
                    }
                    else if (e.Clicks > 1)
                    {
                        if (vnode.DrawingObject.UserData is PipelineDiagnostics subgraph)
                        {
                            this.presenter.NavInto(subgraph.Id);
                        }
                    }

                    this.presenter.ClearSelectedEdge(); // clear selected edge when clicking a node

                    return;
                }

                Edge edge = null;
                if (obj is VEdge vedge)
                {
                    edge = vedge.Edge;
                }
                else
                {
                    if (obj is VLabel vlabel)
                    {
                        edge = vlabel.DrawingObject.UserData as Edge;
                    }
                }

                if (edge != null && !e.RightButtonIsPressed)
                {
                    this.presenter.UpdateReceiverDiagnostics(edge);
                }
            }
        }

        private void GraphViewer_MouseUp(object sender, MsaglMouseEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                e.Handled = true; // no context menu
            }
        }

        private void GraphViewer_MouseWheel(object sender, MsaglMouseEventArgs e)
        {
            this.lastCenteredNode = null; // prevent recentering after zoom elsewhere
        }

        private void FitGraphView()
        {
            var graph = this.graphViewer.Graph;
            if (graph != null)
            {
                this.graphViewer.SetInitialTransform();
                this.lastRenderTransform = this.graphViewer.GraphCanvas.RenderTransform;
                this.graphVisualPanZoom.Remove((int)graph.UserData); // no longer preserving transform
            }
        }

        private void CenterOnNode(Node node)
        {
            var centerNode = this.graphViewer.Graph.FindNode(node.Id); // find (possibly reconstructed) node
            if (centerNode != null)
            {
                this.graphViewer.NodeToCenter(centerNode);
            }
        }

        private void UpdateBreadcrumbNav()
        {
            // walk view stack bottom-to-top
            var graph = this.presenter.DiagnosticsGraph;
            this.breadcrumbNav.Children.Clear();
            foreach (var view in this.presenter.Breadcrumbs)
            {
                var subgraph = graph.SubpipelineDiagnostics.FirstOrDefault(s => s.Id == view);
                if (subgraph == null)
                {
                    this.graphViewer.Graph = null;
                    return;
                }

                graph = subgraph;

                var separator = new TextBlock()
                {
                    Margin = new Thickness(6, 6, 6, 6),
                    Background = Brushes.Transparent,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 18,
                    Text = "/",
                };
                this.breadcrumbNav.Children.Add(separator);

                var link = new Button()
                {
                    Margin = new Thickness(6, 6, 6, 6),
                    Background = Brushes.Transparent,
                    Foreground = Brushes.DodgerBlue,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 18,
                    Content = $"{graph.Name} ({view})",
                    Cursor = Cursors.Hand,
                    Tag = view,
                };
                link.Click += this.BreadcrumbNav_Click;
                this.breadcrumbNav.Children.Add(link);
            }
        }

        private void UpdateViewerGraph(bool forceRelayout)
        {
            this.statusText.Content = this.presenter.DiagnosticsGraph != null ? $"{this.presenter.DiagnosticsGraph.Name} (nodes={this.presenter.DiagnosticsGraph.PipelineElements.Length})" : string.Empty;
            this.UpdateBreadcrumbNav();
            var visual = this.presenter.VisualGraph;
            if (visual != null)
            {
                if (this.graphViewer.Graph == null)
                {
                    // first time
                    this.graphViewer.Graph = visual;
                    this.FitGraphView();
                }
                else
                {
                    // preserve render transform
                    if (this.graphViewer.GraphCanvas.RenderTransform != this.lastRenderTransform)
                    {
                        // preserve if user has zoomed/panned
                        this.graphVisualPanZoom[(int)this.graphViewer.Graph.UserData] = (this.graphViewer.GraphCanvas.RenderTransform, this.lastCenteredNode);
                    }

                    if (forceRelayout)
                    {
                        this.graphViewer.Graph = visual;
                    }
                    else
                    {
                        this.graphViewer.UpdateGraphInPlace(visual);
                    }

                    if (this.graphVisualPanZoom.TryGetValue((int)visual.UserData, out (Transform, Node) transformCenterNode))
                    {
                        this.graphViewer.GraphCanvas.RenderTransform = transformCenterNode.Item1;
                        if (transformCenterNode.Item2 != null)
                        {
                            this.CenterOnNode(transformCenterNode.Item2);
                        }
                    }
                    else
                    {
                        this.FitGraphView();
                    }
                }
            }
            else
            {
                this.graphViewer.Graph = null;
                this.graphViewer.ClearGraphViewer();
            }
        }

        private void BreadcrumbNav_Click(object sender, RoutedEventArgs e)
        {
            this.presenter.NavBackTo((int)((Button)e.Source).Tag);
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            this.presenter.NavHome();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.presenter.NavBack();
        }

        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            this.FitGraphView();
        }

        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void InfoText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.presenter.ClearSelectedEdge(); // hide info panel (clear selected edge) upon clicking
            }
        }
    }
}
