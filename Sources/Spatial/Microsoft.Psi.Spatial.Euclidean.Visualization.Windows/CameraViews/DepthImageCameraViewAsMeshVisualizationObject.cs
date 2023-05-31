// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
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
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for a <see cref="DepthImageCameraView"/>.
    /// </summary>
    [VisualizationObject("Depth Camera View (Mesh)")]
    public class DepthImageCameraViewAsMeshVisualizationObject : ModelVisual3DVisualizationObject<DepthImageCameraView>
    {
        private readonly ModelVisual3D depthImageMesh;
        private CameraIntrinsicsWithPoseVisualizationObject frustum;

        private Shared<DepthImage> depthImage = null;
        private ICameraIntrinsics intrinsics = null;
        private CoordinateSystem position = null;
        private System.Windows.Media.Media3D.Point3D[] depthFramePoints;
        private int[] rawDepth;

        private Color meshColor = Colors.Gray;
        private int meshTransparency = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageCameraViewAsMeshVisualizationObject"/> class.
        /// </summary>
        public DepthImageCameraViewAsMeshVisualizationObject()
        {
            this.depthImageMesh = new ModelVisual3D();
            this.frustum = new CameraIntrinsicsWithPoseVisualizationObject();
        }

        /// <summary>
        /// Gets or sets the visualization object for the frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Frustum")]
        [Description("The frustum representing the depth camera.")]
        public CameraIntrinsicsWithPoseVisualizationObject Frustum
        {
            get { return this.frustum; }
            set { this.Set(nameof(this.Frustum), ref this.frustum, value); }
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
        protected override Action<DepthImageCameraView> Deallocator => data => data.ViewedObject?.Dispose();

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.depthImage != null)
            {
                this.depthImage.Dispose();
            }

            this.depthImage = this.CurrentData?.ViewedObject?.AddRef();
            this.intrinsics = this.CurrentData?.CameraIntrinsics;
            this.position = this.CurrentData?.CameraPose;

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
            this.frustum.SetCurrentValue(this.SynthesizeMessage((this.intrinsics, this.position)));

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
            this.UpdateChildVisibility(this.frustum.ModelView, this.Visible && this.CurrentData != default && this.intrinsics != null && this.position != null);
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

            double scale = this.depthImage.Resource.DepthValueToMetersScaleFactor;

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
                            var other = this.intrinsics.GetCameraSpacePosition(new Point2D(ix, iy), this.rawDepth[i] * scale, this.depthImage.Resource.DepthValueSemantics, true);
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
