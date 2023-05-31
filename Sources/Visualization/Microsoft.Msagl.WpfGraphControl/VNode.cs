// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Msagl.Core.Geometry.Curves;
    using Microsoft.Msagl.Core.Layout;
    using Microsoft.Msagl.Drawing;
    using Edge = Microsoft.Msagl.Drawing.Edge;
    using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
    using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
    using Node = Microsoft.Msagl.Drawing.Node;
    using Point = Microsoft.Msagl.Core.Geometry.Point;
    using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
    using Shape = Microsoft.Msagl.Drawing.Shape;
    using Size = System.Windows.Size;

    /// <summary>
    /// Visual node.
    /// </summary>
    public class VNode : IViewerNode, IInvalidatable
    {
        private readonly Brush collapseSymbolPathInactive = Brushes.Silver;
        private readonly Func<Edge, VEdge> funcFromDrawingEdgeToVEdge;
        private Subgraph subgraph;
        private Node node;
        private Border collapseButtonBorder;
        private Rectangle topMarginRect;
        private Path collapseSymbolPath;
        private bool markedForDragging;
        private Func<double> pathStrokeThicknessFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="VNode"/> class.
        /// </summary>
        /// <param name="node">Underlying node.</param>
        /// <param name="frameworkElementOfNodeForLabelOfLabel">Underlying framework element.</param>
        /// <param name="funcFromDrawingEdgeToVEdge">Function from drawing to visual edge.</param>
        /// <param name="pathStrokeThicknessFunc">Function to path stroke thickness.</param>
        internal VNode(Node node, FrameworkElement frameworkElementOfNodeForLabelOfLabel, Func<Edge, VEdge> funcFromDrawingEdgeToVEdge, Func<double> pathStrokeThicknessFunc)
        {
            this.pathStrokeThicknessFunc = pathStrokeThicknessFunc;
            this.Node = node;
            this.FrameworkElementOfNodeForLabel = frameworkElementOfNodeForLabelOfLabel;

            this.funcFromDrawingEdgeToVEdge = funcFromDrawingEdgeToVEdge;

            this.CreateNodeBoundaryPath();
            if (this.FrameworkElementOfNodeForLabel != null)
            {
                this.FrameworkElementOfNodeForLabel.Tag = this; // get a backpointer to the VNode
                Common.PositionFrameworkElement(this.FrameworkElementOfNodeForLabel, node.GeometryNode.Center, 1);
                Panel.SetZIndex(this.FrameworkElementOfNodeForLabel, Panel.GetZIndex(this.BoundaryPath) + 1);
            }

            this.SetupSubgraphDrawing();
            this.Node.Attr.VisualsChanged += (a, b) => this.Invalidate();
            this.Node.IsVisibleChanged += obj =>
            {
                foreach (var frameworkElement in this.FrameworkElements)
                {
                    frameworkElement.Visibility = this.Node.IsVisible ? Visibility.Visible : Visibility.Hidden;
                }
            };
        }

        /// <summary>
        /// Collapsed changed event.
        /// </summary>
        public event Action<IViewerNode> IsCollapsedChanged;

        /// <summary>
        /// Marked for dragging event.
        /// </summary>
        public event EventHandler MarkedForDraggingEvent;

        /// <summary>
        /// Unmarked for dragging event.
        /// </summary>
        public event EventHandler UnmarkedForDraggingEvent;

        /// <summary>
        /// Gets underlying node.
        /// </summary>
        public Node Node
        {
            get { return this.node; }

            private set
            {
                this.node = value;
                this.subgraph = this.node as Subgraph;
            }
        }

        /// <summary>
        /// Gets underlying drawing object.
        /// </summary>
        public DrawingObject DrawingObject
        {
            get { return this.Node; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether marked for dragging.
        /// </summary>
        public bool MarkedForDragging
        {
            get
            {
                return this.markedForDragging;
            }

            set
            {
                this.markedForDragging = value;
                if (value)
                {
                    this.MarkedForDraggingEvent?.Invoke(this, null);
                }
                else
                {
                    this.UnmarkedForDraggingEvent?.Invoke(this, null);
                }
            }
        }

        /// <summary>
        /// Gets input edges.
        /// </summary>
        public IEnumerable<IViewerEdge> InEdges
        {
            get { return this.Node.InEdges.Select(e => this.funcFromDrawingEdgeToVEdge(e)); }
        }

        /// <summary>
        /// Gets output edges.
        /// </summary>
        public IEnumerable<IViewerEdge> OutEdges
        {
            get { return this.Node.OutEdges.Select(e => this.funcFromDrawingEdgeToVEdge(e)); }
        }

        /// <summary>
        /// Gets edges looping back to self.
        /// </summary>
        public IEnumerable<IViewerEdge> SelfEdges
        {
            get { return this.Node.SelfEdges.Select(e => this.funcFromDrawingEdgeToVEdge(e)); }
        }

        /// <summary>
        /// Gets or sets underlying framework element.
        /// </summary>
        internal FrameworkElement FrameworkElementOfNodeForLabel { get; set; }

        /// <summary>
        /// Gets or sets boundary path.
        /// </summary>
        internal Path BoundaryPath { get; set; }

        /// <summary>
        /// Gets z-index.
        /// </summary>
        internal int ZIndex
        {
            get
            {
                var geomNode = this.Node.GeometryNode;
                if (geomNode == null)
                {
                    return 0;
                }

                int ret = 0;
                do
                {
                    if (geomNode.ClusterParents == null)
                    {
                        return ret;
                    }

                    geomNode = geomNode.ClusterParents.FirstOrDefault();
                    if (geomNode != null)
                    {
                        ret++;
                    }
                    else
                    {
                        return ret;
                    }
                }
                while (true);
            }
        }

        /// <summary>
        /// Gets underlying framework elements.
        /// </summary>
        internal IEnumerable<FrameworkElement> FrameworkElements
        {
            get
            {
                if (this.FrameworkElementOfNodeForLabel != null)
                {
                    yield return this.FrameworkElementOfNodeForLabel;
                }

                if (this.BoundaryPath != null)
                {
                    yield return this.BoundaryPath;
                }

                if (this.collapseButtonBorder != null)
                {
                    yield return this.collapseButtonBorder;
                    yield return this.topMarginRect;
                    yield return this.collapseSymbolPath;
                }
            }
        }

        /// <summary>
        /// Gets path stroke thickness.
        /// </summary>
        private double PathStrokeThickness
        {
            get { return this.pathStrokeThicknessFunc != null ? this.pathStrokeThicknessFunc() : this.Node.Attr.LineWidth; }
        }

        /// <summary>
        /// Get label text and optional tooltip from string in the form "label|tooltip".
        /// </summary>
        /// <param name="text">Text value from which to extract label and tooltip text.</param>
        /// <returns>Label and tooltip text.</returns>
        public static (string, string) GetLabelTextAndToolTip(string text)
        {
            if (text == null)
            {
                return (string.Empty, string.Empty);
            }

            var parts = text.Split('|');
            return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        }

        /// <summary>
        /// Invalidate rendered node.
        /// </summary>
        public void Invalidate()
        {
            if (!this.Node.IsVisible)
            {
                foreach (var fe in this.FrameworkElements)
                {
                    fe.Visibility = Visibility.Hidden;
                }

                return;
            }

            this.BoundaryPath.Data = this.CreatePathFromNodeBoundary();

            Common.PositionFrameworkElement(this.FrameworkElementOfNodeForLabel, this.Node.BoundingBox.Center, 1);

            this.SetFillAndStroke();
            if (this.subgraph == null)
            {
                return;
            }

            this.PositionTopMarginBorder((Cluster)this.subgraph.GeometryNode);
            double collapseBorderSize = this.GetCollapseBorderSymbolSize();
            var collapseButtonCenter = this.GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(this.collapseButtonBorder, collapseButtonCenter, 1);
            double w = collapseBorderSize * 0.4;
            this.collapseSymbolPath.Data = this.CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w / 2), w);
            this.collapseSymbolPath.RenderTransform = ((Cluster)this.subgraph.GeometryNode).IsCollapsed ? new RotateTransform(180, collapseButtonCenter.X, collapseButtonCenter.Y) : null;
            this.topMarginRect.Visibility =
                this.collapseSymbolPath.Visibility =
                    this.collapseButtonBorder.Visibility = Visibility.Visible;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Node.Id;
        }

        /// <summary>
        /// Create node boundary path.
        /// </summary>
        internal void CreateNodeBoundaryPath()
        {
            if (this.FrameworkElementOfNodeForLabel != null)
            {
                // FrameworkElementOfNode.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var center = this.Node.GeometryNode.Center;
                var margin = 2 * this.Node.Attr.LabelMargin;
                var bc = NodeBoundaryCurves.GetNodeBoundaryCurve(this.Node, this.FrameworkElementOfNodeForLabel.Width + margin, this.FrameworkElementOfNodeForLabel.Height + margin);
                bc.Translate(center);
            }

            this.BoundaryPath = new Path { Data = this.CreatePathFromNodeBoundary(), Tag = this };
            Panel.SetZIndex(this.BoundaryPath, this.ZIndex);
            this.SetFillAndStroke();
            if (this.Node.Label != null)
            {
                var (_, tooltip) = GetLabelTextAndToolTip(this.Node.LabelText);
                this.BoundaryPath.ToolTip = tooltip;
                if (this.FrameworkElementOfNodeForLabel != null)
                {
                    this.FrameworkElementOfNodeForLabel.ToolTip = tooltip;
                }
            }
        }

        /// <summary>
        /// Detach node from canvas.
        /// </summary>
        /// <param name="graphCanvas">Graph canvas.</param>
        internal void DetatchFromCanvas(Canvas graphCanvas)
        {
            if (this.BoundaryPath != null)
            {
                graphCanvas.Children.Remove(this.BoundaryPath);
            }

            if (this.FrameworkElementOfNodeForLabel != null)
            {
                graphCanvas.Children.Remove(this.FrameworkElementOfNodeForLabel);
            }
        }

        private static void AddCurve(PathFigure pathFigure, Curve curve)
        {
            foreach (ICurve seg in curve.Segments)
            {
                var ls = seg as LineSegment;
                if (ls != null)
                {
                    pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(ls.End), true));
                }
                else
                {
                    var ellipse = seg as Ellipse;
                    if (ellipse != null)
                    {
                        pathFigure.Segments.Add(
                            new ArcSegment(
                                Common.WpfPoint(ellipse.End),
                                new Size(ellipse.AxisA.Length, ellipse.AxisB.Length),
                                Point.Angle(new Point(1, 0), ellipse.AxisA),
                                ellipse.ParEnd - ellipse.ParEnd >= Math.PI,
                                !ellipse.OrientedCounterclockwise() ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                                true));
                    }
                }
            }
        }

        private void SetupSubgraphDrawing()
        {
            if (this.subgraph == null)
            {
                return;
            }

            this.SetupTopMarginBorder();
            this.SetupCollapseSymbol();
        }

        private void SetupTopMarginBorder()
        {
            var cluster = (Cluster)this.subgraph.GeometryObject;
            this.topMarginRect = new Rectangle
            {
                Fill = Brushes.Transparent,
                Width = this.Node.Width,
                Height = cluster.RectangularBoundary.TopMargin,
            };
            this.PositionTopMarginBorder(cluster);
            this.SetZIndexAndMouseInteractionsForTopMarginRect();
        }

        private void PositionTopMarginBorder(Cluster cluster)
        {
            var box = cluster.BoundaryCurve.BoundingBox;

            Common.PositionFrameworkElement(this.topMarginRect, box.LeftTop + new Point(this.topMarginRect.Width / 2, -this.topMarginRect.Height / 2), 1);
        }

        private void SetZIndexAndMouseInteractionsForTopMarginRect()
        {
            this.topMarginRect.MouseEnter += (a, b) =>
            {
                this.collapseButtonBorder.Background = Common.BrushFromMsaglColor(this.subgraph.CollapseButtonColorActive);
                this.collapseSymbolPath.Stroke = Brushes.Black;
            };

            this.topMarginRect.MouseLeave +=
                (a, b) =>
                {
                    this.collapseButtonBorder.Background = Common.BrushFromMsaglColor(this.subgraph.CollapseButtonColorInactive);
                    this.collapseSymbolPath.Stroke = Brushes.Silver;
                };
            Panel.SetZIndex(this.topMarginRect, int.MaxValue);
        }

        private void SetupCollapseSymbol()
        {
            var collapseBorderSize = this.GetCollapseBorderSymbolSize();
            this.collapseButtonBorder = new Border
            {
                Background = Common.BrushFromMsaglColor(this.subgraph.CollapseButtonColorInactive),
                Width = collapseBorderSize,
                Height = collapseBorderSize,
                CornerRadius = new CornerRadius(collapseBorderSize / 2),
            };

            Panel.SetZIndex(this.collapseButtonBorder, Panel.GetZIndex(this.BoundaryPath) + 1);

            var collapseButtonCenter = this.GetCollapseButtonCenter(collapseBorderSize);
            Common.PositionFrameworkElement(this.collapseButtonBorder, collapseButtonCenter, 1);

            double w = collapseBorderSize * 0.4;
            this.collapseSymbolPath = new Path
            {
                Data = this.CreateCollapseSymbolPath(collapseButtonCenter + new Point(0, -w / 2), w),
                Stroke = this.collapseSymbolPathInactive,
                StrokeThickness = 1,
            };

            Panel.SetZIndex(this.collapseSymbolPath, Panel.GetZIndex(this.collapseButtonBorder) + 1);
            this.topMarginRect.MouseLeftButtonDown += this.TopMarginRectMouseLeftButtonDown;
        }

        private void InvokeIsCollapsedChanged()
        {
            if (this.IsCollapsedChanged != null)
            {
                this.IsCollapsedChanged(this);
            }
        }

        private void TopMarginRectMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this.collapseButtonBorder);
            if (pos.X <= this.collapseButtonBorder.Width && pos.Y <= this.collapseButtonBorder.Height && pos.X >= 0 &&
                pos.Y >= 0)
            {
                e.Handled = true;
                var cluster = (Cluster)this.subgraph.GeometryNode;
                cluster.IsCollapsed = !cluster.IsCollapsed;
                this.InvokeIsCollapsedChanged();
            }
        }

        private double GetCollapseBorderSymbolSize()
        {
            return ((Cluster)this.subgraph.GeometryNode).RectangularBoundary.TopMargin -
                   this.PathStrokeThickness / 2 - 0.5;
        }

        private Point GetCollapseButtonCenter(double collapseBorderSize)
        {
            var box = this.subgraph.GeometryNode.BoundaryCurve.BoundingBox;

            // cannot trust subgraph.GeometryNode.BoundingBox for a cluster
            double offsetFromBoundaryPath = this.PathStrokeThickness / 2 + 0.5;
            var collapseButtonCenter = box.LeftTop + new Point(collapseBorderSize / 2 + offsetFromBoundaryPath, -collapseBorderSize / 2 - offsetFromBoundaryPath);
            return collapseButtonCenter;
        }

        private Geometry CreateCollapseSymbolPath(Point center, double width)
        {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = Common.WpfPoint(center + new Point(-width, width)) };

            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(center), true));
            pathFigure.Segments.Add(
                new System.Windows.Media.LineSegment(Common.WpfPoint(center + new Point(width, width)), true));

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        private byte GetTransparency(byte t)
        {
            return t;
        }

        private void SetFillAndStroke()
        {
            byte trasparency = this.GetTransparency(this.Node.Attr.Color.A);
            this.BoundaryPath.Stroke = Common.BrushFromMsaglColor(new Msagl.Drawing.Color(trasparency, this.Node.Attr.Color.R, this.Node.Attr.Color.G, this.Node.Attr.Color.B));
            this.SetBoundaryFill();
            this.BoundaryPath.StrokeThickness = this.PathStrokeThickness;

            var textBlock = this.FrameworkElementOfNodeForLabel as TextBlock;
            if (textBlock != null)
            {
                var col = this.Node.Label.FontColor;
                textBlock.Foreground =
                    Common.BrushFromMsaglColor(new Msagl.Drawing.Color(this.GetTransparency(col.A), col.R, col.G, col.B));
            }
        }

        private void SetBoundaryFill()
        {
            this.BoundaryPath.Fill = Common.BrushFromMsaglColor(this.Node.Attr.FillColor);
        }

        private Geometry DoubleCircle()
        {
            var box = this.Node.BoundingBox;
            double w = box.Width;
            double h = box.Height;
            var pathGeometry = new PathGeometry();
            var r = new Rect(box.Left, box.Bottom, w, h);
            pathGeometry.AddGeometry(new EllipseGeometry(r));
            var inflation = Math.Min(5.0, Math.Min(w / 3, h / 3));
            r.Inflate(-inflation, -inflation);
            pathGeometry.AddGeometry(new EllipseGeometry(r));
            return pathGeometry;
        }

        private Geometry CreatePathFromNodeBoundary()
        {
            Geometry geometry;
            switch (this.Node.Attr.Shape)
            {
                case Shape.Box:
                case Shape.House:
                case Shape.InvHouse:
                case Shape.Diamond:
                case Shape.Octagon:
                case Shape.Hexagon:

                    geometry = this.CreateGeometryFromMsaglCurve(this.Node.GeometryNode.BoundaryCurve);
                    break;

                case Shape.DoubleCircle:
                    geometry = this.DoubleCircle();
                    break;

                default:
                    geometry = this.GetEllipseGeometry();
                    break;
            }

            return geometry;
        }

        private Geometry CreateGeometryFromMsaglCurve(ICurve curveGeometry)
        {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                IsClosed = true,
                IsFilled = true,
                StartPoint = Common.WpfPoint(curveGeometry.Start),
            };

            var curve = curveGeometry as Curve;
            if (curve != null)
            {
                AddCurve(pathFigure, curve);
            }
            else
            {
                var rect = curveGeometry as RoundedRect;
                if (rect != null)
                {
                    AddCurve(pathFigure, rect.Curve);
                }
                else
                {
                    var ellipse = curveGeometry as Ellipse;
                    if (ellipse != null)
                    {
                        return new EllipseGeometry(Common.WpfPoint(ellipse.Center), ellipse.AxisA.Length, ellipse.AxisB.Length);
                    }

                    var poly = curveGeometry as Polyline;
                    if (poly != null)
                    {
                        var p = poly.StartPoint.Next;
                        do
                        {
                            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(Common.WpfPoint(p.Point), true));
                            p = p.NextOnPolyline;
                        }
                        while (p != poly.StartPoint);
                    }
                }
            }

            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }

        private Geometry GetEllipseGeometry()
        {
            return new EllipseGeometry(Common.WpfPoint(this.Node.BoundingBox.Center), this.Node.BoundingBox.Width / 2, this.Node.BoundingBox.Height / 2);
        }
    }
}