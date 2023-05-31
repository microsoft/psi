// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for Point3D graphs.
    /// </summary>
    /// <typeparam name="TKey">The type of the key for each point 3D graph.</typeparam>
    [VisualizationObject("Point3D graph")]
    public class Point3DGraphVisualizationObject<TKey> : ModelVisual3DGraphVisualizationObject<SphereVisual3D, PipeVisual3D, TKey, Point3D, bool>
    {
        private Color edgeColor = Colors.White;
        private double edgeDiameterMm = 20;

        private Color nodeColor = Colors.White;
        private double nodeRadiusMm = 15;
        private int polygonResolution = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3DGraphVisualizationObject{TKey}"/> class.
        /// </summary>
        /// <param name="nodeVisibilityFunc">An optional function that computes node visibility.</param>
        /// <param name="nodeFillFunc">An optional function that computes the node fill brush.</param>
        /// <param name="edgeVisibilityFunc">An optional function that computes edge visibility.</param>
        /// <param name="edgeFillFunc">An optional function that computes the edge fill brush.</param>
        public Point3DGraphVisualizationObject(
            Func<TKey, bool> nodeVisibilityFunc = null,
            Func<TKey, Brush> nodeFillFunc = null,
            Func<(TKey, TKey), bool> edgeVisibilityFunc = null,
            Func<(TKey, TKey), Brush> edgeFillFunc = null)
            : base(nodeVisibilityFunc, edgeVisibilityFunc)
        {
            this.NodeBrushFunc = nodeFillFunc ?? (_ => new SolidColorBrush(this.NodeColor));
            this.EdgeBrushFunc = edgeFillFunc ?? (_ => new SolidColorBrush(this.EdgeColor));
        }

        /// <summary>
        /// Gets or sets the edge color.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Color")]
        [Description("Color of the edges.")]
        public virtual Color EdgeColor
        {
            get { return this.edgeColor; }
            set { this.Set(nameof(this.EdgeColor), ref this.edgeColor, value); }
        }

        /// <summary>
        /// Gets or sets the edge diameter.
        /// </summary>
        [DataMember]
        [DisplayName("Edge diameter (mm)")]
        [Description("Diameter of edges (mm).")]
        public virtual double EdgeDiameterMm
        {
            get { return this.edgeDiameterMm; }
            set { this.Set(nameof(this.EdgeDiameterMm), ref this.edgeDiameterMm, value); }
        }

        /// <summary>
        /// Gets or sets the node color.
        /// </summary>
        [DataMember]
        [DisplayName("Node Color")]
        [Description("Color of the nodes.")]
        public virtual Color NodeColor
        {
            get { return this.nodeColor; }
            set { this.Set(nameof(this.NodeColor), ref this.nodeColor, value); }
        }

        /// <summary>
        /// Gets or sets the node radius.
        /// </summary>
        [DataMember]
        [DisplayName("Node radius (mm)")]
        [Description("Radius of nodes (mm).")]
        public virtual double NodeRadiusMm
        {
            get { return this.nodeRadiusMm; }
            set { this.Set(nameof(this.NodeRadiusMm), ref this.nodeRadiusMm, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering polygons for nodes and edges.
        /// </summary>
        [DataMember]
        [Description("Level of resolution at which to render node and edge polygons (minimum value is 3).")]
        public int PolygonResolution
        {
            get { return this.polygonResolution; }
            set { this.Set(nameof(this.PolygonResolution), ref this.polygonResolution, value < 3 ? 3 : value); }
        }

        /// <summary>
        /// Gets the node brush function.
        /// </summary>
        protected Func<TKey, Brush> NodeBrushFunc { get; }

        /// <summary>
        /// Gets the edge brush function.
        /// </summary>
        protected Func<(TKey, TKey), Brush> EdgeBrushFunc { get; }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.EdgeColor) ||
                propertyName == nameof(this.EdgeDiameterMm) ||
                propertyName == nameof(this.NodeColor) ||
                propertyName == nameof(this.NodeRadiusMm) ||
                propertyName == nameof(this.PolygonResolution))
            {
                this.UpdateVisuals();
            }
            else
            {
                base.NotifyPropertyChanged(propertyName);
            }
        }

        /// <inheritdoc/>
        protected override void UpdateNodeVisuals(SphereVisual3D sphereVisual3D, TKey nodeKey)
        {
            sphereVisual3D.BeginEdit();

            if (this.NodeVisibilityFunc(nodeKey))
            {
                var nodePosition = this.CurrentData.Nodes[nodeKey];

                if (sphereVisual3D.Radius != this.NodeRadiusMm / 1000.0)
                {
                    sphereVisual3D.Radius = this.NodeRadiusMm / 1000.0;
                }

                var nodeFill = this.NodeBrushFunc(nodeKey);

                if (sphereVisual3D.Fill != nodeFill)
                {
                    sphereVisual3D.Fill = nodeFill;
                }

                sphereVisual3D.Transform = new Win3D.TranslateTransform3D(nodePosition.X, nodePosition.Y, nodePosition.Z);

                sphereVisual3D.PhiDiv = this.PolygonResolution;
                sphereVisual3D.ThetaDiv = this.PolygonResolution;

                sphereVisual3D.Visible = true;
            }
            else
            {
                sphereVisual3D.Visible = false;
            }

            sphereVisual3D.EndEdit();
        }

        /// <inheritdoc/>
        protected override void UpdateEdgeVisuals(PipeVisual3D pipeVisual3D, (TKey Start, TKey End) edge)
        {
            pipeVisual3D.BeginEdit();

            if (this.EdgeVisibilityFunc(edge) &&
                this.NodesVisuals.TryGetVisual(edge.Start, out var startNode) &&
                this.NodesVisuals.TryGetVisual(edge.End, out var endNode))
            {
                if (pipeVisual3D.Diameter != this.EdgeDiameterMm / 1000.0)
                {
                    pipeVisual3D.Diameter = this.EdgeDiameterMm / 1000.0;
                }

                var node1Position = startNode.Transform.Value;
                var node2Position = endNode.Transform.Value;

                pipeVisual3D.Point1 = new Win3D.Point3D(node1Position.OffsetX, node1Position.OffsetY, node1Position.OffsetZ);
                pipeVisual3D.Point2 = new Win3D.Point3D(node2Position.OffsetX, node2Position.OffsetY, node2Position.OffsetZ);

                var edgeFill = this.EdgeBrushFunc(edge);
                if (pipeVisual3D.Fill != edgeFill)
                {
                    pipeVisual3D.Fill = edgeFill;
                }

                pipeVisual3D.ThetaDiv = this.PolygonResolution;

                pipeVisual3D.Visible = true;
            }
            else
            {
                pipeVisual3D.Visible = false;
            }

            pipeVisual3D.EndEdit();
        }
    }
}