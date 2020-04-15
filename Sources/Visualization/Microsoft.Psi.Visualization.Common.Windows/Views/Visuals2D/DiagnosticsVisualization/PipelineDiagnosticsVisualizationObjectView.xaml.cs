// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
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
    /// Interaction logic for DiagnosticsVisualizationObjectView.xaml.
    /// </summary>
    public partial class PipelineDiagnosticsVisualizationObjectView : UserControl
    {
        private GraphViewer graphViewer = new GraphViewer() { LayoutEditingEnabled = false };
        private Dictionary<int, (Transform, Node)> graphVisualPanZoom = new Dictionary<int, (Transform, Node)>();
        private Transform lastRenderTransform = Transform.Identity;
        private Node lastCenteredNode = null;
        private PipelineDiagnosticsVisualizationPresenter presenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsVisualizationObjectView"/> class.
        /// </summary>
        public PipelineDiagnosticsVisualizationObjectView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.DiagnosticsVisualizationObjectView_DataContextChanged;
            this.Loaded += this.DiagnosticsVisualizationObjectView_Loaded;
            this.Unloaded += this.DiagnosticsVisualizationObjectView_Unloaded;
        }

        /// <summary>
        /// Gets the image visualization object.
        /// </summary>
        public PipelineDiagnosticsVisualizationObject DiagnosticsVisualizationObject { get; private set; }

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

        private void DiagnosticsVisualizationObjectView_Loaded(object sender, RoutedEventArgs e)
        {
            this.graphViewer.BindToPanel(this.dockPanel);
            this.graphViewer.MouseDown += this.GraphViewer_MouseDown;
            this.graphViewer.MouseUp += this.GraphViewer_MouseUp;
            this.graphViewer.MouseWheel += this.GraphViewer_MouseWheel;
            this.graphViewer.ObjectUnderMouseCursorChanged += this.GraphViewer_ObjectUnderMouseCursorChanged;
            this.SizeChanged += this.DiagnosticsVisualizationObjectView_SizeChanged;
            this.presenter = new PipelineDiagnosticsVisualizationPresenter(this, this.DiagnosticsVisualizationObject);
        }

        private void GraphViewer_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e)
        {
            Mouse.OverrideCursor = e.NewObject != null ? Cursors.Hand : Cursors.Arrow;
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
                var vnode = obj as VNode;
                if (vnode != null)
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
                        var subgraph = vnode.DrawingObject.UserData as PipelineDiagnostics;
                        if (subgraph != null)
                        {
                            this.presenter.NavInto(subgraph.Id);
                        }
                    }

                    this.presenter.UpdateSelectedEdge(null); // clear selected edge when clicking a node

                    return;
                }

                var vedge = obj as VEdge;
                Edge edge = null;
                if (vedge != null)
                {
                    edge = vedge.Edge;
                }
                else
                {
                    var vlabel = obj as VLabel;
                    if (vlabel != null)
                    {
                        edge = vlabel.DrawingObject.UserData as Edge;
                    }
                }

                if (edge != null)
                {
                    this.presenter.UpdateSelectedEdge(edge);
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

        private void DiagnosticsVisualizationObjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.DiagnosticsVisualizationObject.PropertyChanged -= this.DiagnosticsVisualizationObject_PropertyChanged;
        }

        private void DiagnosticsVisualizationObjectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.DiagnosticsVisualizationObject = (PipelineDiagnosticsVisualizationObject)this.DataContext;
            this.DiagnosticsVisualizationObject.PropertyChanged += this.DiagnosticsVisualizationObject_PropertyChanged;
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

        private void DiagnosticsVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PipelineDiagnosticsVisualizationObject.LayoutDirection))
            {
                // forget and refit all transforms upon changing layout direction
                this.graphVisualPanZoom = new Dictionary<int, (Transform, Node)>();
                this.FitGraphView();
            }
            else if (e.PropertyName == nameof(this.DiagnosticsVisualizationObject.CurrentValue) && this.DiagnosticsVisualizationObject.CurrentValue != null && this.DiagnosticsVisualizationObject.CurrentValue.Value.Data != null)
            {
                this.presenter.UpdateGraph(this.DiagnosticsVisualizationObject.CurrentValue.Value.Data, false);
            }
            else
            {
                this.presenter.UpdateSettings(this.DiagnosticsVisualizationObject);
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
                this.presenter.UpdateSelectedEdge(null); // hide info panel (clear selected edge) upon clicking
            }
        }
    }
}
