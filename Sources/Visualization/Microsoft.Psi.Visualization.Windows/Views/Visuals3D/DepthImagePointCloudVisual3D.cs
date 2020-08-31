// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Extensions;
    using euclidean = MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a depth image point cloud visual 3D.
    /// </summary>
    public class DepthImagePointCloudVisual3D : ModelVisual3D
    {
        private readonly PointsVisual3D pointsVisual3D;
        private Color color;
        private double size;
        private int sparsity;

        private DepthImage depthImage;
        private ICameraIntrinsics intrinsics;
        private CoordinateSystem position;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImagePointCloudVisual3D"/> class.
        /// </summary>
        public DepthImagePointCloudVisual3D()
        {
            this.pointsVisual3D = new PointsVisual3D()
            {
                Color = this.color,
                Size = this.size,
            };

            this.Children.Add(this.pointsVisual3D);
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Color Color
        {
            get
            {
                return this.color;
            }

            set
            {
                this.color = value;
                this.pointsVisual3D.Color = this.color;
            }
        }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Size
        {
            get
            {
                return this.size;
            }

            set
            {
                this.size = value;
                this.pointsVisual3D.Size = this.size;
            }
        }

        /// <summary>
        /// Gets or sets the sparsity.
        /// </summary>
        public int Sparsity
        {
            get { return this.sparsity; }

            set
            {
                this.sparsity = value;
                this.UpdateVisuals();
            }
        }

        /// <summary>
        /// Gets or sets the depth image.
        /// </summary>
        public DepthImage DepthImage
        {
            get { return this.depthImage; }

            set
            {
                this.depthImage = value;
                this.UpdateVisuals();
            }
        }

        /// <summary>
        /// Gets or sets the intrinsics.
        /// </summary>
        public ICameraIntrinsics Intrinsics
        {
            get { return this.intrinsics; }

            set
            {
                this.intrinsics = value;
                this.UpdateVisuals();
            }
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        public CoordinateSystem Position
        {
            get { return this.position; }

            set
            {
                this.position = value;
                this.UpdateVisuals();
            }
        }

        /// <summary>
        /// Updates the points from a specified depth image, intrinsics, and position.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="intrinsics">The intrinsics.</param>
        /// <param name="position">The position.</param>
        public void UpdatePointCloud(DepthImage depthImage, ICameraIntrinsics intrinsics, CoordinateSystem position)
        {
            this.depthImage = depthImage;
            this.intrinsics = intrinsics;
            this.position = position;
            this.UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            var points = this.pointsVisual3D.Points;
            this.pointsVisual3D.Points = null; // "unhook" for performance
            points.Clear();

            if (this.depthImage == null || this.intrinsics == null || this.position == null)
            {
                this.pointsVisual3D.Points = points;
                return;
            }

            int width = this.depthImage.Width;
            int height = this.depthImage.Height;
            int cx = width / 2;
            int cy = height / 2;

            double scale = 0.001;

            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)this.depthImage.ImageData.ToPointer());

                for (int iy = 0; iy < height; iy += this.sparsity)
                {
                    for (int ix = 0; ix < width; ix += this.sparsity)
                    {
                        int i = (iy * width) + ix;
                        var d = depthFrame[i];
                        if (d != 0)
                        {
                            var other = this.intrinsics.ToCameraSpace(new euclidean.Point2D(ix, iy), depthFrame[i] * scale, true);
                            points.Add(other.TransformBy(this.position).ToPoint3D());
                        }
                    }
                }
            }

            this.pointsVisual3D.Points = points;
        }
    }
}