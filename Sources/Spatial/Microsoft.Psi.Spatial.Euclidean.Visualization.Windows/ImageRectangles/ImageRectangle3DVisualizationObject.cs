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
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a 3D image rectangle visualization object.
    /// </summary>
    [VisualizationObject("Image in 3D Rectangle")]
    public class ImageRectangle3DVisualizationObject : ModelVisual3DVisualizationObject<ImageRectangle3D>
    {
        private readonly MeshGeometryVisual3D modelVisual;
        private readonly DisplayImage displayImage;
        private readonly PipeVisual3D[] borderEdges;

        // Fill properties
        private double imageOpacity = 100;
        private Shared<Image> image = null;

        // Border properties
        private Color borderColor = Colors.White;
        private double borderThicknessMm = 15;
        private double borderOpacity = 100;
        private int pipeDiv = 7;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3DVisualizationObject"/> class.
        /// </summary>
        public ImageRectangle3DVisualizationObject()
        {
            // Create the fill mesh
            this.displayImage = new DisplayImage();
            this.modelVisual = new MeshGeometryVisual3D
            {
                MeshGeometry = new MeshGeometry3D(),
            };

            this.modelVisual.MeshGeometry.Positions.Add(default);
            this.modelVisual.MeshGeometry.Positions.Add(default);
            this.modelVisual.MeshGeometry.Positions.Add(default);
            this.modelVisual.MeshGeometry.Positions.Add(default);
            this.modelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 0));
            this.modelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 0));
            this.modelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 1));
            this.modelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 1));

            this.modelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(1);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(2);

            this.modelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(2);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(3);

            this.modelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(3);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(2);

            this.modelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(2);
            this.modelVisual.MeshGeometry.TriangleIndices.Add(1);

            // Create the border lines
            this.borderEdges = new PipeVisual3D[4];
            for (int i = 0; i < this.borderEdges.Length; i++)
            {
                this.borderEdges[i] = new PipeVisual3D();
            }

            // Set the color, thickness, opacity
            this.UpdateLineProperties();

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the image opacity.
        /// </summary>
        [DataMember]
        [DisplayName("Image Opacity")]
        [Description("The image opacity inside the rectangle.")]
        public double ImageOpacity
        {
            get { return this.imageOpacity; }
            set { this.Set(nameof(this.ImageOpacity), ref this.imageOpacity, value); }
        }

        /// <summary>
        /// Gets or sets the border color.
        /// </summary>
        [DataMember]
        [DisplayName("Border Color")]
        [Description("The color of the rectangle border.")]
        public Color BorderColor
        {
            get { return this.borderColor; }
            set { this.Set(nameof(this.BorderColor), ref this.borderColor, value); }
        }

        /// <summary>
        /// Gets or sets the border thickness.
        /// </summary>
        [DataMember]
        [DisplayName("Border Thickness (mm)")]
        [Description("The thickness of the rectangle border in millimeters.")]
        public double BorderThicknessMm
        {
            get { return this.borderThicknessMm; }
            set { this.Set(nameof(this.BorderThicknessMm), ref this.borderThicknessMm, value); }
        }

        /// <summary>
        /// Gets or sets the border opacity.
        /// </summary>
        [DataMember]
        [DisplayName("Border Opacity")]
        [Description("The opacity of the rectangle border.")]
        public double BorderOpacity
        {
            get { return this.borderOpacity; }
            set { this.Set(nameof(this.BorderOpacity), ref this.borderOpacity, value); }
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

        /// <inheritdoc/>
        protected override Action<ImageRectangle3D> Deallocator => data => data?.Dispose();

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null && !this.CurrentData.Rectangle3D.IsDegenerate &&
                this.CurrentData.Image != null && this.CurrentData.Image.Resource != null)
            {
                if (this.image != null)
                {
                    this.image.Dispose();
                }

                this.image = this.CurrentData.Image.AddRef();

                var rectangle = this.CurrentData.Rectangle3D;
                var topLeftPoint3D = rectangle.TopLeft.ToPoint3D();
                var topRightPoint3D = rectangle.TopRight.ToPoint3D();
                var bottomRightPoint3D = rectangle.BottomRight.ToPoint3D();
                var bottomLeftPoint3D = rectangle.BottomLeft.ToPoint3D();

                this.modelVisual.MeshGeometry.Positions[0] = topLeftPoint3D;
                this.modelVisual.MeshGeometry.Positions[1] = topRightPoint3D;
                this.modelVisual.MeshGeometry.Positions[2] = bottomRightPoint3D;
                this.modelVisual.MeshGeometry.Positions[3] = bottomLeftPoint3D;

                this.UpdateLinePosition(this.borderEdges[0], topLeftPoint3D, topRightPoint3D);
                this.UpdateLinePosition(this.borderEdges[1], topRightPoint3D, bottomRightPoint3D);
                this.UpdateLinePosition(this.borderEdges[2], bottomRightPoint3D, bottomLeftPoint3D);
                this.UpdateLinePosition(this.borderEdges[3], bottomLeftPoint3D, topLeftPoint3D);

                this.UpdateRectangleContents();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.ImageOpacity))
            {
                this.UpdateRectangleContents();
            }
            else if (propertyName == nameof(this.BorderColor) ||
                     propertyName == nameof(this.BorderOpacity) ||
                     propertyName == nameof(this.BorderThicknessMm) ||
                     propertyName == nameof(this.PipeDivisions))
            {
                this.UpdateLineProperties();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateRectangleContents()
        {
            if (this.image != null && this.image.Resource != null)
            {
                // Update the display image
                this.displayImage.UpdateImage(this.image);

                // Render the display image
                var material = new DiffuseMaterial(new ImageBrush(this.displayImage.Image) { Opacity = this.imageOpacity * 0.01 });
                this.modelVisual.Material = material;
                this.modelVisual.BackMaterial = material;
            }
        }

        private void UpdateLinePosition(Visual3D visual, Point3D point1, Point3D point2)
        {
            PipeVisual3D line = visual as PipeVisual3D;
            line.Point1 = point1;
            line.Point2 = point2;
        }

        private void UpdateLineProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.borderOpacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.borderColor.R,
                this.borderColor.G,
                this.borderColor.B);

            foreach (PipeVisual3D line in this.borderEdges)
            {
                line.Diameter = this.borderThicknessMm / 1000.0;
                line.Fill = new SolidColorBrush(alphaColor);
                line.ThetaDiv = this.pipeDiv;
            }
        }

        private void UpdateVisibility()
        {
            var visibleBorder = this.Visible && this.CurrentData != null && !this.CurrentData.Rectangle3D.IsDegenerate;
            var visibleImage = visibleBorder && this.image != null && this.image.Resource != null;

            this.UpdateChildVisibility(this.modelVisual, visibleImage);

            foreach (PipeVisual3D line in this.borderEdges)
            {
                this.UpdateChildVisibility(line, visibleBorder);
            }
        }
    }
}
