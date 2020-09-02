// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Extensions;
    using euclidean = MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a depth image mesh visual 3D.
    /// </summary>
    public class DepthImageMeshVisual3D : ModelVisual3D
    {
        private System.Windows.Media.Media3D.Point3D[] depthFramePoints;
        private int[] rawDepth;

        private Color color;
        private int transparency;

        private DepthImage depthImage;
        private ICameraIntrinsics intrinsics;
        private CoordinateSystem position;

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
                this.UpdateMaterial();
            }
        }

        /// <summary>
        /// Gets or sets the transparency.
        /// </summary>
        public int Transparency
        {
            get
            {
                return this.transparency;
            }

            set
            {
                this.transparency = value;
                this.UpdateMaterial();
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
        /// Updates the color of the mesh.
        /// </summary>
        public void UpdateMaterial()
        {
            if (this.Content != null)
            {
                var material = this.CreateMaterial();
                (this.Content as GeometryModel3D).Material = material;
                (this.Content as GeometryModel3D).BackMaterial = material;
            }
        }

        /// <summary>
        /// Updates the mesh from a specified depth image, intrinsics, and position.
        /// </summary>
        /// <param name="depthImage">The depth image.</param>
        /// <param name="intrinsics">The intrinsics.</param>
        /// <param name="position">The position.</param>
        public void UpdateMesh(DepthImage depthImage, ICameraIntrinsics intrinsics, CoordinateSystem position)
        {
            this.depthImage = depthImage;
            this.intrinsics = intrinsics;
            this.position = position;
            this.UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            this.UpdateDepthFramePoints(this.depthImage, this.intrinsics, this.position);

            if (this.depthImage != null && this.intrinsics != null && this.position != null)
            {
                this.CreateMesh(this.depthImage.Width, this.depthImage.Height);
            }
            else
            {
                this.CreateEmptyMesh();
            }
        }

        private void CreateMesh(int width, int height, double depthDifferenceTolerance = 200)
        {
            var meshGeometry = new MeshGeometry3D();
            var triangleIndices = new List<int>();
            for (int iy = 0; iy + 1 < height; iy++)
            {
                for (int ix = 0; ix + 1 < width; ix++)
                {
                    int i0 = (iy * width) + ix;
                    int i1 = (iy * width) + ix + 1;
                    int i2 = ((iy + 1) * width) + ix + 1;
                    int i3 = ((iy + 1) * width) + ix;

                    var d0 = this.rawDepth[i0];
                    var d1 = this.rawDepth[i1];
                    var d2 = this.rawDepth[i2];
                    var d3 = this.rawDepth[i3];

                    var dmax0 = Math.Max(Math.Max(d0, d1), d2);
                    var dmin0 = Math.Min(Math.Min(d0, d1), d2);
                    var dmax1 = Math.Max(d0, Math.Max(d2, d3));
                    var dmin1 = Math.Min(d0, Math.Min(d2, d3));

                    if (dmax0 - dmin0 < depthDifferenceTolerance && dmin0 != -1)
                    {
                        triangleIndices.Add(i0);
                        triangleIndices.Add(i1);
                        triangleIndices.Add(i2);
                    }

                    if (dmax1 - dmin1 < depthDifferenceTolerance && dmin1 != -1)
                    {
                        triangleIndices.Add(i0);
                        triangleIndices.Add(i2);
                        triangleIndices.Add(i3);
                    }
                }
            }

            meshGeometry.TriangleIndices = new Int32Collection(triangleIndices);
            meshGeometry.Positions = new Point3DCollection(this.depthFramePoints);

            var material = this.CreateMaterial();
            this.Content = new GeometryModel3D(meshGeometry, material);
            (this.Content as GeometryModel3D).BackMaterial = material;
        }

        private void CreateEmptyMesh()
        {
            var meshGeometry = new MeshGeometry3D();
            var triangleIndices = new List<int>();
            meshGeometry.TriangleIndices = new Int32Collection(triangleIndices);
            meshGeometry.Positions = new Point3DCollection(this.depthFramePoints);
            var material = this.CreateMaterial();
            this.Content = new GeometryModel3D(meshGeometry, material);
            (this.Content as GeometryModel3D).BackMaterial = material;
        }

        private void UpdateDepthFramePoints(DepthImage depthImage, ICameraIntrinsics intrinsics, euclidean.CoordinateSystem position)
        {
            // Handle null cases by clearing the depthFramePoints
            if (depthImage == null || intrinsics == null || position == null)
            {
                if (this.rawDepth.Length > 0)
                {
                    this.rawDepth = new int[0];
                    this.depthFramePoints = new System.Windows.Media.Media3D.Point3D[0];
                }

                return;
            }

            if (this.depthFramePoints?.Length != (this.depthImage.Width * this.depthImage.Height))
            {
                this.rawDepth = new int[this.depthImage.Width * this.depthImage.Height];
                this.depthFramePoints = new System.Windows.Media.Media3D.Point3D[this.depthImage.Width * this.depthImage.Height];
            }

            int width = depthImage.Width;
            int height = depthImage.Height;

            int cx = width / 2;
            int cy = height / 2;

            double scale = 0.001;

            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)depthImage.ImageData.ToPointer());

                Parallel.For(0, height, iy =>
                {
                    for (int ix = 0; ix < width; ix++)
                    {
                        int i = (iy * width) + ix;
                        this.rawDepth[i] = depthFrame[i];

                        if (this.rawDepth[i] == 0)
                        {
                            this.rawDepth[i] = -1;
                            this.depthFramePoints[i] = default;
                        }
                        else
                        {
                            var other = intrinsics.ToCameraSpace(new euclidean.Point2D(ix, iy), this.rawDepth[i] * scale, true);
                            this.depthFramePoints[i] = other.TransformBy(position).ToPoint3D();
                        }
                    }
                });
            }
        }

        private Material CreateMaterial()
        {
            return new DiffuseMaterial(
                    new SolidColorBrush(
                        Color.FromArgb(
                            (byte)(Math.Max(0, Math.Min(100, this.transparency)) * 2.55),
                            this.color.R,
                            this.color.G,
                            this.color.B)));
        }
    }
}