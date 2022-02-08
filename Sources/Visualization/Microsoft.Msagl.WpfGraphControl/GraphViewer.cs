// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using Microsoft.Msagl.Core;
    using Microsoft.Msagl.Core.Geometry;
    using Microsoft.Msagl.Core.Geometry.Curves;
    using Microsoft.Msagl.Core.Layout;
    using Microsoft.Msagl.Drawing;
    using Microsoft.Msagl.Layout.LargeGraphLayout;
    using Microsoft.Msagl.Miscellaneous;
    using Microsoft.Msagl.Miscellaneous.LayoutEditing;
    using Microsoft.Msagl.Prototype.LayoutEditing;
    using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
    using Edge = Microsoft.Msagl.Core.Layout.Edge;
    using Ellipse = System.Windows.Shapes.Ellipse;
    using ILabeledObject = Microsoft.Msagl.Drawing.ILabeledObject;
    using Label = Microsoft.Msagl.Drawing.Label;
    using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
    using ModifierKeys = Microsoft.Msagl.Drawing.ModifierKeys;
    using Node = Microsoft.Msagl.Core.Layout.Node;
    using Point = Microsoft.Msagl.Core.Geometry.Point;
    using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
    using Size = System.Windows.Size;
    using WpfPoint = System.Windows.Point;

    /// <summary>
    /// Graph viewer.
    /// </summary>
    public class GraphViewer : IViewer, IDisposable
    {
        private const double DesiredPathThicknessInInches = 0.008;

        private static double dpiX;
        private static int dpiY;

        private readonly Canvas graphCanvas = new Canvas();
        private readonly Dictionary<DrawingObject, FrameworkElement> drawingObjectsToFrameworkElements = new Dictionary<DrawingObject, FrameworkElement>();
        private readonly LayoutEditor layoutEditor;
        private readonly Dictionary<DrawingObject, IViewerObject> drawingObjectsToIViewerObjects = new Dictionary<DrawingObject, IViewerObject>();
        private readonly object processGraphLock = new object();
        private readonly Dictionary<DrawingObject, Func<DrawingObject, FrameworkElement>> registeredCreators = new Dictionary<DrawingObject, Func<DrawingObject, FrameworkElement>>();
        private readonly ClickCounter clickCounter;

        private Path targetArrowheadPathForRubberEdge;
        private Path rubberEdgePath;
        private Path rubberLinePath;
        private Point sourcePortLocationForEdgeRouting;
        private Graph drawingGraph;
        private GeometryGraph geometryGraphUnderLayout;
        private bool needToCalculateLayout = true;
        private object objectUnderMouseCursor;
        private FrameworkElement rectToFillGraphBackground;
        private System.Windows.Shapes.Rectangle rectToFillCanvas;
        private CancelToken cancelToken = new CancelToken();
        private BackgroundWorker backgroundWorker;
        private Point mouseDownPositionInGraph;
        private bool mouseDownPositionInGraphInitialized;
        private Ellipse sourcePortCircle;
        private WpfPoint objectUnderMouseDetectionLocation;
        private TextBlock textBoxForApproxNodeBoundaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewer"/> class.
        /// </summary>
        public GraphViewer()
        {
            this.layoutEditor = new LayoutEditor(this);

            this.graphCanvas.SizeChanged += this.GraphCanvasSizeChanged;
            this.graphCanvas.MouseRightButtonDown += this.GraphCanvasMouseRightButtonDown;
            this.graphCanvas.MouseLeftButtonDown += this.GraphCanvasLeftMouseDown;
            this.graphCanvas.MouseMove += this.GraphCanvasMouseMove;

            this.graphCanvas.MouseRightButtonUp += this.GraphCanvasMouseRightButtonUp;
            this.graphCanvas.MouseWheel += this.GraphCanvasMouseWheel;
            this.graphCanvas.MouseLeftButtonUp += this.GraphCanvasLeftMouseUp;
            this.ViewChangeEvent += this.AdjustBtrectRenderTransform;

            this.LayoutEditingEnabled = true;
            this.clickCounter = new ClickCounter(() => Mouse.GetPosition((IInputElement)this.graphCanvas.Parent));
            this.clickCounter.Elapsed += this.ClickCounterElapsed;
            this.RunLayoutAsync = false;
        }

        /// <summary>
        /// Layout started event.
        /// </summary>
        public event EventHandler LayoutStarted;

        /// <summary>
        /// Layout completed event.
        /// </summary>
        public event EventHandler LayoutComplete;

        /// <summary>
        /// Graph changed event.
        /// </summary>
        public event EventHandler GraphChanged;

        /// <summary>
        /// View changed event.
        /// </summary>
        public event EventHandler<EventArgs> ViewChangeEvent;

        /// <summary>
        /// Mouse down event.
        /// </summary>
        public event EventHandler<MsaglMouseEventArgs> MouseDown;

        /// <summary>
        /// Mouse move event.
        /// </summary>
        public event EventHandler<MsaglMouseEventArgs> MouseMove;

        /// <summary>
        /// Mouse up event.
        /// </summary>
        public event EventHandler<MsaglMouseEventArgs> MouseUp;

        /// <summary>
        /// Mouse wheel event.
        /// </summary>
        public event EventHandler<MsaglMouseEventArgs> MouseWheel;

        /// <summary>
        /// Object (node/edge/label) under mouse cursor changed.
        /// </summary>
        public event EventHandler<ObjectUnderMouseCursorChangedEventArgs> ObjectUnderMouseCursorChanged;

        /// <summary>
        /// Gets X DPI.
        /// </summary>
        public static double DpiXStatic
        {
            get
            {
                if (dpiX == 0)
                {
                    GetDpi();
                }

                return dpiX;
            }
        }

        /// <summary>
        /// Gets Y DPI.
        /// </summary>
        public static double DpiYStatic
        {
            get
            {
                if (dpiX == 0)
                {
                    GetDpi();
                }

                return dpiY;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to layout in a task.
        /// </summary>
        public bool RunLayoutAsync { get; set; }

        /// <summary>
        /// Gets modifier keys being held.
        /// </summary>
        public ModifierKeys ModifierKeys
        {
            get
            {
                switch (Keyboard.Modifiers)
                {
                    case System.Windows.Input.ModifierKeys.Alt:
                        return ModifierKeys.Alt;
                    case System.Windows.Input.ModifierKeys.Control:
                        return ModifierKeys.Control;
                    case System.Windows.Input.ModifierKeys.None:
                        return ModifierKeys.None;
                    case System.Windows.Input.ModifierKeys.Shift:
                        return ModifierKeys.Shift;
                    case System.Windows.Input.ModifierKeys.Windows:
                        return ModifierKeys.Windows;
                    default:
                        return ModifierKeys.None;
                }
            }
        }

        /// <summary>
        /// Gets graph entities.
        /// </summary>
        public IEnumerable<IViewerObject> Entities
        {
            get
            {
                foreach (var viewerObject in this.drawingObjectsToIViewerObjects.Values)
                {
                    yield return viewerObject;

                    if (viewerObject is VEdge edge && edge.VLabel != null)
                    {
                        yield return edge.VLabel;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the canvas to draw the graph.
        /// </summary>
        public Canvas GraphCanvas
        {
            get { return this.graphCanvas; }
        }

        /// <summary>
        /// Gets layout editor.
        /// </summary>
        public LayoutEditor LayoutEditor
        {
            get { return this.layoutEditor; }
        }

        /// <summary>
        /// Gets zoom factor.
        /// </summary>
        public double ZoomFactor
        {
            get { return this.CurrentScale / this.FitFactor; }
        }

        /// <summary>
        /// Gets current scale.
        /// </summary>
        public double CurrentScale
        {
            get { return ((MatrixTransform)this.graphCanvas.RenderTransform).Matrix.M11; }
        }

        /// <summary>
        /// Gets X DPI.
        /// </summary>
        public double DpiX
        {
            get { return DpiXStatic; }
        }

        /// <summary>
        /// Gets Y DPI.
        /// </summary>
        public double DpiY
        {
            get { return DpiYStatic; }
        }

        /// <summary>
        /// Gets or sets plane transformation.
        /// </summary>
        public PlaneTransformation Transform
        {
            get
            {
                var mt = this.graphCanvas.RenderTransform as MatrixTransform;
                if (mt == null)
                {
                    return PlaneTransformation.UnitTransformation;
                }

                var m = mt.Matrix;
                return new PlaneTransformation(m.M11, m.M12, m.OffsetX, m.M21, m.M22, m.OffsetY);
            }

            set
            {
                this.SetRenderTransformWithoutRaisingEvents(value);

                if (this.ViewChangeEvent != null)
                {
                    this.ViewChangeEvent(null, null);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether layout needs to be calculated.
        /// </summary>
        public bool NeedToCalculateLayout
        {
            get { return this.needToCalculateLayout; }
            set { this.needToCalculateLayout = value; }
        }

        /// <summary>
        /// Gets or sets the cancel token used to cancel a long running layout.
        /// </summary>
        public CancelToken CancelToken
        {
            get { return this.cancelToken; }
            set { this.cancelToken = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether layout is done, but the overlap is removed for graphs with geometry.
        /// </summary>
        public bool NeedToRemoveOverlapOnly { get; set; }

        /// <summary>
        /// Gets current object under the mouse cursor.
        /// </summary>
        public IViewerObject ObjectUnderMouseCursor
        {
            get
            {
                // this function can bring a stale object
                var location = Mouse.GetPosition(this.graphCanvas);
                if (!(this.objectUnderMouseDetectionLocation == location))
                {
                    this.UpdateWithWpfHitObjectUnderMouseOnLocation(location, this.MyHitTestResultCallbackWithNoCallbacksToTheUser);
                }

                return this.GetIViewerObjectFromObjectUnderCursor(this.objectUnderMouseCursor);
            }

            private set
            {
                var old = this.objectUnderMouseCursor;
                bool callSelectionChanged = this.objectUnderMouseCursor != value && this.ObjectUnderMouseCursorChanged != null;

                this.objectUnderMouseCursor = value;

                if (callSelectionChanged)
                {
                    this.ObjectUnderMouseCursorChanged(
                        this,
                        new ObjectUnderMouseCursorChangedEventArgs(
                            this.GetIViewerObjectFromObjectUnderCursor(old),
                            this.GetIViewerObjectFromObjectUnderCursor(this.objectUnderMouseCursor)));
                }
            }
        }

        /// <summary>
        /// Gets or sets line thickness for editing.
        /// </summary>
        public double LineThicknessForEditing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the layout editing with the mouse is enabled if and only if this field is set to false.
        /// </summary>
        public bool LayoutEditingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether inserting edge.
        /// </summary>
        public bool InsertingEdge { get; set; }

        /// <summary>
        /// Gets underlying polyline circle radius.
        /// </summary>
        public double UnderlyingPolylineCircleRadius
        {
            get { return 0.1 * this.DpiX / this.CurrentScale; }
        }

        /// <summary>
        /// Gets or sets viewer graph.
        /// </summary>
        public IViewerGraph ViewerGraph { get; set; }

        /// <summary>
        /// Gets arrowhead length.
        /// </summary>
        public double ArrowheadLength
        {
            get { return 0.2 * this.DpiX / this.CurrentScale; }
        }

        /// <summary>
        /// Gets or sets graph being viewed.
        /// </summary>
        public Graph Graph
        {
            get { return this.drawingGraph; }

            set
            {
                this.drawingGraph = value;
                if (this.drawingGraph != null)
                {
                    Console.WriteLine("starting processing a graph with {0} nodes and {1} edges", this.drawingGraph.NodeCount, this.drawingGraph.EdgeCount);
                }

                this.ProcessGraph();
            }
        }

        /// <summary>
        /// Gets client viewport mapped to graph.
        /// </summary>
        public Rectangle ClientViewportMappedToGraph
        {
            get
            {
                var t = this.Transform.Inverse;
                var p0 = new Point(0, 0);
                var p1 = new Point(this.graphCanvas.RenderSize.Width, this.graphCanvas.RenderSize.Height);
                return new Rectangle(t * p0, t * p1);
            }
        }

        /// <summary>
        /// Gets current X offset.
        /// </summary>
        protected double CurrentXOffset
        {
            get { return ((MatrixTransform)this.graphCanvas.RenderTransform).Matrix.OffsetX; }
        }

        /// <summary>
        /// Gets current Y offset.
        /// </summary>
        protected double CurrentYOffset
        {
            get { return ((MatrixTransform)this.graphCanvas.RenderTransform).Matrix.OffsetY; }
        }

        /// <summary>
        /// Gets or sets target port circle.
        /// </summary>
        protected Ellipse TargetPortCircle { get; set; }

        /// <summary>
        /// Gets mouse hit tolerance.
        /// </summary>
        protected double MouseHitTolerance
        {
            get { return 0.05 * this.DpiX / this.CurrentScale; }
        }

        private bool UnderLayout
        {
            get { return this.backgroundWorker != null; }
        }

        private double FitFactor
        {
            get
            {
                var geomGraph = this.GeomGraph;
                if (this.drawingGraph == null || geomGraph == null || geomGraph.Width == 0 || geomGraph.Height == 0)
                {
                    return 1;
                }

                var size = this.graphCanvas.RenderSize;

                return this.GetFitFactor(size);
            }
        }

        private GeometryGraph GeomGraph
        {
            get { return this.drawingGraph.GeometryGraph; }
        }

        /// <summary>
        /// Create menu item.
        /// </summary>
        /// <param name="title">Menu title.</param>
        /// <param name="voidVoidDelegate">Menu delegate.</param>
        /// <returns>Menu item.</returns>
        public static object CreateMenuItem(string title, VoidDelegate voidVoidDelegate)
        {
            var menuItem = new MenuItem { Header = title };
            menuItem.Click += (_, __) => voidVoidDelegate();
            return menuItem;
        }

        /// <summary>
        /// Measure string of text.
        /// </summary>
        /// <param name="text">Text to measure.</param>
        /// <param name="family">Font family.</param>
        /// <param name="size">Font size.</param>
        /// <param name="visual">Visual element used to determine pixels per density-independent-pixel.</param>
        /// <returns>Text size.</returns>
        public static Size MeasureText(string text, FontFamily family, double size, Visual visual)
        {
            FormattedText formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(family, default(System.Windows.FontStyle), FontWeights.Regular, FontStretches.Normal),
                size,
                Brushes.Black,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
            return new Size(formattedText.Width, formattedText.Height);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.backgroundWorker?.Dispose();
        }

        /// <summary>
        /// Update graph being viewed in place (assuming structure hasn't changed, otherwise triggers full re-layout).
        /// </summary>
        /// <remarks>
        /// This updates only the labels and color attributes, unless a structural change is detected.
        /// </remarks>
        /// <param name="graph">Graph being viewed.</param>
        public void UpdateGraphInPlace(Graph graph)
        {
            //// Here we attempt to update graph visuals in-place. This includes the labels and color attributes on nodes and edges.
            //// If the graph has changed structurally then we fall back to rebuilding everything by setting the `Graph` property.

            if (this.drawingGraph == null)
            {
                // First update? Build
                this.Graph = graph;
                return;
            }

            if (this.drawingGraph.NodeCount != graph.NodeCount)
            {
                // Nodes changed? Rebuild
                this.Graph = graph;
                return;
            }

            var nodeDict = this.drawingGraph.Nodes.ToDictionary(n => n.Id);
            foreach (var n in graph.Nodes)
            {
                if (!nodeDict.ContainsKey(n.Id))
                {
                    // New node? Rebuild
                    this.Graph = graph;
                    return;
                }

                var m = nodeDict[n.Id];
                if (n.Edges.Count() != m.Edges.Count())
                {
                    // Edges changed? Rebuild
                    this.Graph = graph;
                    return;
                }

                // Get node drawing object and update label TextBlock and Path in-place
                var nodeDrawingObj = this.drawingObjectsToIViewerObjects[m] as VNode;
                var textBlock = nodeDrawingObj.FrameworkElementOfNodeForLabel as TextBlock;
                (var text, var tooltip) = VNode.GetLabelTextAndToolTip(n.LabelText);
                textBlock.Text = text;
                textBlock.ToolTip = tooltip;
                textBlock.Foreground = Common.BrushFromMsaglColor(n.Label.FontColor);
                nodeDrawingObj.BoundaryPath.Stroke = Common.BrushFromMsaglColor(n.Attr.Color);
                nodeDrawingObj.BoundaryPath.Fill = Common.BrushFromMsaglColor(n.Attr.FillColor);

                foreach (var e in n.Edges)
                {
                    var matchingEdges = m.Edges.Where(x => x.Source == e.Source && x.Target == e.Target && (int)x.UserData == (int)e.UserData);

                    if (!matchingEdges.Any())
                    {
                        // New edge? Rebuild
                        this.Graph = graph;
                        return;
                    }
                    else if (matchingEdges.Count() > 1)
                    {
                        throw new Exception("Multiple existing edges match the same incoming edge by UserData.");
                    }

                    // Get edge drawing object and update label TextBlock and Paths in-place
                    var edgeDrawingObj = this.drawingObjectsToIViewerObjects[matchingEdges.First()] as VEdge;
                    var edgeColor = Common.BrushFromMsaglColor(e.Attr.Color);
                    edgeDrawingObj.CurvePath.Stroke = edgeColor;
                    edgeDrawingObj.TargetArrowHeadPath.Stroke = edgeColor;
                    edgeDrawingObj.TargetArrowHeadPath.Fill = edgeColor;
                    edgeDrawingObj.CurvePath.StrokeThickness = e.Attr.LineWidth;
                    var edgeLabelTextBlock = edgeDrawingObj.VLabel.FrameworkElement as TextBlock;
                    edgeLabelTextBlock.Text = e.LabelText;
                    edgeLabelTextBlock.Foreground = edgeColor;
                }
            }
        }

        /// <summary>
        /// On drag end.
        /// </summary>
        /// <param name="changedObjects">Changed object.</param>
        public void OnDragEnd(IEnumerable<IViewerObject> changedObjects)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set popup menus.
        /// </summary>
        /// <param name="menuItems">Menu items.</param>
        public void PopupMenus(params Tuple<string, VoidDelegate>[] menuItems)
        {
            var contextMenu = new ContextMenu();
            foreach (var pair in menuItems)
            {
                contextMenu.Items.Add(CreateMenuItem(pair.Item1, pair.Item2));
            }

            contextMenu.Closed += this.ContextMenuClosed;
            ContextMenuService.SetContextMenu(this.graphCanvas, contextMenu);
        }

        /// <summary>
        /// Create viewer node.
        /// </summary>
        /// <param name="drawingNode">Drawing node.</param>
        /// <returns>Viewer node.</returns>
        public IViewerNode CreateIViewerNode(Msagl.Drawing.Node drawingNode)
        {
            var frameworkElement = this.CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2 * drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2 * drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new Node(bc, drawingNode);
            var node = this.CreateVNode(drawingNode);
            this.layoutEditor.AttachLayoutChangeEvent(node);
            return node;
        }

        /// <summary>
        /// Create and register framework element of drawing node.
        /// </summary>
        /// <param name="node">Drawing node.</param>
        /// <returns>Framework element.</returns>
        public FrameworkElement CreateAndRegisterFrameworkElementOfDrawingNode(Msagl.Drawing.Node node)
        {
            lock (this)
            {
                return this.drawingObjectsToFrameworkElements[node] = this.CreateTextBlockForDrawingObj(node);
            }
        }

        /// <summary>
        /// zooms to the default view.
        /// </summary>
        public void SetInitialTransform()
        {
            if (this.drawingGraph == null || this.GeomGraph == null)
            {
                return;
            }

            var scale = this.FitFactor;
            var graphCenter = this.GeomGraph.BoundingBox.Center;
            var vp = new Rectangle(new Point(0, 0), new Point(this.graphCanvas.RenderSize.Width, this.graphCanvas.RenderSize.Height));
            this.SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, graphCenter, vp);
        }

        /// <summary>
        /// Draw image.
        /// </summary>
        /// <param name="fileName">Image file name.</param>
        /// <returns>Image.</returns>
        public Image DrawImage(string fileName)
        {
            var ltrans = this.graphCanvas.LayoutTransform;
            var rtrans = this.graphCanvas.RenderTransform;
            this.graphCanvas.LayoutTransform = null;
            this.graphCanvas.RenderTransform = null;
            var renderSize = this.graphCanvas.RenderSize;

            double scale = this.FitFactor;
            int w = (int)(this.GeomGraph.Width * scale);
            int h = (int)(this.GeomGraph.Height * scale);

            this.SetTransformOnViewportWithoutRaisingViewChangeEvent(scale, this.GeomGraph.BoundingBox.Center, new Rectangle(0, 0, w, h));
            Size size = new Size(w, h);

            // Measure and arrange the surface
            // VERY IMPORTANT
            this.graphCanvas.Measure(size);
            this.graphCanvas.Arrange(new Rect(size));
            foreach (var node in this.drawingGraph.Nodes.Concat(this.drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                IViewerObject o;
                if (this.drawingObjectsToIViewerObjects.TryGetValue(node, out o))
                {
                    ((VNode)o).Invalidate();
                }
            }

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(w, h, this.DpiX, this.DpiY, PixelFormats.Pbgra32);
            renderBitmap.Render(this.graphCanvas);

            if (fileName != null)
            {
                // Create a file stream for saving image
                using (System.IO.FileStream outStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
                {
                    // Use png encoder for our data
                    PngBitmapEncoder encoder = new PngBitmapEncoder();

                    // push the rendered bitmap to it
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    // save the data to the stream
                    encoder.Save(outStream);
                }
            }

            this.graphCanvas.LayoutTransform = ltrans;
            this.graphCanvas.RenderTransform = rtrans;
            this.graphCanvas.Measure(renderSize);
            this.graphCanvas.Arrange(new Rect(renderSize));

            return new Image { Source = renderBitmap };
        }

        /// <summary>
        /// Register label creator.
        /// </summary>
        /// <param name="drawingObject">Drawing object.</param>
        /// <param name="func">Function from drawing object to framework element.</param>
        public void RegisterLabelCreator(DrawingObject drawingObject, Func<DrawingObject, FrameworkElement> func)
        {
            this.registeredCreators[drawingObject] = func;
        }

        /// <summary>
        /// Unregister label creator.
        /// </summary>
        /// <param name="drawingObject">Drawing object.</param>
        public void UnregisterLabelCreator(DrawingObject drawingObject)
        {
            this.registeredCreators.Remove(drawingObject);
        }

        /// <summary>
        /// Get label creator function.
        /// </summary>
        /// <param name="drawingObject">Drawing object.</param>
        /// <returns>Label creator function.</returns>
        public Func<DrawingObject, FrameworkElement> GetLabelCreator(DrawingObject drawingObject)
        {
            return this.registeredCreators[drawingObject];
        }

        /// <summary>
        /// Draw rubber line.
        /// </summary>
        /// <param name="args">Mouse event args.</param>
        public void DrawRubberLine(MsaglMouseEventArgs args)
        {
            this.DrawRubberLine(this.ScreenToSource(args));
        }

        /// <summary>
        /// Stop drawing rubber line.
        /// </summary>
        public void StopDrawingRubberLine()
        {
            this.graphCanvas.Children.Remove(this.rubberLinePath);
            this.rubberLinePath = null;
            this.graphCanvas.Children.Remove(this.targetArrowheadPathForRubberEdge);
            this.targetArrowheadPathForRubberEdge = null;
        }

        /// <summary>
        /// Add edge.
        /// </summary>
        /// <param name="edge">Edge to be added.</param>
        /// <param name="registerForUndo">Whether to register for undo.</param>
        public void AddEdge(IViewerEdge edge, bool registerForUndo)
        {
            var drawingEdge = edge.Edge;
            Edge geomEdge = drawingEdge.GeometryEdge;

            this.drawingGraph.AddPrecalculatedEdge(drawingEdge);
            this.drawingGraph.GeometryGraph.Edges.Add(geomEdge);
        }

        /// <summary>
        /// Create edge with given geometry.
        /// </summary>
        /// <param name="drawingEdge">Edge geometry.</param>
        /// <returns>Edge.</returns>
        public IViewerEdge CreateEdgeWithGivenGeometry(DrawingEdge drawingEdge)
        {
            return this.CreateEdge(drawingEdge, this.drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        /// <summary>
        /// Add a node.
        /// </summary>
        /// <param name="node">Node to add.</param>
        /// <param name="registerForUndo">Whether to register for undo.</param>
        public void AddNode(IViewerNode node, bool registerForUndo)
        {
            if (this.drawingGraph == null)
            {
                throw new InvalidOperationException(); // adding a node when the graph does not exist
            }

            var visNode = (VNode)node;
            this.drawingGraph.AddNode(visNode.Node);
            this.drawingGraph.GeometryGraph.Nodes.Add(visNode.Node.GeometryNode);
            this.layoutEditor.AttachLayoutChangeEvent(visNode);
            this.graphCanvas.Children.Add(visNode.FrameworkElementOfNodeForLabel);
            this.layoutEditor.CleanObstacles();
        }

        /// <summary>
        /// Add node.
        /// </summary>
        /// <param name="drawingNode">Drawing node to add.</param>
        /// <returns>Viewer object.</returns>
        public IViewerObject AddNode(Msagl.Drawing.Node drawingNode)
        {
            this.Graph.AddNode(drawingNode);
            var node = this.CreateVNode(drawingNode);
            this.LayoutEditor.AttachLayoutChangeEvent(node);
            this.LayoutEditor.CleanObstacles();
            return node;
        }

        /// <summary>
        /// Remove edge.
        /// </summary>
        /// <param name="edge">Edge to remove.</param>
        /// <param name="registerForUndo">Whether to register for undo.</param>
        public void RemoveEdge(IViewerEdge edge, bool registerForUndo)
        {
            lock (this)
            {
                var vedge = (VEdge)edge;
                var dedge = vedge.Edge;
                this.drawingGraph.RemoveEdge(dedge);
                this.drawingGraph.GeometryGraph.Edges.Remove(dedge.GeometryEdge);
                this.drawingObjectsToFrameworkElements.Remove(dedge);
                this.drawingObjectsToIViewerObjects.Remove(dedge);

                vedge.RemoveItselfFromCanvas(this.graphCanvas);
            }
        }

        /// <summary>
        /// Remove node.
        /// </summary>
        /// <param name="node">Node to remove.</param>
        /// <param name="registerForUndo">Whether to register for undo.</param>
        public void RemoveNode(IViewerNode node, bool registerForUndo)
        {
            lock (this)
            {
                this.RemoveEdges(node.Node.OutEdges);
                this.RemoveEdges(node.Node.InEdges);
                this.RemoveEdges(node.Node.SelfEdges);
                this.drawingObjectsToFrameworkElements.Remove(node.Node);
                this.drawingObjectsToIViewerObjects.Remove(node.Node);
                var vnode = (VNode)node;
                vnode.DetatchFromCanvas(this.graphCanvas);

                this.drawingGraph.RemoveNode(node.Node);
                this.drawingGraph.GeometryGraph.Nodes.Remove(node.Node.GeometryNode);
                this.layoutEditor.DetachNode(node);
                this.layoutEditor.CleanObstacles();
            }
        }

        /// <summary>
        /// Route edge.
        /// </summary>
        /// <param name="drawingEdge">Drawing edge to route.</param>
        /// <returns>Viewer edge.</returns>
        public IViewerEdge RouteEdge(DrawingEdge drawingEdge)
        {
            var geomEdge = GeometryGraphCreator.CreateGeometryEdgeFromDrawingEdge(drawingEdge);
            var geomGraph = this.drawingGraph.GeometryGraph;
            LayoutHelpers.RouteAndLabelEdges(geomGraph, this.drawingGraph.LayoutAlgorithmSettings, new[] { geomEdge });
            return this.CreateEdge(drawingEdge, this.drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings);
        }

        /// <summary>
        /// Set source port for edge routing.
        /// </summary>
        /// <param name="portLocation">Port location.</param>
        public void SetSourcePortForEdgeRouting(Point portLocation)
        {
            this.sourcePortLocationForEdgeRouting = portLocation;
            if (this.sourcePortCircle == null)
            {
                this.sourcePortCircle = this.CreatePortPath();
                this.graphCanvas.Children.Add(this.sourcePortCircle);
            }

            this.sourcePortCircle.Width = this.sourcePortCircle.Height = this.UnderlyingPolylineCircleRadius;
            this.sourcePortCircle.StrokeThickness = this.sourcePortCircle.Width / 10;
            Common.PositionFrameworkElement(this.sourcePortCircle, portLocation, 1);
        }

        /// <summary>
        /// Set target port for edge routing.
        /// </summary>
        /// <param name="portLocation">Point of port location.</param>
        public void SetTargetPortForEdgeRouting(Point portLocation)
        {
            if (this.TargetPortCircle == null)
            {
                this.TargetPortCircle = this.CreatePortPath();
                this.graphCanvas.Children.Add(this.TargetPortCircle);
            }

            this.TargetPortCircle.Width = this.TargetPortCircle.Height = this.UnderlyingPolylineCircleRadius;
            this.TargetPortCircle.StrokeThickness = this.TargetPortCircle.Width / 10;
            Common.PositionFrameworkElement(this.TargetPortCircle, portLocation, 1);
        }

        /// <summary>
        /// Remove source port edge routing.
        /// </summary>
        public void RemoveSourcePortEdgeRouting()
        {
            this.graphCanvas.Children.Remove(this.sourcePortCircle);
            this.sourcePortCircle = null;
        }

        /// <summary>
        /// Remove target port edge routing.
        /// </summary>
        public void RemoveTargetPortEdgeRouting()
        {
            this.graphCanvas.Children.Remove(this.TargetPortCircle);
            this.TargetPortCircle = null;
        }

        /// <summary>
        /// Draw rubber edge.
        /// </summary>
        /// <param name="edgeGeometry">Edge geometry.</param>
        public void DrawRubberEdge(EdgeGeometry edgeGeometry)
        {
            if (this.rubberEdgePath == null)
            {
                this.rubberEdgePath = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = this.GetBorderPathThickness() * 3,
                };
                this.graphCanvas.Children.Add(this.rubberEdgePath);
                this.targetArrowheadPathForRubberEdge = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = this.GetBorderPathThickness() * 3,
                };
                this.graphCanvas.Children.Add(this.targetArrowheadPathForRubberEdge);
            }

            this.rubberEdgePath.Data = VEdge.GetICurveWpfGeometry(edgeGeometry.Curve);
            this.targetArrowheadPathForRubberEdge.Data = VEdge.DefiningTargetArrowHead(edgeGeometry, edgeGeometry.LineWidth);
        }

        /// <summary>
        /// Stop drawing rubber edge.
        /// </summary>
        public void StopDrawingRubberEdge()
        {
            this.graphCanvas.Children.Remove(this.rubberEdgePath);
            this.graphCanvas.Children.Remove(this.targetArrowheadPathForRubberEdge);
            this.rubberEdgePath = null;
            this.targetArrowheadPathForRubberEdge = null;
        }

        /// <summary>
        /// Draw rubber line.
        /// </summary>
        /// <param name="rubberEnd">Rubber end point.</param>
        public void DrawRubberLine(Point rubberEnd)
        {
            if (this.rubberLinePath == null)
            {
                this.rubberLinePath = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = this.GetBorderPathThickness() * 3,
                };
                this.graphCanvas.Children.Add(this.rubberLinePath);
            }

            this.rubberLinePath.Data =
                VEdge.GetICurveWpfGeometry(new LineSegment(this.sourcePortLocationForEdgeRouting, rubberEnd));
        }

        /// <summary>
        /// Create viewer node.
        /// </summary>
        /// <param name="drawingNode">Drawing node.</param>
        /// <param name="center">Center point.</param>
        /// <param name="visualElement">Visual element.</param>
        /// <returns>Viewer node.</returns>
        public IViewerNode CreateIViewerNode(Msagl.Drawing.Node drawingNode, Point center, object visualElement)
        {
            if (this.drawingGraph == null)
            {
                return null;
            }

            var frameworkElement = visualElement as FrameworkElement ?? this.CreateTextBlockForDrawingObj(drawingNode);
            var width = frameworkElement.Width + 2 * drawingNode.Attr.LabelMargin;
            var height = frameworkElement.Height + 2 * drawingNode.Attr.LabelMargin;
            var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(drawingNode, width, height);
            drawingNode.GeometryNode = new Node(bc, drawingNode) { Center = center };
            var node = this.CreateVNode(drawingNode);
            this.drawingGraph.AddNode(drawingNode);
            this.drawingGraph.GeometryGraph.Nodes.Add(drawingNode.GeometryNode);
            this.layoutEditor.AttachLayoutChangeEvent(node);
            this.MakeRoomForNewNode(drawingNode);

            return node;
        }

        /// <summary>
        /// Keeps centerOfZoom pinned to the screen and changes the scale by zoomFactor.
        /// </summary>
        /// <param name="zoomFactor">Zoom factor.</param>
        /// <param name="centerOfZoom">Center point of zoom.</param>
        public void ZoomAbout(double zoomFactor, WpfPoint centerOfZoom)
        {
            var scale = zoomFactor * this.FitFactor;
            var centerOfZoomOnScreen =
                this.graphCanvas.TransformToAncestor((FrameworkElement)this.graphCanvas.Parent).Transform(centerOfZoom);
            this.SetTransform(scale, centerOfZoomOnScreen.X - centerOfZoom.X * scale, centerOfZoomOnScreen.Y + centerOfZoom.Y * scale);
        }

        /// <summary>
        /// Moves the point to the center of the viewport.
        /// </summary>
        /// <param name="sourcePoint">Source point.</param>
        public void PointToCenter(Point sourcePoint)
        {
            WpfPoint center = new WpfPoint(this.graphCanvas.RenderSize.Width / 2, this.graphCanvas.RenderSize.Height / 2);
            this.SetTransformFromTwoPoints(center, sourcePoint);
        }

        /// <summary>
        /// Pan node to center with scale.
        /// </summary>
        /// <param name="node">Node to center.</param>
        /// <param name="scale">Scale factor.</param>
        public void NodeToCenterWithScale(Msagl.Drawing.Node node, double scale)
        {
            if (node.GeometryNode == null)
            {
                return;
            }

            var screenPoint = new WpfPoint(this.graphCanvas.RenderSize.Width / 2, this.graphCanvas.RenderSize.Height / 2);
            var sourcePoint = node.BoundingBox.Center;
            this.SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }

        /// <summary>
        /// Pan node to center.
        /// </summary>
        /// <param name="node">Node to center.</param>
        public void NodeToCenter(Msagl.Drawing.Node node)
        {
            if (node.GeometryNode == null)
            {
                return;
            }

            this.PointToCenter(node.GeometryNode.Center);
        }

        /// <summary>
        /// Invalidate child object.
        /// </summary>
        /// <param name="objectToInvalidate">Child object to invalidate.</param>
        public void Invalidate(IViewerObject objectToInvalidate)
        {
            ((IInvalidatable)objectToInvalidate).Invalidate();
        }

        /// <summary>
        /// Invalidate graph viewer.
        /// </summary>
        public void Invalidate()
        {
        }

        /// <summary>
        /// Screen point (under mouse) to source point.
        /// </summary>
        /// <param name="e">Mouse event args.</param>
        /// <returns>Source point.</returns>
        public Point ScreenToSource(MsaglMouseEventArgs e)
        {
            var p = new Point(e.X, e.Y);
            var m = this.Transform.Inverse;
            return m * p;
        }

        /// <summary>
        /// adds the main panel of the viewer to the children of the parent.
        /// </summary>
        /// <param name="panel">Panel to which to bind.</param>
        public void BindToPanel(Panel panel)
        {
            panel.Children.Add(this.GraphCanvas);
            this.GraphCanvas.UpdateLayout();
        }

        /// <inheritdoc/>
        public void StartDrawingRubberLine(Point startingPoint)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clear graph viewer.
        /// </summary>
        public void ClearGraphViewer()
        {
            this.ClearGraphCanvasChildren();

            this.drawingObjectsToIViewerObjects.Clear();
            this.drawingObjectsToFrameworkElements.Clear();
        }

        /// <summary>
        /// Create MSAGL mouse event args.
        /// </summary>
        /// <param name="e">Mouse event args.</param>
        /// <returns>MSAGL mouse event args.</returns>
        internal MsaglMouseEventArgs CreateMouseEventArgs(MouseEventArgs e)
        {
            return new GvMouseEventArgs(e, this);
        }

        /// <summary>
        /// Create text block.
        /// </summary>
        /// <param name="drawingLabel">Drawing label.</param>
        /// <returns>Text block.</returns>
        private static TextBlock CreateTextBlock(Label drawingLabel)
        {
            var (text, _) = VNode.GetLabelTextAndToolTip(drawingLabel.Text);
            var textBlock = new TextBlock
            {
                Tag = drawingLabel,
                Text = text,
                FontFamily = new FontFamily(drawingLabel.FontName),
                FontSize = drawingLabel.FontSize,
                Foreground = Common.BrushFromMsaglColor(drawingLabel.FontColor),
            };

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Width = textBlock.DesiredSize.Width;
            textBlock.Height = textBlock.DesiredSize.Height;
            return textBlock;
        }

        private static void GetDpi()
        {
            int hdcSrc = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
            dpiX = NativeMethods.GetDeviceCaps(hdcSrc, 88);
            dpiY = NativeMethods.GetDeviceCaps(hdcSrc, 90);
            NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hdcSrc);
        }

        private void ClickCounterElapsed(object sender, EventArgs e)
        {
            var vedge = this.clickCounter.ClickedObject as VEdge;
            if (vedge != null)
            {
                if (this.clickCounter.UpCount == this.clickCounter.DownCount && this.clickCounter.UpCount == 1)
                {
                    this.HandleClickForEdge(vedge);
                }
            }

            this.clickCounter.ClickedObject = null;
        }

        private void AdjustBtrectRenderTransform(object sender, EventArgs e)
        {
            if (this.rectToFillCanvas == null)
            {
                return;
            }

            this.rectToFillCanvas.RenderTransform = (Transform)this.graphCanvas.RenderTransform.Inverse;
            var parent = (Panel)this.GraphCanvas.Parent;
            this.rectToFillCanvas.Width = parent.ActualWidth;
            this.rectToFillCanvas.Height = parent.ActualHeight;
        }

        private void GraphCanvasLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.OnMouseUp(e);
        }

        private void HandleClickForEdge(VEdge edge)
        {
            var layoutSettings = this.Graph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (layoutSettings != null)
            {
                var geo = layoutSettings.GeometryEdgesToLgEdgeInfos[edge.Edge.GeometryEdge];
                geo.SlidingZoomLevel = geo.SlidingZoomLevel != 0 ? 0 : double.PositiveInfinity;

                this.ViewChangeEvent(null, null);
            }
        }

        private void GraphCanvasLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.MouseDown != null)
            {
                this.MouseDown(this, this.CreateMouseEventArgs(e));
            }
        }

        private void GraphCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                const double zoomFractionLocal = 0.9;
                var zoomInc = e.Delta < 0 ? zoomFractionLocal : 1.0 / zoomFractionLocal;
                this.ZoomAbout(this.ZoomFactor * zoomInc, e.GetPosition(this.graphCanvas));
                e.Handled = true;
                this.MouseWheel(this, this.CreateMouseEventArgs(e)); // TODO: real mouse *wheel* event args (including delta)
            }
        }

        private void GraphCanvasMouseRightButtonDown(object sender, MouseEventArgs e)
        {
            this.clickCounter.AddMouseDown(this.objectUnderMouseCursor);
            if (this.MouseDown != null)
            {
                this.MouseDown(this, this.CreateMouseEventArgs(e));
            }

            if (e.Handled)
            {
                return;
            }

            this.mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(this.graphCanvas));
            this.mouseDownPositionInGraphInitialized = true;
        }

        private void GraphCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (this.MouseMove != null)
            {
                this.MouseMove(this, this.CreateMouseEventArgs(e));
            }

            if (e.Handled)
            {
                return;
            }

            if (Mouse.RightButton == MouseButtonState.Pressed && (!this.LayoutEditingEnabled || this.objectUnderMouseCursor == null))
            {
                if (!this.mouseDownPositionInGraphInitialized)
                {
                    this.mouseDownPositionInGraph = Common.MsaglPoint(e.GetPosition(this.graphCanvas));
                    this.mouseDownPositionInGraphInitialized = true;
                }

                this.Pan(e);
            }
            else
            {
                // Retrieve the coordinate of the mouse position.
                WpfPoint mouseLocation = e.GetPosition(this.graphCanvas);

                // Clear the contents of the list used for hit test results.
                this.ObjectUnderMouseCursor = null;
                this.UpdateWithWpfHitObjectUnderMouseOnLocation(mouseLocation, this.MyHitTestResultCallback);
            }
        }

        private void UpdateWithWpfHitObjectUnderMouseOnLocation(WpfPoint pt, HitTestResultCallback hitTestResultCallback)
        {
            this.objectUnderMouseDetectionLocation = pt;

            // Expand the hit test area by creating a geometry centered on the hit test point.
            var rect = new Rect(new WpfPoint(pt.X - this.MouseHitTolerance, pt.Y - this.MouseHitTolerance), new WpfPoint(pt.X + this.MouseHitTolerance, pt.Y + this.MouseHitTolerance));
            var expandedHitTestArea = new RectangleGeometry(rect);

            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(this.graphCanvas, null, hitTestResultCallback, new GeometryHitTestParameters(expandedHitTestArea));
        }

        // Return the result of the hit test to the callback.
        private HitTestResultBehavior MyHitTestResultCallback(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
            {
                return HitTestResultBehavior.Continue;
            }

            if (frameworkElement.Tag == null)
            {
                return HitTestResultBehavior.Continue;
            }

            var tag = frameworkElement.Tag;
            var iviewerObj = tag as IViewerObject;
            if (iviewerObj != null && iviewerObj.DrawingObject.IsVisible)
            {
                if (this.ObjectUnderMouseCursor is IViewerEdge || this.ObjectUnderMouseCursor == null
                    ||
                    Panel.GetZIndex(frameworkElement) >
                    Panel.GetZIndex(this.GetFrameworkElementFromIViewerObject(this.ObjectUnderMouseCursor)))
                {
                    // always overwrite an edge or take the one with greater zIndex
                    this.ObjectUnderMouseCursor = iviewerObj;
                }
            }

            return HitTestResultBehavior.Continue;
        }

        private FrameworkElement GetFrameworkElementFromIViewerObject(IViewerObject viewerObject)
        {
            FrameworkElement ret;

            var node = viewerObject as VNode;
            if (node != null)
            {
                ret = node.FrameworkElementOfNodeForLabel ?? node.BoundaryPath;
            }
            else
            {
                var label = viewerObject as VLabel;
                if (label != null)
                {
                    ret = label.FrameworkElement;
                }
                else
                {
                    var edge = viewerObject as VEdge;
                    if (edge != null)
                    {
                        ret = edge.CurvePath;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected object type in GraphViewer");
                    }
                }
            }

            if (ret == null)
            {
                throw new InvalidOperationException("did not find a framework element!");
            }

            return ret;
        }

        // Return the result of the hit test to the callback.
        private HitTestResultBehavior MyHitTestResultCallbackWithNoCallbacksToTheUser(HitTestResult result)
        {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
            {
                return HitTestResultBehavior.Continue;
            }

            object tag = frameworkElement.Tag;
            if (tag != null)
            {
                // it is a tagged element
                var ivo = tag as IViewerObject;
                if (ivo != null)
                {
                    if (ivo.DrawingObject.IsVisible)
                    {
                        this.objectUnderMouseCursor = ivo;
                        if (tag is VNode || tag is Label)
                        {
                            return HitTestResultBehavior.Stop;
                        }
                    }
                }
                else
                {
                    this.objectUnderMouseCursor = tag;
                    return HitTestResultBehavior.Stop;
                }
            }

            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// this function pins the sourcePoint to screenPoint.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="sourcePoint">Source point.</param>
        private void SetTransformFromTwoPoints(WpfPoint screenPoint, Point sourcePoint)
        {
            var scale = this.CurrentScale;
            this.SetTransform(scale, screenPoint.X - scale * sourcePoint.X, screenPoint.Y + scale * sourcePoint.Y);
        }

        private void Pan(MouseEventArgs e)
        {
            if (this.UnderLayout)
            {
                return;
            }

            if (!this.graphCanvas.IsMouseCaptured)
            {
                this.graphCanvas.CaptureMouse();
            }

            this.SetTransformFromTwoPoints(e.GetPosition((FrameworkElement)this.graphCanvas.Parent), this.mouseDownPositionInGraph);

            if (this.ViewChangeEvent != null)
            {
                this.ViewChangeEvent(null, null);
            }
        }

        private void GraphCanvasMouseRightButtonUp(object sender, MouseEventArgs e)
        {
            this.OnMouseUp(e);
            this.clickCounter.AddMouseUp();
            if (this.graphCanvas.IsMouseCaptured)
            {
                e.Handled = true;
                this.graphCanvas.ReleaseMouseCapture();
            }
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            if (this.MouseUp != null)
            {
                this.MouseUp(this, this.CreateMouseEventArgs(e));
            }
        }

        private void GraphCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.drawingGraph == null)
            {
                return;
            }

            // keep the same zoom level
            double oldfit = this.GetFitFactor(e.PreviousSize);
            double fitNow = this.FitFactor;
            double scaleFraction = fitNow / oldfit;
            this.SetTransform(this.CurrentScale * scaleFraction, this.CurrentXOffset * scaleFraction, this.CurrentYOffset * scaleFraction);
        }

        private IViewerObject GetIViewerObjectFromObjectUnderCursor(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            return obj as IViewerObject;
        }

        private double GetBorderPathThickness()
        {
            return DesiredPathThicknessInInches * this.DpiX;
        }

        private void ContextMenuClosed(object sender, RoutedEventArgs e)
        {
            ContextMenuService.SetContextMenu(this.graphCanvas, null);
        }

        private void ProcessGraph()
        {
            lock (this.processGraphLock)
            {
                this.ProcessGraphUnderLock();
            }
        }

        private void ProcessGraphUnderLock()
        {
            try
            {
                if (this.LayoutStarted != null)
                {
                    this.LayoutStarted(null, null);
                }

                this.CancelToken = new CancelToken();

                if (this.drawingGraph == null)
                {
                    return;
                }

                this.HideCanvas();
                this.ClearGraphViewer();
                this.CreateFrameworkElementsForLabelsOnly();
                if (this.NeedToCalculateLayout)
                {
                    this.drawingGraph.CreateGeometryGraph(); // forcing the layout recalculation
                    if (this.graphCanvas.Dispatcher.CheckAccess())
                    {
                        this.PopulateGeometryOfGeometryGraph();
                    }
                    else
                    {
                        this.graphCanvas.Dispatcher.Invoke(this.PopulateGeometryOfGeometryGraph);
                    }
                }

                this.geometryGraphUnderLayout = this.drawingGraph.GeometryGraph;
                if (this.RunLayoutAsync)
                {
                    this.SetUpBackgrounWorkerAndRunAsync();
                }
                else
                {
                    this.RunLayoutInUIThread();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void RunLayoutInUIThread()
        {
            this.LayoutGraph();
            this.PostLayoutStep();
            if (this.LayoutComplete != null)
            {
                this.LayoutComplete(null, null);
            }
        }

        private void SetUpBackgrounWorkerAndRunAsync()
        {
            this.backgroundWorker = new BackgroundWorker();
            this.backgroundWorker.DoWork += (a, b) => this.LayoutGraph();
            this.backgroundWorker.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    MessageBox.Show(args.Error.ToString());
                    this.ClearGraphViewer();
                }
                else if (this.CancelToken.Canceled)
                {
                    this.ClearGraphViewer();
                }
                else
                {
                    if (this.graphCanvas.Dispatcher.CheckAccess())
                    {
                        this.PostLayoutStep();
                    }
                    else
                    {
                        this.graphCanvas.Dispatcher.Invoke(this.PostLayoutStep);
                    }
                }

                this.backgroundWorker = null; // this will signal that we are not under layout anymore
                if (this.LayoutComplete != null)
                {
                    this.LayoutComplete(null, null);
                }
            };
            this.backgroundWorker.RunWorkerAsync();
        }

        private void HideCanvas()
        {
            if (this.graphCanvas.Dispatcher.CheckAccess())
            {
                this.graphCanvas.Visibility = Visibility.Hidden; // hide canvas while we lay it out asynchronously.
            }
            else
            {
                this.graphCanvas.Dispatcher.Invoke(() => this.graphCanvas.Visibility = Visibility.Hidden);
            }
        }

        private void LayoutGraph()
        {
            if (this.NeedToCalculateLayout)
            {
                try
                {
                    LayoutHelpers.CalculateLayout(this.geometryGraphUnderLayout, this.drawingGraph.LayoutAlgorithmSettings, this.CancelToken);
                }
                catch (OperationCanceledException)
                {
                    // swallow this exception
                }
            }
        }

        private void PostLayoutStep()
        {
            this.graphCanvas.Visibility = Visibility.Visible;
            this.PushDataFromLayoutGraphToFrameworkElements();
            this.backgroundWorker = null; // this will signal that we are not under layout anymore
            if (this.GraphChanged != null)
            {
                this.GraphChanged(this, null);
            }

            this.SetInitialTransform();
        }

        private void ClearGraphCanvasChildren()
        {
            if (this.graphCanvas.Dispatcher.CheckAccess())
            {
                this.graphCanvas.Children.Clear();
            }
            else
            {
                this.graphCanvas.Dispatcher.Invoke(() => this.graphCanvas.Children.Clear());
            }
        }

        private void SetTransformOnViewportWithoutRaisingViewChangeEvent(double scale, Point graphCenter, Rectangle vp)
        {
            var dx = vp.Width / 2 - scale * graphCenter.X;
            var dy = vp.Height / 2 + scale * graphCenter.Y;

            this.SetTransformWithoutRaisingViewChangeEvent(scale, dx, dy);
        }

        private void SetTransform(double scale, double dx, double dy)
        {
            if (this.ScaleIsOutOfRange(scale))
            {
                return;
            }

            this.graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
            if (this.ViewChangeEvent != null)
            {
                this.ViewChangeEvent(null, null);
            }
        }

        private void SetTransformWithoutRaisingViewChangeEvent(double scale, double dx, double dy)
        {
            if (this.ScaleIsOutOfRange(scale))
            {
                return;
            }

            this.graphCanvas.RenderTransform = new MatrixTransform(scale, 0, 0, -scale, dx, dy);
        }

        private bool ScaleIsOutOfRange(double scale)
        {
            return scale < 0.000001 || scale > 100000.0; // todo: remove hardcoded values
        }

        private double GetFitFactor(Size rect)
        {
            var geomGraph = this.GeomGraph;
            return geomGraph == null ? 1 : Math.Min(rect.Width / geomGraph.Width, rect.Height / geomGraph.Height);
        }

        private void PushDataFromLayoutGraphToFrameworkElements()
        {
            this.CreateRectToFillCanvas();
            this.CreateAndPositionGraphBackgroundRectangle();
            this.CreateVNodes();
            this.CreateEdges();
        }

        private void CreateRectToFillCanvas()
        {
            var parent = (Panel)this.GraphCanvas.Parent;
            this.rectToFillCanvas = new System.Windows.Shapes.Rectangle();
            Canvas.SetLeft(this.rectToFillCanvas, 0);
            Canvas.SetTop(this.rectToFillCanvas, 0);
            this.rectToFillCanvas.Width = parent.ActualWidth;
            this.rectToFillCanvas.Height = parent.ActualHeight;

            this.rectToFillCanvas.Fill = Brushes.Transparent;
            Panel.SetZIndex(this.rectToFillCanvas, -2);
            this.graphCanvas.Children.Add(this.rectToFillCanvas);
        }

        private void CreateEdges()
        {
            foreach (var edge in this.drawingGraph.Edges)
            {
                this.CreateEdge(edge, null);
            }
        }

        private VEdge CreateEdge(DrawingEdge edge, LgLayoutSettings layoutSettings)
        {
            lock (this)
            {
                if (this.drawingObjectsToIViewerObjects.ContainsKey(edge))
                {
                    return (VEdge)this.drawingObjectsToIViewerObjects[edge];
                }

                if (layoutSettings != null)
                {
                    return this.CreateEdgeForLgCase(layoutSettings, edge);
                }

                FrameworkElement labelTextBox;
                this.drawingObjectsToFrameworkElements.TryGetValue(edge, out labelTextBox);
                var visEdge = new VEdge(edge, labelTextBox);

                var index = this.ZIndexOfEdge(edge);
                this.drawingObjectsToIViewerObjects[edge] = visEdge;

                if (edge.Label != null)
                {
                    this.SetVEdgeLabel(edge, visEdge, index);
                }

                Panel.SetZIndex(visEdge.CurvePath, index);
                this.graphCanvas.Children.Add(visEdge.CurvePath);
                this.SetVEdgeArrowheads(visEdge, index);

                return visEdge;
            }
        }

        private int ZIndexOfEdge(DrawingEdge edge)
        {
            var source = (VNode)this.drawingObjectsToIViewerObjects[edge.SourceNode];
            var target = (VNode)this.drawingObjectsToIViewerObjects[edge.TargetNode];

            return Math.Max(source.ZIndex, target.ZIndex) + 1;
        }

        private VEdge CreateEdgeForLgCase(LgLayoutSettings layoutSettings, DrawingEdge edge)
        {
            return (VEdge)(this.drawingObjectsToIViewerObjects[edge] = new VEdge(edge, layoutSettings)
            {
                PathStrokeThicknessFunc = () => this.GetBorderPathThickness() * edge.Attr.LineWidth,
            });
        }

        private void SetVEdgeLabel(DrawingEdge edge, VEdge visEdge, int index)
        {
            FrameworkElement frameworkElementForEdgeLabel;
            if (!this.drawingObjectsToFrameworkElements.TryGetValue(edge, out frameworkElementForEdgeLabel))
            {
                this.drawingObjectsToFrameworkElements[edge] =
                    frameworkElementForEdgeLabel = this.CreateTextBlockForDrawingObj(edge);
                frameworkElementForEdgeLabel.Tag = new VLabel(edge, frameworkElementForEdgeLabel);
            }

            visEdge.VLabel = (VLabel)frameworkElementForEdgeLabel.Tag;
            if (frameworkElementForEdgeLabel.Parent == null)
            {
                this.graphCanvas.Children.Add(frameworkElementForEdgeLabel);
                Panel.SetZIndex(frameworkElementForEdgeLabel, index);
            }
        }

        private void SetVEdgeArrowheads(VEdge edge, int z)
        {
            if (edge.SourceArrowHeadPath != null)
            {
                Panel.SetZIndex(edge.SourceArrowHeadPath, z);
                this.graphCanvas.Children.Add(edge.SourceArrowHeadPath);
            }

            if (edge.TargetArrowHeadPath != null)
            {
                Panel.SetZIndex(edge.TargetArrowHeadPath, z);
                this.graphCanvas.Children.Add(edge.TargetArrowHeadPath);
            }
        }

        private void CreateVNodes()
        {
            foreach (var node in this.drawingGraph.Nodes.Concat(this.drawingGraph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf()))
            {
                this.CreateVNode(node);
                this.Invalidate(this.drawingObjectsToIViewerObjects[node]);
            }
        }

        private IViewerNode CreateVNode(Msagl.Drawing.Node node)
        {
            lock (this)
            {
                if (this.drawingObjectsToIViewerObjects.ContainsKey(node))
                {
                    return (IViewerNode)this.drawingObjectsToIViewerObjects[node];
                }

                FrameworkElement labelElement;
                if (!this.drawingObjectsToFrameworkElements.TryGetValue(node, out labelElement))
                {
                    labelElement = this.CreateAndRegisterFrameworkElementOfDrawingNode(node);
                }

                var vn = new VNode(node, labelElement, e => (VEdge)this.drawingObjectsToIViewerObjects[e], () => this.GetBorderPathThickness() * node.Attr.LineWidth);

                foreach (var fe in vn.FrameworkElements)
                {
                    this.graphCanvas.Children.Add(fe);
                }

                this.drawingObjectsToIViewerObjects[node] = vn;

                return vn;
            }
        }

        private void CreateAndPositionGraphBackgroundRectangle()
        {
            this.CreateGraphBackgroundRect();
            this.SetBackgroundRectanglePositionAndSize();

            var rect = this.rectToFillGraphBackground as System.Windows.Shapes.Rectangle;
            if (rect != null)
            {
                rect.Fill = Common.BrushFromMsaglColor(this.drawingGraph.Attr.BackgroundColor);
            }

            Panel.SetZIndex(this.rectToFillGraphBackground, -1);
            this.graphCanvas.Children.Add(this.rectToFillGraphBackground);
        }

        private void CreateGraphBackgroundRect()
        {
            var browingSettings = this.drawingGraph.LayoutAlgorithmSettings as LgLayoutSettings;
            if (browingSettings == null)
            {
                this.rectToFillGraphBackground = new System.Windows.Shapes.Rectangle();
            }
        }

        private void SetBackgroundRectanglePositionAndSize()
        {
            if (this.GeomGraph == null)
            {
                return;
            }

            this.rectToFillGraphBackground.Width = this.GeomGraph.Width;
            this.rectToFillGraphBackground.Height = this.GeomGraph.Height;

            var center = this.GeomGraph.BoundingBox.Center;
            Common.PositionFrameworkElement(this.rectToFillGraphBackground, center, 1);
        }

        private void PopulateGeometryOfGeometryGraph()
        {
            this.geometryGraphUnderLayout = this.drawingGraph.GeometryGraph;
            foreach (
                Node msaglNode in
                    this.geometryGraphUnderLayout.Nodes)
            {
                var node = (Msagl.Drawing.Node)msaglNode.UserData;
                if (this.graphCanvas.Dispatcher.CheckAccess())
                {
                    msaglNode.BoundaryCurve = this.GetNodeBoundaryCurve(node);
                }
                else
                {
                    var msagNodeInThread = msaglNode;
                    this.graphCanvas.Dispatcher.Invoke(() => msagNodeInThread.BoundaryCurve = this.GetNodeBoundaryCurve(node));
                }
            }

            foreach (
                Cluster cluster in this.geometryGraphUnderLayout.RootCluster.AllClustersWideFirstExcludingSelf())
            {
                var subgraph = (Subgraph)cluster.UserData;
                if (this.graphCanvas.Dispatcher.CheckAccess())
                {
                    cluster.CollapsedBoundary = this.GetClusterCollapsedBoundary(subgraph);
                }
                else
                {
                    var clusterInThread = cluster;
                    this.graphCanvas.Dispatcher.Invoke(
                        () => clusterInThread.BoundaryCurve = this.GetClusterCollapsedBoundary(subgraph));
                }

                if (cluster.RectangularBoundary == null)
                {
                    cluster.RectangularBoundary = new RectangularClusterBoundary();
                }

                cluster.RectangularBoundary.TopMargin = subgraph.DiameterOfOpenCollapseButton + 0.5 +
                                                        subgraph.Attr.LineWidth / 2;
            }

            foreach (var msaglEdge in this.geometryGraphUnderLayout.Edges)
            {
                var drawingEdge = (DrawingEdge)msaglEdge.UserData;
                this.AssignLabelWidthHeight(msaglEdge, drawingEdge);
            }
        }

        private ICurve GetClusterCollapsedBoundary(Subgraph subgraph)
        {
            double width, height;

            FrameworkElement fe;
            if (this.drawingObjectsToFrameworkElements.TryGetValue(subgraph, out fe))
            {
                width = fe.Width + 2 * subgraph.Attr.LabelMargin + subgraph.DiameterOfOpenCollapseButton;
                height = Math.Max(fe.Height + 2 * subgraph.Attr.LabelMargin, subgraph.DiameterOfOpenCollapseButton);
            }
            else
            {
                return this.GetApproximateCollapsedBoundary(subgraph);
            }

            if (width < this.drawingGraph.Attr.MinNodeWidth)
            {
                width = this.drawingGraph.Attr.MinNodeWidth;
            }

            if (height < this.drawingGraph.Attr.MinNodeHeight)
            {
                height = this.drawingGraph.Attr.MinNodeHeight;
            }

            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }

        private ICurve GetApproximateCollapsedBoundary(Subgraph subgraph)
        {
            if (this.textBoxForApproxNodeBoundaries == null)
            {
                this.SetUpTextBoxForApproxNodeBoundaries();
            }

            double width, height;
            if (string.IsNullOrEmpty(subgraph.LabelText))
            {
                height = width = subgraph.DiameterOfOpenCollapseButton;
            }
            else
            {
                (var text, _) = VNode.GetLabelTextAndToolTip(subgraph.LabelText);
                double a = ((double)text.Length) / this.textBoxForApproxNodeBoundaries.Text.Length *
                           subgraph.Label.FontSize / Label.DefaultFontSize;
                width = this.textBoxForApproxNodeBoundaries.Width * a + subgraph.DiameterOfOpenCollapseButton;
                height =
                    Math.Max(
                        this.textBoxForApproxNodeBoundaries.Height * subgraph.Label.FontSize / Label.DefaultFontSize,
                        subgraph.DiameterOfOpenCollapseButton);
            }

            if (width < this.drawingGraph.Attr.MinNodeWidth)
            {
                width = this.drawingGraph.Attr.MinNodeWidth;
            }

            if (height < this.drawingGraph.Attr.MinNodeHeight)
            {
                height = this.drawingGraph.Attr.MinNodeHeight;
            }

            return NodeBoundaryCurves.GetNodeBoundaryCurve(subgraph, width, height);
        }

        private void AssignLabelWidthHeight(Core.Layout.ILabeledObject labeledGeomObj, DrawingObject drawingObj)
        {
            if (this.drawingObjectsToFrameworkElements.ContainsKey(drawingObj))
            {
                FrameworkElement fe = this.drawingObjectsToFrameworkElements[drawingObj];
                labeledGeomObj.Label.Width = fe.Width;
                labeledGeomObj.Label.Height = fe.Height;
            }
        }

        private ICurve GetNodeBoundaryCurve(Msagl.Drawing.Node node)
        {
            double width, height;

            FrameworkElement fe;
            if (this.drawingObjectsToFrameworkElements.TryGetValue(node, out fe))
            {
                width = fe.Width + 2 * node.Attr.LabelMargin;
                height = fe.Height + 2 * node.Attr.LabelMargin;
            }
            else
            {
                return this.GetNodeBoundaryCurveByMeasuringText(node);
            }

            if (width < this.drawingGraph.Attr.MinNodeWidth)
            {
                width = this.drawingGraph.Attr.MinNodeWidth;
            }

            if (height < this.drawingGraph.Attr.MinNodeHeight)
            {
                height = this.drawingGraph.Attr.MinNodeHeight;
            }

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }

        private ICurve GetNodeBoundaryCurveByMeasuringText(Msagl.Drawing.Node node)
        {
            double width, height;
            if (string.IsNullOrEmpty(node.LabelText))
            {
                width = 10;
                height = 10;
            }
            else
            {
                (var text, _) = VNode.GetLabelTextAndToolTip(node.LabelText);
                var size = MeasureText(text, new FontFamily(node.Label.FontName), node.Label.FontSize, this.graphCanvas);
                width = size.Width;
                height = size.Height;
            }

            width += 2 * node.Attr.LabelMargin;
            height += 2 * node.Attr.LabelMargin;

            if (width < this.drawingGraph.Attr.MinNodeWidth)
            {
                width = this.drawingGraph.Attr.MinNodeWidth;
            }

            if (height < this.drawingGraph.Attr.MinNodeHeight)
            {
                height = this.drawingGraph.Attr.MinNodeHeight;
            }

            return NodeBoundaryCurves.GetNodeBoundaryCurve(node, width, height);
        }

        private void SetUpTextBoxForApproxNodeBoundaries()
        {
            this.textBoxForApproxNodeBoundaries = new TextBlock
            {
                Text = "Fox jumping over River",
                FontFamily = new FontFamily(Label.DefaultFontName),
                FontSize = Label.DefaultFontSize,
            };

            this.textBoxForApproxNodeBoundaries.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            this.textBoxForApproxNodeBoundaries.Width = this.textBoxForApproxNodeBoundaries.DesiredSize.Width;
            this.textBoxForApproxNodeBoundaries.Height = this.textBoxForApproxNodeBoundaries.DesiredSize.Height;
        }

        private void CreateFrameworkElementsForLabelsOnly()
        {
            foreach (var edge in this.drawingGraph.Edges)
            {
                var fe = this.CreateDefaultFrameworkElementForDrawingObject(edge);
                if (fe != null)
                {
                    if (this.graphCanvas.Dispatcher.CheckAccess())
                    {
                        fe.Tag = new VLabel(edge, fe);
                    }
                    else
                    {
                        var localEdge = edge;
                        this.graphCanvas.Dispatcher.Invoke(() => fe.Tag = new VLabel(localEdge, fe));
                    }
                }
            }

            foreach (var node in this.drawingGraph.Nodes)
            {
                this.CreateDefaultFrameworkElementForDrawingObject(node);
            }

            if (this.drawingGraph.RootSubgraph != null)
            {
                foreach (var subgraph in this.drawingGraph.RootSubgraph.AllSubgraphsWidthFirstExcludingSelf())
                {
                    this.CreateDefaultFrameworkElementForDrawingObject(subgraph);
                }
            }
        }

        private FrameworkElement CreateTextBlockForDrawingObj(DrawingObject drawingObj)
        {
            Func<DrawingObject, FrameworkElement> registeredCreator;
            if (this.registeredCreators.TryGetValue(drawingObj, out registeredCreator))
            {
                return registeredCreator(drawingObj);
            }

            if (drawingObj is Subgraph)
            {
                return null; // todo: add Label support later
            }

            var labeledObj = drawingObj as ILabeledObject;
            if (labeledObj == null)
            {
                return null;
            }

            var drawingLabel = labeledObj.Label;
            if (drawingLabel == null)
            {
                return null;
            }

            TextBlock textBlock = null;
            if (this.graphCanvas.Dispatcher.CheckAccess())
            {
                textBlock = CreateTextBlock(drawingLabel);
            }
            else
            {
                this.graphCanvas.Dispatcher.Invoke(() => textBlock = CreateTextBlock(drawingLabel));
            }

            return textBlock;
        }

        private FrameworkElement CreateDefaultFrameworkElementForDrawingObject(DrawingObject drawingObject)
        {
            lock (this)
            {
                var textBlock = this.CreateTextBlockForDrawingObj(drawingObject);
                if (textBlock != null)
                {
                    this.drawingObjectsToFrameworkElements[drawingObject] = textBlock;
                }

                return textBlock;
            }
        }

        private void RemoveEdges(IEnumerable<DrawingEdge> drawingEdges)
        {
            foreach (var de in drawingEdges.ToArray())
            {
                var vedge = (VEdge)this.drawingObjectsToIViewerObjects[de];
                this.RemoveEdge(vedge, false);
            }
        }

        private Ellipse CreatePortPath()
        {
            return new Ellipse
            {
                Stroke = Brushes.Brown,
                Fill = Brushes.Brown,
            };
        }

        private void SetRenderTransformWithoutRaisingEvents(PlaneTransformation value)
        {
            this.graphCanvas.RenderTransform = new MatrixTransform(value[0, 0], value[0, 1], value[1, 0], value[1, 1], value[0, 2], value[1, 2]);
        }

        private void MakeRoomForNewNode(Msagl.Drawing.Node drawingNode)
        {
            IncrementalDragger incrementalDragger = new IncrementalDragger(new[] { drawingNode.GeometryNode }, this.Graph.GeometryGraph, this.Graph.LayoutAlgorithmSettings);
            incrementalDragger.Drag(default(Point));

            foreach (var n in incrementalDragger.ChangedGraph.Nodes)
            {
                var dn = (Msagl.Drawing.Node)n.UserData;
                var vn = this.drawingObjectsToIViewerObjects[dn] as VNode;
                if (vn != null)
                {
                    vn.Invalidate();
                }
            }

            foreach (var n in incrementalDragger.ChangedGraph.Edges)
            {
                var dn = (Msagl.Drawing.Edge)n.UserData;
                var ve = this.drawingObjectsToIViewerObjects[dn] as VEdge;
                if (ve != null)
                {
                    ve.Invalidate();
                }
            }
        }
    }
}