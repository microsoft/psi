// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a depth image 3D point cloud visualization object.
    /// </summary>
    [VisualizationObject("Visualize Depth Image as 3D Point Cloud")]
    public class DepthImageAs3DPointCloudVisualizationObject : Instant3DVisualizationObject<Shared<DepthImage>>
    {
        private Color color = Colors.Green;
        private double pointSize = 1.0;
        private int sparsity = 1;

        private PointsVisual3D pointsVisual;

        /// <summary>
        /// Gets or sets the point cloud color.
        /// </summary>
        [DataMember]
        [DisplayName("Color")]
        [Description("Color used for point cloud points.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the point size.
        /// </summary>
        [DataMember]
        [DisplayName("Point Size")]
        [Description("Size of each point cloud point.")]
        public double PointSize
        {
            get { return this.pointSize; }
            set { this.Set(nameof(this.PointSize), ref this.pointSize, value); }
        }

        /// <summary>
        /// Gets or sets the point cloud sparsity.
        /// </summary>
        [DataMember]
        [DisplayName("Sparsity")]
        [Description("Sparseness of the cloud point.")]
        public int Sparsity
        {
            get { return this.sparsity; }
            set { this.Set(nameof(this.Sparsity), ref this.sparsity, value); }
        }

        /// <inheritdoc/>
        protected override void InitNew()
        {
            this.PropertyChanged += this.VisualizationPropertyChanged;
            this.Visual3D = this.pointsVisual = new PointsVisual3D()
            {
                Size = this.PointSize,
                Color = this.Color,
            };
            base.InitNew();
        }

        private void UpdatePoints(DepthImage depthImage)
        {
            var points = this.pointsVisual.Points;
            this.pointsVisual.Points = null; // "unhook" for performance
            points.Clear();

            int width = depthImage.Width;
            int height = depthImage.Height;
            int cx = width / 2;
            int cy = height / 2;

            const double fxinv = 1.0 / 366;
            const double fyinv = 1.0 / 366;
            const double scale = 0.001; // millimeters

            const ushort tooNearDepth = 500;
            const ushort tooFarDepth = 10000;
            const ushort unknownDepth = 0;

            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)depthImage.ImageData.ToPointer());

                for (int iy = 0; iy < height; iy += this.sparsity)
                {
                    for (int ix = 0; ix < width; ix += this.sparsity)
                    {
                        int i = (iy * width) + ix;
                        var d = depthFrame[(iy * width) + ix];
                        if (d != unknownDepth && d > tooNearDepth && d < tooFarDepth)
                        {
                            double zz = d * scale;
                            double x = (cx - ix) * zz * fxinv;
                            double y = zz;
                            double z = (cy - iy) * zz * fyinv;
                            points.Add(new Point3D(x, y, z));
                        }
                    }
                }
            }

            this.pointsVisual.Points = points;
        }

        private void VisualizationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.CurrentValue):
                case nameof(this.Sparsity):
                    if (this.CurrentData != null)
                    {
                        this.UpdatePoints(this.CurrentData.Resource);
                    }

                    break;
                case nameof(this.Color):
                    this.pointsVisual.Color = this.Color;
                    break;
                case nameof(this.PointSize):
                    this.pointsVisual.Size = this.PointSize;
                    break;
            }
        }
    }
}
