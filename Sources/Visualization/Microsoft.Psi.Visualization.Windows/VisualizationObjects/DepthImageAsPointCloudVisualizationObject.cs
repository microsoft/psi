// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using MathNet.Numerics.LinearAlgebra;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a depth image 3D point cloud visualization object.
    /// </summary>
    [VisualizationObject("3D Point Cloud With Manual Focal Length")]
    public class DepthImageAsPointCloudVisualizationObject : ModelVisual3DVisualizationObject<Shared<EncodedDepthImage>>
    {
        private readonly DepthImagePointCloudVisual3D depthImagePointCloudVisual3D;
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;

        private CameraIntrinsics intrinsics;
        private Color pointCloudColor = Colors.Gray;
        private double pointSize = 1.0;
        private int sparsity = 3;
        private Color frustumColor = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;
        private double focalLengthX = 500;
        private double focalLengthY = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public DepthImageAsPointCloudVisualizationObject()
        {
            this.depthImagePointCloudVisual3D = new DepthImagePointCloudVisual3D()
            {
                Color = this.pointCloudColor,
                Size = this.pointSize,
                Sparsity = this.sparsity,
            };

            this.cameraIntrinsicsVisual3D = new CameraIntrinsicsVisual3D()
            {
                Color = this.frustumColor,
                ImagePlaneDistanceCm = this.imagePlaneDistanceCm,
            };
        }

        /// <summary>
        /// Gets or sets the point cloud color.
        /// </summary>
        [DataMember]
        [DisplayName("Point Color")]
        [Description("The color of a point in the cloud.")]
        public Color PointCloudColor
        {
            get { return this.pointCloudColor; }
            set { this.Set(nameof(this.PointCloudColor), ref this.pointCloudColor, value); }
        }

        /// <summary>
        /// Gets or sets the point size.
        /// </summary>
        [DataMember]
        [DisplayName("Point Size")]
        [Description("The size of a point in the cloud.")]
        public double PointSize
        {
            get { return this.pointSize; }
            set { this.Set(nameof(this.PointSize), ref this.pointSize, value); }
        }

        /// <summary>
        /// Gets or sets the point cloud sparsity.
        /// </summary>
        [DataMember]
        [DisplayName("Point Cloud Sparsity")]
        [Description("The sparsity (in pixels) of the point cloud.")]
        public int Sparsity
        {
            get { return this.sparsity; }
            set { this.Set(nameof(this.Sparsity), ref this.sparsity, value); }
        }

        /// <summary>
        /// Gets or sets the frustum color.
        /// </summary>
        [DataMember]
        [DisplayName("Frustum Color")]
        [Description("The color of the rendered frustum.")]
        public Color FrustumColor
        {
            get { return this.frustumColor; }
            set { this.Set(nameof(this.FrustumColor), ref this.frustumColor, value); }
        }

        /// <summary>
        /// Gets or sets the image plane distance.
        /// </summary>
        [DataMember]
        [DisplayName("Image Plane Distance (cm)")]
        [Description("The image plane distance in centimeters.")]
        public double ImagePlaneDistanceCm
        {
            get { return this.imagePlaneDistanceCm; }
            set { this.Set(nameof(this.ImagePlaneDistanceCm), ref this.imagePlaneDistanceCm, value); }
        }

        /// <summary>
        /// Gets or sets the focal length in the X dimension.
        /// </summary>
        [DataMember]
        [DisplayName("Focal Length X")]
        [Description("The focal length in the X dimension.")]
        public double FocalLengthX
        {
            get { return this.focalLengthX; }
            set { this.Set(nameof(this.FocalLengthX), ref this.focalLengthX, value); }
        }

        /// <summary>
        /// Gets or sets the focal length in the Y dimension.
        /// </summary>
        [DataMember]
        [DisplayName("Focal Length Y")]
        [Description("The focal length in the Y dimension.")]
        public double FocalLengthY
        {
            get { return this.focalLengthY; }
            set { this.Set(nameof(this.FocalLengthY), ref this.focalLengthY, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.PointCloudColor))
            {
                this.depthImagePointCloudVisual3D.Color = this.PointCloudColor;
            }
            else if (propertyName == nameof(this.PointSize))
            {
                this.depthImagePointCloudVisual3D.Size = this.PointSize;
            }
            else if (propertyName == nameof(this.Sparsity))
            {
                this.depthImagePointCloudVisual3D.Sparsity = this.Sparsity;
            }
            else if (propertyName == nameof(this.FrustumColor))
            {
                this.cameraIntrinsicsVisual3D.Color = this.FrustumColor;
            }
            else if (propertyName == nameof(this.ImagePlaneDistanceCm))
            {
                this.cameraIntrinsicsVisual3D.ImagePlaneDistanceCm = this.ImagePlaneDistanceCm;
            }
            else if (propertyName == nameof(this.FocalLengthX) || propertyName == nameof(this.FocalLengthY))
            {
                this.UpdateVisuals();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.ComputeIntrinsics();
            if (this.intrinsics != null)
            {
                this.depthImagePointCloudVisual3D.UpdatePointCloud(this.CurrentData?.Resource?.Decode(new DepthImageFromStreamDecoder()), this.intrinsics, new MathNet.Spatial.Euclidean.CoordinateSystem());
                this.cameraIntrinsicsVisual3D.Intrinsics = this.intrinsics;
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthImagePointCloudVisual3D, this.Visible && this.CurrentData != default && this.intrinsics != null);
            this.UpdateChildVisibility(this.cameraIntrinsicsVisual3D, this.Visible && this.intrinsics != null);
        }

        private void ComputeIntrinsics()
        {
            if (this.CurrentData == null || this.CurrentData.Resource == null)
            {
                this.intrinsics = null;
            }
            else
            {
                var width = this.CurrentData.Resource.Width;
                var height = this.CurrentData.Resource.Height;
                var transform = Matrix<double>.Build.Dense(3, 3);
                transform[0, 0] = this.FocalLengthX;
                transform[1, 1] = this.FocalLengthY;
                transform[2, 2] = 1;
                transform[0, 2] = width / 2.0;
                transform[1, 2] = height / 2.0;
                this.intrinsics = new CameraIntrinsics(width, height, transform);
            }
        }
    }
}
