// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Extensions;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for a spatial depth camera view as a 3D mesh, from a depth image, camera intrinsics, and position.
    /// </summary>
    [VisualizationObject("3D Depth Camera View as Mesh")]
    public class SpatialDepthCameraViewAsMeshVisualizationObject : ModelVisual3DVisualizationObject<(Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        private SpatialCameraVisualizationObject spatialCamera;
        private ModelVisual3D depthImageMesh;

        private Shared<DepthImage> depthImage = null;
        private ICameraIntrinsics intrinsics = null;
        private CoordinateSystem position = null;
        private System.Windows.Media.Media3D.Point3D[] depthFramePoints;
        private int[] rawDepth;

        private Color meshColor = Colors.Gray;
        private int meshTransparency = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDepthCameraViewAsMeshVisualizationObject"/> class.
        /// </summary>
        public SpatialDepthCameraViewAsMeshVisualizationObject()
        {
            this.depthImageMesh = new ModelVisual3D();
            this.spatialCamera = new SpatialCameraVisualizationObject();
        }

        /// <summary>
        /// Gets or sets the visualization object for the camera intrinsics as a frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Frustum")]
        [Description("The frustum representing the spatial depth camera.")]
        public SpatialCameraVisualizationObject SpatialCamera
        {
            get { return this.spatialCamera; }
            set { this.Set(nameof(this.SpatialCamera), ref this.spatialCamera, value); }
        }

        /// <summary>
        /// Gets or sets the mesh color.
        /// </summary>
        [DataMember]
        [DisplayName("Mesh Color")]
        [Description("The color for the mesh.")]
        public Color MeshColor
        {
            get { return this.meshColor; }
            set { this.Set(nameof(this.MeshColor), ref this.meshColor, value); }
        }

        /// <summary>
        /// Gets or sets the mesh transparency.
        /// </summary>
        [DataMember]
        [DisplayName("Mesh Transparency")]
        [Description("The transparency level for the mesh.")]
        public int MeshTransparency
        {
            get { return this.meshTransparency; }
            set { this.Set(nameof(this.MeshTransparency), ref this.meshTransparency, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.depthImage != null)
            {
                this.depthImage.Dispose();
            }

            this.depthImage = this.CurrentData.Item1?.AddRef();
            this.intrinsics = this.CurrentData.Item2;
            this.position = this.CurrentData.Item3;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.MeshColor))
            {
                this.UpdateMaterial();
            }
            else if (propertyName == nameof(this.MeshTransparency))
            {
                this.UpdateMaterial();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.spatialCamera.SetCurrentValue(this.SynthesizeMessage((this.intrinsics, this.position)));

            this.UpdateDepthFramePoints();

            if (this.depthImage != null && this.depthImage.Resource != null && this.intrinsics != null && this.position != null)
            {
                this.CreateMesh(this.depthImage.Resource.Width, this.depthImage.Resource.Height);
            }
            else
            {
                this.CreateEmptyMesh();
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthImageMesh, this.Visible && this.CurrentData != default && this.depthImage != null && this.depthImage.Resource != null && this.intrinsics != null && this.position != null);
            this.UpdateChildVisibility(this.spatialCamera.ModelView, this.Visible && this.CurrentData != default && this.intrinsics != null && this.position != null);
        }

        private void UpdateMaterial()
        {
            if (this.depthImageMesh.Content != null)
            {
                var material = this.CreateMaterial();
                (this.depthImageMesh.Content as GeometryModel3D).Material = material;
                (this.depthImageMesh.Content as GeometryModel3D).BackMaterial = material;
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
            this.depthImageMesh.Content = new GeometryModel3D(meshGeometry, material);
            (this.depthImageMesh.Content as GeometryModel3D).BackMaterial = material;
        }

        private void CreateEmptyMesh()
        {
            var meshGeometry = new MeshGeometry3D();
            var triangleIndices = new List<int>();
            meshGeometry.TriangleIndices = new Int32Collection(triangleIndices);
            meshGeometry.Positions = new Point3DCollection(this.depthFramePoints);
            var material = this.CreateMaterial();
            this.depthImageMesh.Content = new GeometryModel3D(meshGeometry, material);
            (this.depthImageMesh.Content as GeometryModel3D).BackMaterial = material;
        }

        private void UpdateDepthFramePoints()
        {
            // Handle null cases by clearing the depthFramePoints
            if (this.depthImage == null || this.depthImage.Resource == null || this.intrinsics == null || this.position == null)
            {
                if (this.rawDepth.Length > 0)
                {
                    this.rawDepth = new int[0];
                    this.depthFramePoints = new System.Windows.Media.Media3D.Point3D[0];
                }

                return;
            }

            if (this.depthFramePoints?.Length != (this.depthImage.Resource.Width * this.depthImage.Resource.Height))
            {
                this.rawDepth = new int[this.depthImage.Resource.Width * this.depthImage.Resource.Height];
                this.depthFramePoints = new System.Windows.Media.Media3D.Point3D[this.depthImage.Resource.Width * this.depthImage.Resource.Height];
            }

            int width = this.depthImage.Resource.Width;
            int height = this.depthImage.Resource.Height;

            int cx = width / 2;
            int cy = height / 2;

            double scale = 0.001;

            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)this.depthImage.Resource.ImageData.ToPointer());

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
                            var other = this.intrinsics.ToCameraSpace(new Point2D(ix, iy), this.rawDepth[i] * scale, true);
                            this.depthFramePoints[i] = other.TransformBy(this.position).ToPoint3D();
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
                        (byte)(Math.Max(0, Math.Min(100, this.meshTransparency)) * 2.55),
                        this.meshColor.R,
                        this.meshColor.G,
                        this.meshColor.B)));
        }
    }
}
