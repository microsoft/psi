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
    /// Implements a 3D depth image rectangle visualization object.
    /// </summary>
    [VisualizationObject("Depth Image in 3D Rectangle")]
    public class DepthImageRectangle3DVisualizationObject : ModelVisual3DVisualizationObject<DepthImageRectangle3D>
    {
        private readonly MeshGeometryVisual3D depthImageModelVisual;
        private readonly DisplayImage displayImage;
        private readonly PipeVisual3D[] borderEdges;

        // Fill properties
        private double imageOpacity = 100;
        private Shared<DepthImage> depthImage = null;

        // Border properties
        private Color borderColor = Colors.White;
        private double borderThicknessMm = 15;
        private double borderOpacity = 100;
        private int pipeDiv = 7;

        /// <summary>
        /// The depth image range.
        /// </summary>
        private DepthImageRangeMode rangeMode = DepthImageRangeMode.Maximum;

        /// <summary>
        /// Indicates the value in the depth image that is considered invalid and pseudo-colorized as transparent.
        /// </summary>
        private int invalidValue = -1;

        /// <summary>
        /// Indicates whether to render invalid depths as transparent.
        /// </summary>
        private bool invalidAsTransparent = false;

        /// <summary>
        /// Indicates the minimum of the depth values range in the image.
        /// </summary>
        private int rangeMin = 0;

        /// <summary>
        /// Indicates the maximum of the depth values range in the image.
        /// </summary>
        private int rangeMax = 65535;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageRectangle3DVisualizationObject"/> class.
        /// </summary>
        public DepthImageRectangle3DVisualizationObject()
        {
            // Create a rectangle mesh for the image
            this.displayImage = new DisplayImage();
            this.depthImageModelVisual = new MeshGeometryVisual3D
            {
                MeshGeometry = new MeshGeometry3D(),
            };

            this.depthImageModelVisual.MeshGeometry.Positions.Add(default);
            this.depthImageModelVisual.MeshGeometry.Positions.Add(default);
            this.depthImageModelVisual.MeshGeometry.Positions.Add(default);
            this.depthImageModelVisual.MeshGeometry.Positions.Add(default);
            this.depthImageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 0));
            this.depthImageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 0));
            this.depthImageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 1));
            this.depthImageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 1));

            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(1);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(2);

            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(2);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(3);

            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(3);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(2);

            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(2);
            this.depthImageModelVisual.MeshGeometry.TriangleIndices.Add(1);

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

        /// <summary>
        /// Gets or sets a value indicating an invalid depth.
        /// </summary>
        [DataMember]
        [DisplayName("Invalid Value")]
        [Description("Specifies the pixel value that denotes an invalid depth.")]
        public int InvalidValue
        {
            get { return this.invalidValue; }
            set { this.Set(nameof(this.InvalidValue), ref this.invalidValue, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to render invalid depths as transparent.
        /// </summary>
        [DataMember]
        [DisplayName("Invalid Value as Transparent")]
        [Description("Indicates whether to render invalid depths as transparent.")]
        public bool InvalidAsTransparent
        {
            get { return this.invalidAsTransparent; }
            set { this.Set(nameof(this.InvalidAsTransparent), ref this.invalidAsTransparent, value); }
        }

        /// <summary>
        /// Gets or sets the range of values to use.
        /// </summary>
        [DataMember]
        [DisplayName("Range Mode")]
        [Description("Specifies the range of depth values in the image.")]
        public DepthImageRangeMode RangeMode
        {
            get => this.rangeMode;
            set
            {
                this.Set(nameof(this.RangeMode), ref this.rangeMode, value);
                if (this.rangeMode != DepthImageRangeMode.Auto && this.rangeMode != DepthImageRangeMode.Custom)
                {
                    (var min, var max, var invalid) = DepthImageVisualizationObject.GetRange(this.rangeMode);
                    this.SetRange(min, max);
                    this.InvalidValue = invalid;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the minimum of the depth values range in the image.
        /// </summary>
        [DataMember]
        [DisplayName("Range Min")]
        [Description("Specifies the minimum depth value for pseudo-colorizing the image.")]
        public int RangeMin
        {
            get => this.rangeMin;
            set
            {
                if (value != this.rangeMin)
                {
                    this.RangeMode = DepthImageRangeMode.Custom;
                    this.Set(nameof(this.RangeMin), ref this.rangeMin, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the maximum of the depth values range in the image.
        /// </summary>
        [DataMember]
        [DisplayName("Range Max")]
        [Description("Specifies the maximum depth value for pseudo-colorizing the image.")]
        public int RangeMax
        {
            get => this.rangeMax;
            set
            {
                if (value != this.rangeMax)
                {
                    this.RangeMode = DepthImageRangeMode.Custom;
                    this.Set(nameof(this.RangeMax), ref this.rangeMax, value);
                }
            }
        }

        /// <inheritdoc/>
        protected override Action<DepthImageRectangle3D> Deallocator => data => data?.Dispose();

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null && !this.CurrentData.Rectangle3D.IsDegenerate &&
                this.CurrentData.Image != null && this.CurrentData.Image.Resource != null)
            {
                if (this.depthImage != null)
                {
                    this.depthImage.Dispose();
                }

                this.depthImage = this.CurrentData.Image.AddRef();

                var rectangle = this.CurrentData.Rectangle3D;
                var topLeftPoint3D = rectangle.TopLeft.ToPoint3D();
                var topRightPoint3D = rectangle.TopRight.ToPoint3D();
                var bottomRightPoint3D = rectangle.BottomRight.ToPoint3D();
                var bottomLeftPoint3D = rectangle.BottomLeft.ToPoint3D();

                this.depthImageModelVisual.MeshGeometry.Positions[0] = topLeftPoint3D;
                this.depthImageModelVisual.MeshGeometry.Positions[1] = topRightPoint3D;
                this.depthImageModelVisual.MeshGeometry.Positions[2] = bottomRightPoint3D;
                this.depthImageModelVisual.MeshGeometry.Positions[3] = bottomLeftPoint3D;

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
            if (propertyName == nameof(this.ImageOpacity) ||
                propertyName == nameof(this.RangeMax) ||
                propertyName == nameof(this.RangeMin) ||
                propertyName == nameof(this.RangeMode) ||
                propertyName == nameof(this.InvalidValue) ||
                propertyName == nameof(this.InvalidAsTransparent))
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
            if (this.depthImage != null && this.depthImage.Resource != null)
            {
                // Update the display image
                using var sharedColorizedImage = ImagePool.GetOrCreate(
                    this.depthImage.Resource.Width,
                    this.depthImage.Resource.Height,
                    Imaging.PixelFormat.BGRA_32bpp);

                if (this.RangeMode == DepthImageRangeMode.Auto)
                {
                    (var minRange, var maxRange) = this.depthImage.Resource.GetPixelRange();
                    this.SetRange(minRange, maxRange);
                }

                this.depthImage.Resource.PseudoColorize(
                    sharedColorizedImage.Resource,
                    ((ushort)this.RangeMin, (ushort)this.RangeMax),
                    (this.InvalidValue < 0) ? null : (ushort)this.InvalidValue,
                    this.InvalidAsTransparent);

                this.displayImage.UpdateImage(sharedColorizedImage);

                // Render the display image
                var material = new DiffuseMaterial(new ImageBrush(this.displayImage.Image) { Opacity = this.imageOpacity * 0.01 });
                this.depthImageModelVisual.Material = material;
                this.depthImageModelVisual.BackMaterial = material;
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
            var visibleImage = visibleBorder && this.depthImage != null && this.depthImage.Resource != null;

            this.UpdateChildVisibility(this.depthImageModelVisual, visibleImage);

            foreach (PipeVisual3D line in this.borderEdges)
            {
                this.UpdateChildVisibility(line, visibleBorder);
            }
        }

        /// <summary>
        /// Programmatically sets the range without altering the range compute mode.
        /// </summary>
        /// <param name="rangeMin">The new range minimum value.</param>
        /// <param name="rangeMax">The new range maximum value.</param>
        private void SetRange(int rangeMin, int rangeMax)
        {
            this.Set(nameof(this.RangeMin), ref this.rangeMin, rangeMin);
            this.Set(nameof(this.RangeMax), ref this.rangeMax, rangeMax);
        }
    }
}
