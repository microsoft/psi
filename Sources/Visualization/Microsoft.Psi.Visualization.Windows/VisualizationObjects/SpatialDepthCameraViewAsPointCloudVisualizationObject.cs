// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Extensions;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for a spatial depth camera view as a point cloud, from a depth image, camera intrinsics, and position.
    /// </summary>
    [VisualizationObject("3D Depth Camera View as Point Cloud")]
    public class SpatialDepthCameraViewAsPointCloudVisualizationObject : ModelVisual3DVisualizationObject<(Shared<DepthImage>, ICameraIntrinsics, CoordinateSystem)>
    {
        private Point3DListAsPointCloudVisualizationObject pointCloud;
        private SpatialCameraVisualizationObject spatialCamera;

        private int sparsity = 3;
        private Shared<DepthImage> depthImage = null;
        private ICameraIntrinsics intrinsics = null;
        private CoordinateSystem position = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDepthCameraViewAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public SpatialDepthCameraViewAsPointCloudVisualizationObject()
        {
            this.PointCloud = new Point3DListAsPointCloudVisualizationObject();
            this.SpatialCamera = new SpatialCameraVisualizationObject();
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
        /// Gets or sets the visualization object for the point cloud.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Point Cloud")]
        [Description("The 3D point cloud representing the depth image.")]
        public Point3DListAsPointCloudVisualizationObject PointCloud
        {
            get { return this.pointCloud; }
            set { this.Set(nameof(this.PointCloud), ref this.pointCloud, value); }
        }

        /// <summary>
        /// Gets or sets the point cloud sparsity.
        /// </summary>
        [DataMember]
        [DisplayName("Point Cloud Sparsity")]
        [Description("The sparsity (in pixels) of the point cloud (min=1).")]
        public int Sparsity
        {
            get { return this.sparsity; }
            set { this.Set(nameof(this.Sparsity), ref this.sparsity, value > 0 ? value : 1); }
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
            if (propertyName == nameof(this.Sparsity))
            {
                this.UpdateDepthImagePointCloud();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.spatialCamera.SetCurrentValue(this.SynthesizeMessage((this.intrinsics, this.position)));
            this.UpdateDepthImagePointCloud();
        }

        private void UpdateDepthImagePointCloud()
        {
            if (this.depthImage == null || this.depthImage.Resource == null || this.intrinsics == null || this.position == null)
            {
                this.PointCloud.SetCurrentValue(this.SynthesizeMessage<List<Windows.Point3D>>(null));
                return;
            }

            var points = new List<Windows.Point3D>();
            int width = this.depthImage.Resource.Width;
            int height = this.depthImage.Resource.Height;
            int cx = width / 2;
            int cy = height / 2;

            double scale = 0.001;

            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)this.depthImage.Resource.ImageData.ToPointer());

                for (int iy = 0; iy < height; iy += this.sparsity)
                {
                    for (int ix = 0; ix < width; ix += this.sparsity)
                    {
                        int i = (iy * width) + ix;
                        var d = depthFrame[i];
                        if (d != 0)
                        {
                            var other = this.intrinsics.ToCameraSpace(new Point2D(ix, iy), depthFrame[i] * scale, true);
                            points.Add(other.TransformBy(this.position).ToPoint3D());
                        }
                    }
                }
            }

            this.PointCloud.SetCurrentValue(this.SynthesizeMessage(points));
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.pointCloud.ModelView, this.Visible && this.CurrentData != default && this.depthImage != null && this.depthImage.Resource != null && this.intrinsics != null && this.position != null);
            this.UpdateChildVisibility(this.spatialCamera.ModelView, this.Visible && this.CurrentData != default && this.intrinsics != null && this.position != null);
        }
    }
}
