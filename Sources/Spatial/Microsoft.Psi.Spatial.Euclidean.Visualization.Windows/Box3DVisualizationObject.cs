// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a 3D box visualization object.
    /// </summary>
    [VisualizationObject("3D box")]
    public class Box3DVisualizationObject : ModelVisual3DVisualizationObject<Box3D>
    {
        // Edge properties
        private Color edgeColor = Colors.White;
        private double edgeThicknessMm = 3;
        private double edgeOpacity = 100;
        private bool edgeVisible = true;
        private int pipeDiv = 7;

        // Facet properties
        private Color facetColor = Colors.DodgerBlue;
        private double facetOpacity = 10;
        private bool facetVisible = true;

        // The edges and facets that make up the 3D rectangle
        private PipeVisual3D[] edges;
        private MeshGeometryVisual3D[] facets;

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3DVisualizationObject"/> class.
        /// </summary>
        public Box3DVisualizationObject()
        {
            this.CreateBoxEdges();
            this.CreateBoxFacets();
            this.UpdateFacetContents();

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Color")]
        [Description("The color of the box edges.")]
        public Color EdgeColor
        {
            get { return this.edgeColor; }
            set { this.Set(nameof(this.EdgeColor), ref this.edgeColor, value); }
        }

        /// <summary>
        /// Gets or sets the edge thickness in millimeters.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Thickness (mm)")]
        [Description("The thickness of the box edges in millimeters.")]
        public double EdgeThicknessMm
        {
            get { return this.edgeThicknessMm; }
            set { this.Set(nameof(this.EdgeThicknessMm), ref this.edgeThicknessMm, value); }
        }

        /// <summary>
        /// Gets or sets the edge opacity.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Opacity")]
        [Description("The opacity of the box edges.")]
        public double EdgeOpacity
        {
            get { return this.edgeOpacity; }
            set { this.Set(nameof(this.EdgeOpacity), ref this.edgeOpacity, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the edges are displayed.
        /// </summary>
        [DataMember]
        [DisplayName("Edge Visibility")]
        [Description("Indicates whether the edges are displayed or not.")]
        public bool EdgeVisible
        {
            get { return this.edgeVisible; }
            set { this.Set(nameof(this.EdgeVisible), ref this.edgeVisible, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering each edge as a pipe.
        /// </summary>
        [DataMember]
        [DisplayName("Pipe Divisions")]
        [Description("Number of divisions to use when rendering each rectangle edge as a pipe (minimum value is 3).")]
        public int PipeDivisions
        {
            get { return this.pipeDiv; }
            set { this.Set(nameof(this.PipeDivisions), ref this.pipeDiv, value < 3 ? 3 : value); }
        }

        /// <summary>
        /// Gets or sets the facet color.
        /// </summary>
        [DataMember]
        [DisplayName("Facet Color")]
        [Description("The color of the box facets.")]
        public Color FacetColor
        {
            get { return this.facetColor; }
            set { this.Set(nameof(this.FacetColor), ref this.facetColor, value); }
        }

        /// <summary>
        /// Gets or sets the facet opacity.
        /// </summary>
        [DataMember]
        [DisplayName("Facet Opacity")]
        [Description("The opacity of the box facets.")]
        public double FacetOpacity
        {
            get { return this.facetOpacity; }
            set { this.Set(nameof(this.FacetOpacity), ref this.facetOpacity, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the edges are displayed.
        /// </summary>
        [DataMember]
        [DisplayName("Facet Visibility")]
        [Description("Indicates whether the box facets are displayed or not.")]
        public bool FacetVisible
        {
            get { return this.facetVisible; }
            set { this.Set(nameof(this.FacetVisible), ref this.facetVisible, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                // Update the edge locations.
                var box = this.CurrentData;
                var p0 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Min.X) + box.YAxis.ScaleBy(box.Bounds.Min.Y) + box.ZAxis.ScaleBy(box.Bounds.Min.Z)).ToPoint3D();
                var p1 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Max.X) + box.YAxis.ScaleBy(box.Bounds.Min.Y) + box.ZAxis.ScaleBy(box.Bounds.Min.Z)).ToPoint3D();
                var p2 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Max.X) + box.YAxis.ScaleBy(box.Bounds.Max.Y) + box.ZAxis.ScaleBy(box.Bounds.Min.Z)).ToPoint3D();
                var p3 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Min.X) + box.YAxis.ScaleBy(box.Bounds.Max.Y) + box.ZAxis.ScaleBy(box.Bounds.Min.Z)).ToPoint3D();
                var p4 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Min.X) + box.YAxis.ScaleBy(box.Bounds.Min.Y) + box.ZAxis.ScaleBy(box.Bounds.Max.Z)).ToPoint3D();
                var p5 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Max.X) + box.YAxis.ScaleBy(box.Bounds.Min.Y) + box.ZAxis.ScaleBy(box.Bounds.Max.Z)).ToPoint3D();
                var p6 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Max.X) + box.YAxis.ScaleBy(box.Bounds.Max.Y) + box.ZAxis.ScaleBy(box.Bounds.Max.Z)).ToPoint3D();
                var p7 = (box.Origin + box.XAxis.ScaleBy(box.Bounds.Min.X) + box.YAxis.ScaleBy(box.Bounds.Max.Y) + box.ZAxis.ScaleBy(box.Bounds.Max.Z)).ToPoint3D();

                this.UpdateLinePosition(this.edges[0], p0, p1);
                this.UpdateLinePosition(this.edges[1], p1, p2);
                this.UpdateLinePosition(this.edges[2], p2, p3);
                this.UpdateLinePosition(this.edges[3], p3, p0);
                this.UpdateLinePosition(this.edges[4], p4, p5);
                this.UpdateLinePosition(this.edges[5], p5, p6);
                this.UpdateLinePosition(this.edges[6], p6, p7);
                this.UpdateLinePosition(this.edges[7], p7, p4);
                this.UpdateLinePosition(this.edges[8], p0, p4);
                this.UpdateLinePosition(this.edges[9], p1, p5);
                this.UpdateLinePosition(this.edges[10], p2, p6);
                this.UpdateLinePosition(this.edges[11], p3, p7);

                // Update the facets.
                for (int i = 0; i < this.facets.Length; i++)
                {
                    var rectangle = box.GetFacet((Box3DFacet)Enum.GetValues(typeof(Box3DFacet)).GetValue(i));
                    this.facets[i].MeshGeometry.Positions[0] = rectangle.TopLeft.ToPoint3D();
                    this.facets[i].MeshGeometry.Positions[1] = rectangle.TopRight.ToPoint3D();
                    this.facets[i].MeshGeometry.Positions[2] = rectangle.BottomRight.ToPoint3D();
                    this.facets[i].MeshGeometry.Positions[3] = rectangle.BottomLeft.ToPoint3D();
                }
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            // Check if the changed property is one that require updating the lines in the image.
            if (propertyName == nameof(this.EdgeColor) ||
                propertyName == nameof(this.EdgeOpacity) ||
                propertyName == nameof(this.EdgeThicknessMm) ||
                propertyName == nameof(this.PipeDivisions))
            {
                this.UpdateLineProperties();
            }
            else if (propertyName == nameof(this.FacetColor) ||
                     propertyName == nameof(this.FacetOpacity))
            {
                this.UpdateFacetContents();
            }
            else if (propertyName == nameof(this.Visible) ||
                     propertyName == nameof(this.EdgeVisible) ||
                     propertyName == nameof(this.FacetVisible))
            {
                this.UpdateVisibility();
            }
        }

        private void CreateBoxEdges()
        {
            // Create the edges
            this.edges = new PipeVisual3D[12];
            for (int i = 0; i < this.edges.Length; i++)
            {
                this.edges[i] = new PipeVisual3D();
            }

            // Set the color, thickness, opacity
            this.UpdateLineProperties();
        }

        private void CreateBoxFacets()
        {
            this.facets = new MeshGeometryVisual3D[6];
            for (int i = 0; i < this.facets.Length; i++)
            {
                this.facets[i] = new MeshGeometryVisual3D
                {
                    MeshGeometry = new MeshGeometry3D(),
                };

                this.facets[i].MeshGeometry.Positions.Add(default);
                this.facets[i].MeshGeometry.Positions.Add(default);
                this.facets[i].MeshGeometry.Positions.Add(default);
                this.facets[i].MeshGeometry.Positions.Add(default);
                this.facets[i].MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 0));
                this.facets[i].MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 0));
                this.facets[i].MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 1));
                this.facets[i].MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 1));

                this.facets[i].MeshGeometry.TriangleIndices.Add(0);
                this.facets[i].MeshGeometry.TriangleIndices.Add(1);
                this.facets[i].MeshGeometry.TriangleIndices.Add(2);

                this.facets[i].MeshGeometry.TriangleIndices.Add(0);
                this.facets[i].MeshGeometry.TriangleIndices.Add(2);
                this.facets[i].MeshGeometry.TriangleIndices.Add(3);

                this.facets[i].MeshGeometry.TriangleIndices.Add(0);
                this.facets[i].MeshGeometry.TriangleIndices.Add(3);
                this.facets[i].MeshGeometry.TriangleIndices.Add(2);

                this.facets[i].MeshGeometry.TriangleIndices.Add(0);
                this.facets[i].MeshGeometry.TriangleIndices.Add(2);
                this.facets[i].MeshGeometry.TriangleIndices.Add(1);
            }
        }

        private void UpdateFacetContents()
        {
            double opacity = Math.Max(0, Math.Min(100, this.facetOpacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.facetColor.R,
                this.facetColor.G,
                this.facetColor.B);
            var material = new DiffuseMaterial(new SolidColorBrush(alphaColor));

            for (int i = 0; i < this.facets.Length; i++)
            {
                this.facets[i].Material = material;
                this.facets[i].BackMaterial = material;
            }
        }

        private void UpdateLinePosition(Visual3D visual, Point3D point1, Point3D point2)
        {
            PipeVisual3D line = visual as PipeVisual3D;
            line.Point1 = point1;
            line.Point2 = point2;
        }

        private void UpdateVisibility()
        {
            foreach (var line in this.edges)
            {
                this.UpdateChildVisibility(line, this.Visible && this.EdgeVisible && this.CurrentData != null);
            }

            foreach (var facet in this.facets)
            {
                this.UpdateChildVisibility(facet, this.Visible && this.FacetVisible && this.CurrentData != null);
            }
        }

        private void UpdateLineProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.EdgeOpacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.EdgeColor.R,
                this.EdgeColor.G,
                this.EdgeColor.B);

            foreach (PipeVisual3D line in this.edges)
            {
                line.Diameter = this.EdgeThicknessMm / 1000.0;
                line.Fill = new SolidColorBrush(alphaColor);
                line.ThetaDiv = this.PipeDivisions;
            }
        }
    }
}
