// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Msagl.Core.Geometry.Curves;
    using Microsoft.Msagl.Core.Layout;
    using Microsoft.Msagl.Drawing;
    using Microsoft.Msagl.Layout.LargeGraphLayout;
    using Edge = Microsoft.Msagl.Drawing.Edge;
    using Ellipse = Microsoft.Msagl.Core.Geometry.Curves.Ellipse;
    using LineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
    using Point = Microsoft.Msagl.Core.Geometry.Point;
    using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
    using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
    using Size = System.Windows.Size;

    /// <summary>
    /// Visual edge.
    /// </summary>
    public class VEdge : IViewerEdge, IInvalidatable
    {
        private const double ArrowAngle = 30.0; // degrees

        private static readonly double HalfArrowAngleTan = Math.Tan(ArrowAngle * 0.5 * Math.PI / 180.0);
        private static readonly double HalfArrowAngleCos = Math.Cos(ArrowAngle * 0.5 * Math.PI / 180.0);

        private static double dashSize = 0.05; // inches

        private bool markedForDragging = false;
        private FrameworkElement labelFrameworkElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="VEdge"/> class.
        /// </summary>
        /// <param name="edge">Underlying edge.</param>
        /// <param name="labelFrameworkElement">Underlying label framework element.</param>
        public VEdge(Edge edge, FrameworkElement labelFrameworkElement)
        {
            this.Edge = edge;
            this.CurvePath = new Path
            {
                Data = GetICurveWpfGeometry(edge.GeometryEdge.Curve),
                Tag = this,
            };

            this.EdgeAttrClone = edge.Attr.Clone();

            if (edge.Attr.ArrowAtSource)
            {
                this.SourceArrowHeadPath = new Path
                {
                    Data = this.DefiningSourceArrowHead(),
                    Tag = this,
                };
            }

            if (edge.Attr.ArrowAtTarget)
            {
                this.TargetArrowHeadPath = new Path
                {
                    Data = DefiningTargetArrowHead(this.Edge.GeometryEdge.EdgeGeometry, this.PathStrokeThickness),
                    Tag = this,
                };
            }

            this.SetPathStroke();

            if (labelFrameworkElement != null)
            {
                this.labelFrameworkElement = labelFrameworkElement;
                Common.PositionFrameworkElement(this.labelFrameworkElement, edge.Label.Center, 1);
            }

            edge.Attr.VisualsChanged += (a, b) => this.Invalidate();

            edge.IsVisibleChanged += obj =>
            {
                foreach (var frameworkElement in this.FrameworkElements)
                {
                    frameworkElement.Visibility = edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
                }
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VEdge"/> class.
        /// </summary>
        /// <param name="edge">Underlying edge.</param>
        /// <param name="layoutSettings">Layout settings.</param>
        public VEdge(Edge edge, LgLayoutSettings layoutSettings)
        {
            this.Edge = edge;
            this.EdgeAttrClone = edge.Attr.Clone();
        }

        /// <inheritdoc/>
        public event EventHandler MarkedForDraggingEvent;

        /// <inheritdoc/>
        public event EventHandler UnmarkedForDraggingEvent;

        /// <summary>
        /// Gets or sets a value indicating whether marked for dragging.
        /// </summary>
        public bool MarkedForDragging
        {
            get { return this.markedForDragging; }

            set
            {
                if (value != this.markedForDragging)
                {
                    this.markedForDragging = value;

                    if (value && this.MarkedForDraggingEvent != null)
                    {
                        this.MarkedForDraggingEvent(this, EventArgs.Empty);
                    }
                    else if (!value && this.UnmarkedForDraggingEvent != null)
                    {
                        this.UnmarkedForDraggingEvent(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Gets underlying edge.
        /// </summary>
        public Edge Edge { get; private set; }

        /// <summary>
        /// Gets source node.
        /// </summary>
        public IViewerNode Source { get; private set; }

        /// <summary>
        /// Gets target node.
        /// </summary>
        public IViewerNode Target { get; private set; }

        /// <summary>
        /// Gets or sets the radius of the polyline corner.
        /// </summary>
        public double RadiusOfPolylineCorner { get; set; }

        /// <summary>
        /// Gets underlying drawing object.
        /// </summary>
        public DrawingObject DrawingObject
        {
            get { return this.Edge; }
        }

        /// <summary>
        /// Gets or sets corresponding visual label.
        /// </summary>
        internal VLabel VLabel { get; set; }

        /// <summary>
        /// Gets or sets function returning path stroke thickness.
        /// </summary>
        internal Func<double> PathStrokeThicknessFunc { get; set; }

        /// <summary>
        /// Gets underlying framework element.
        /// </summary>
        internal IEnumerable<FrameworkElement> FrameworkElements
        {
            get
            {
                if (this.SourceArrowHeadPath != null)
                {
                    yield return this.SourceArrowHeadPath;
                }

                if (this.TargetArrowHeadPath != null)
                {
                    yield return this.TargetArrowHeadPath;
                }

                if (this.CurvePath != null)
                {
                    yield return this.CurvePath;
                }

                if (this.labelFrameworkElement != null)
                {
                    yield return this.labelFrameworkElement;
                }
            }
        }

        /// <summary>
        /// Gets or sets curve path.
        /// </summary>
        internal Path CurvePath { get; set; }

        /// <summary>
        /// Gets or sets source arrow head path.
        /// </summary>
        internal Path SourceArrowHeadPath { get; set; }

        /// <summary>
        /// Gets or sets target arrow head path.
        /// </summary>
        internal Path TargetArrowHeadPath { get; set; }

        /// <summary>
        /// Gets or sets edge attibutes (clone).
        /// </summary>
        internal EdgeAttr EdgeAttrClone { get; set; }

        private double PathStrokeThickness
        {
            get
            {
                return this.PathStrokeThicknessFunc != null ? this.PathStrokeThicknessFunc() : this.Edge.Attr.LineWidth;
            }
        }

        /// <summary>
        /// Invalidate rendered edge.
        /// </summary>
        public void Invalidate()
        {
            var vis = this.Edge.IsVisible ? Visibility.Visible : Visibility.Hidden;
            foreach (var fe in this.FrameworkElements)
            {
                fe.Visibility = vis;
            }

            if (vis == Visibility.Hidden)
            {
                return;
            }

            this.CurvePath.Data = GetICurveWpfGeometry(this.Edge.GeometryEdge.Curve);
            if (this.Edge.Attr.ArrowAtSource)
            {
                this.SourceArrowHeadPath.Data = this.DefiningSourceArrowHead();
            }

            if (this.Edge.Attr.ArrowAtTarget)
            {
                this.TargetArrowHeadPath.Data = DefiningTargetArrowHead(this.Edge.GeometryEdge.EdgeGeometry, this.PathStrokeThickness);
            }

            this.SetPathStroke();
            if (this.VLabel != null)
            {
                ((IInvalidatable)this.VLabel).Invalidate();
            }
        }

        /// <summary>
        /// Create framework element for rail.
        /// </summary>
        /// <param name="rail">Rail for which to create element.</param>
        /// <param name="edgeTransparency">Edge transparency.</param>
        /// <returns>Framework element.</returns>
        public FrameworkElement CreateFrameworkElementForRail(Rail rail, byte edgeTransparency)
        {
            var curve = rail.Geometry as ICurve;
            Path fe;
            if (curve != null)
            {
                fe = (Path)this.CreateFrameworkElementForRailCurve(rail, curve, edgeTransparency);
            }
            else
            {
                var arrowhead = rail.Geometry as Arrowhead;
                if (arrowhead != null)
                {
                    fe = (Path)this.CreateFrameworkElementForRailArrowhead(rail, arrowhead, rail.CurveAttachmentPoint, edgeTransparency);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            fe.Tag = rail;
            return fe;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Edge.ToString();
        }

        /// <summary>
        /// Generate geometry of defining target arrow head.
        /// </summary>
        /// <param name="edgeGeometry">Edge geometry.</param>
        /// <param name="thickness">Edge thickness.</param>
        /// <returns>Geometry.</returns>
        internal static Geometry DefiningTargetArrowHead(EdgeGeometry edgeGeometry, double thickness)
        {
            if (edgeGeometry.TargetArrowhead == null || edgeGeometry.Curve == null)
            {
                return null;
            }

            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, edgeGeometry.Curve.End, edgeGeometry.TargetArrowhead.TipPosition, thickness);
                return streamGeometry;
            }
        }

        /// <summary>
        /// Fill context for curve.
        /// </summary>
        /// <param name="context">Stream geometry context.</param>
        /// <param name="curve">Curve.</param>
        internal static void FillContextForICurve(StreamGeometryContext context, ICurve curve)
        {
            context.BeginFigure(Common.WpfPoint(curve.Start), false, false);

            var c = curve as Curve;
            if (c != null)
            {
                FillContexForCurve(context, c);
            }
            else
            {
                var cubicBezierSeg = curve as CubicBezierSegment;
                if (cubicBezierSeg != null)
                {
                    context.BezierTo(Common.WpfPoint(cubicBezierSeg.B(1)), Common.WpfPoint(cubicBezierSeg.B(2)), Common.WpfPoint(cubicBezierSeg.B(3)), true, false);
                }
                else
                {
                    var ls = curve as LineSegment;
                    if (ls != null)
                    {
                        context.LineTo(Common.WpfPoint(ls.End), true, false);
                    }
                    else
                    {
                        var rr = curve as RoundedRect;
                        if (rr != null)
                        {
                            FillContexForCurve(context, rr.Curve);
                        }
                        else
                        {
                            var poly = curve as Polyline;
                            if (poly != null)
                            {
                                FillContexForPolyline(context, poly);
                            }
                            else
                            {
                                var ellipse = curve as Ellipse;
                                if (ellipse != null)
                                {
                                    double sweepAngle = EllipseSweepAngle(ellipse);
                                    bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                                    Rectangle box = ellipse.FullBox();
                                    context.ArcTo(
                                        Common.WpfPoint(ellipse.End),
                                        new Size(box.Width / 2, box.Height / 2),
                                        sweepAngle,
                                        largeArc,
                                        sweepAngle < 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                                        true,
                                        true);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get WPF curve geometry.
        /// </summary>
        /// <param name="curve">Curve for which to get geometry.</param>
        /// <returns>Curve geometry.</returns>
        internal static Geometry GetICurveWpfGeometry(ICurve curve)
        {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                FillStreamGeometryContext(context, curve);
                return streamGeometry;
            }
        }

        /// <summary>
        /// Invalidate rendered edge.
        /// </summary>
        /// <param name="fe">Framework element.</param>
        /// <param name="rail">Rail.</param>
        /// <param name="edgeTransparency">Edge transparency.</param>
        internal void Invalidate(FrameworkElement fe, Rail rail, byte edgeTransparency)
        {
            var path = fe as Path;
            if (path != null)
            {
                this.SetPathStrokeToRailPath(rail, path, edgeTransparency);
            }
        }

        /// <summary>
        /// Get dash size.
        /// </summary>
        /// <returns>Dash size.</returns>
        internal double DashSize()
        {
            var w = this.PathStrokeThickness;
            var dashSizeInPoints = dashSize * GraphViewer.DpiXStatic;
            return dashSizeInPoints / w;
        }

        /// <summary>
        /// Remove this edge from canvas.
        /// </summary>
        /// <param name="graphCanvas">Canvas from which to remove.</param>
        internal void RemoveItselfFromCanvas(Canvas graphCanvas)
        {
            if (this.CurvePath != null)
            {
                graphCanvas.Children.Remove(this.CurvePath);
            }

            if (this.SourceArrowHeadPath != null)
            {
                graphCanvas.Children.Remove(this.SourceArrowHeadPath);
            }

            if (this.TargetArrowHeadPath != null)
            {
                graphCanvas.Children.Remove(this.TargetArrowHeadPath);
            }

            if (this.VLabel != null)
            {
                graphCanvas.Children.Remove(this.VLabel.FrameworkElement );
            }
        }

        private static void FillStreamGeometryContext(StreamGeometryContext context, ICurve curve)
        {
            if (curve == null)
            {
                return;
            }

            FillContextForICurve(context, curve);
        }

        private static void FillContexForPolyline(StreamGeometryContext context, Polyline poly)
        {
            for (PolylinePoint pp = poly.StartPoint.Next; pp != null; pp = pp.Next)
            {
                context.LineTo(Common.WpfPoint(pp.Point), true, false);
            }
        }

        private static void FillContexForCurve(StreamGeometryContext context, Curve c)
        {
            foreach (ICurve seg in c.Segments)
            {
                var bezSeg = seg as CubicBezierSegment;
                if (bezSeg != null)
                {
                    context.BezierTo(Common.WpfPoint(bezSeg.B(1)), Common.WpfPoint(bezSeg.B(2)), Common.WpfPoint(bezSeg.B(3)), true, false);
                }
                else
                {
                    var ls = seg as LineSegment;
                    if (ls != null)
                    {
                        context.LineTo(Common.WpfPoint(ls.End), true, false);
                    }
                    else
                    {
                        var ellipse = seg as Ellipse;
                        if (ellipse != null)
                        {
                            double sweepAngle = EllipseSweepAngle(ellipse);
                            bool largeArc = Math.Abs(sweepAngle) >= Math.PI;
                            Rectangle box = ellipse.FullBox();
                            context.ArcTo(
                                Common.WpfPoint(ellipse.End),
                                new Size(box.Width / 2, box.Height / 2),
                                sweepAngle,
                                largeArc,
                                sweepAngle < 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                                true,
                                true);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
        }

        private static void AddArrow(StreamGeometryContext context, Point start, Point end, double thickness)
        {
            if (thickness > 1)
            {
                Point dir = end - start;
                Point h = dir;
                double dl = dir.Length;
                if (dl < 0.001)
                {
                    return;
                }

                dir /= dl;

                var s = new Point(-dir.Y, dir.X);
                double w = 0.5 * thickness;
                Point s0 = w * s;
                s *= h.Length * HalfArrowAngleTan;
                s += s0;

                double rad = w / HalfArrowAngleCos;

                context.BeginFigure(Common.WpfPoint(start + s), true, true);
                context.LineTo(Common.WpfPoint(start - s), true, false);
                context.LineTo(Common.WpfPoint(end - s0), true, false);
                context.ArcTo(Common.WpfPoint(end + s0), new Size(rad, rad), Math.PI - ArrowAngle, false, SweepDirection.Clockwise, true, false);
            }
            else
            {
                Point dir = end - start;
                double dl = dir.Length;

                // take into account the widths
                double delta = Math.Min(dl / 2, thickness + thickness / 2);
                dir *= (dl - delta) / dl;
                end = start + dir;
                dir = dir.Rotate(Math.PI / 2);
                Point s = dir * HalfArrowAngleTan;

                context.BeginFigure(Common.WpfPoint(start + s), true, true);
                context.LineTo(Common.WpfPoint(end), true, true);
                context.LineTo(Common.WpfPoint(start - s), true, true);
            }
        }

        private static double EllipseSweepAngle(Ellipse ellipse)
        {
            double sweepAngle = ellipse.ParEnd - ellipse.ParStart;
            return ellipse.OrientedCounterclockwise() ? sweepAngle : -sweepAngle;
        }

        private Geometry DefiningSourceArrowHead()
        {
            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, this.Edge.GeometryEdge.Curve.Start, this.Edge.GeometryEdge.EdgeGeometry.SourceArrowhead.TipPosition, this.PathStrokeThickness);
                return streamGeometry;
            }
        }

        private void SetPathStroke()
        {
            this.SetPathStrokeToPath(this.CurvePath);
            if (this.SourceArrowHeadPath != null)
            {
                this.SourceArrowHeadPath.Stroke = this.SourceArrowHeadPath.Fill = Common.BrushFromMsaglColor(this.Edge.Attr.Color);
                this.SourceArrowHeadPath.StrokeThickness = this.PathStrokeThickness;
            }

            if (this.TargetArrowHeadPath != null)
            {
                this.TargetArrowHeadPath.Stroke = this.TargetArrowHeadPath.Fill = Common.BrushFromMsaglColor(this.Edge.Attr.Color);
                this.TargetArrowHeadPath.StrokeThickness = this.PathStrokeThickness;
            }
        }

        private void SetPathStrokeToRailPath(Rail rail, Path path, byte transparency)
        {
            path.Stroke = this.SetStrokeColorForRail(transparency, rail);
            path.StrokeThickness = this.PathStrokeThickness;

            foreach (var style in this.Edge.Attr.Styles)
            {
                if (style == Msagl.Drawing.Style.Dotted)
                {
                    path.StrokeDashArray = new DoubleCollection { 1, 1 };
                }
                else if (style == Msagl.Drawing.Style.Dashed)
                {
                    var f = this.DashSize();
                    path.StrokeDashArray = new DoubleCollection { f, f };
                }
            }
        }

        private Brush SetStrokeColorForRail(byte transparency, Rail rail)
        {
            return
                rail.IsHighlighted == false ?
                new SolidColorBrush(new System.Windows.Media.Color
                {
                   A = transparency,
                   R = this.Edge.Attr.Color.R,
                   G = this.Edge.Attr.Color.G,
                   B = this.Edge.Attr.Color.B,
                }) :
                Brushes.Red;
        }

        private void SetPathStrokeToPath(Path path)
        {
            path.Stroke = Common.BrushFromMsaglColor(this.Edge.Attr.Color);
            path.StrokeThickness = this.PathStrokeThickness;

            foreach (var style in this.Edge.Attr.Styles)
            {
                if (style == Msagl.Drawing.Style.Dotted)
                {
                    path.StrokeDashArray = new DoubleCollection { 1, 1 };
                }
                else if (style == Msagl.Drawing.Style.Dashed)
                {
                    var f = this.DashSize();
                    path.StrokeDashArray = new DoubleCollection { f, f };
                }
            }
        }

        private FrameworkElement CreateFrameworkElementForRailArrowhead(Rail rail, Arrowhead arrowhead, Point curveAttachmentPoint, byte edgeTransparency)
        {
            var streamGeometry = new StreamGeometry();

            using (StreamGeometryContext context = streamGeometry.Open())
            {
                AddArrow(context, curveAttachmentPoint, arrowhead.TipPosition, this.PathStrokeThickness);
            }

            var path = new Path
            {
                Data = streamGeometry,
                Tag = this,
            };

            this.SetPathStrokeToRailPath(rail, path, edgeTransparency);
            return path;
        }

        private FrameworkElement CreateFrameworkElementForRailCurve(Rail rail, ICurve curve, byte transparency)
        {
            var path = new Path
            {
                Data = GetICurveWpfGeometry(curve),
            };

            this.SetPathStrokeToRailPath(rail, path, transparency);
            return path;
        }
    }
}