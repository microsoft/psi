// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a depth image 3D point cloud visualization object.
    /// </summary>
    [VisualizationObject("Encoded Depth Image 3D Point Cloud with Intrinsics")]
    public class DepthImageWithIntrinsicsAsPointCloudVisualizationObject : ModelVisual3DVisualizationObject<(Shared<EncodedDepthImage>, ICameraIntrinsics)>
    {
        private readonly DepthImagePointCloudVisual3D depthImagePointCloudVisual3D;
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;

        private Color pointCloudColor = Colors.Gray;
        private double pointSize = 1.0;
        private int sparsity = 3;
        private Color frustumColor = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageWithIntrinsicsAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public DepthImageWithIntrinsicsAsPointCloudVisualizationObject()
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
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.depthImagePointCloudVisual3D.UpdatePointCloud(this.CurrentData.Item1?.Resource?.Decode(new DepthImageFromStreamDecoder()), this.CurrentData.Item2, new MathNet.Spatial.Euclidean.CoordinateSystem());
            this.cameraIntrinsicsVisual3D.Intrinsics = this.CurrentData.Item2;
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthImagePointCloudVisual3D, this.Visible && this.CurrentData.Item1 != default);
            this.UpdateChildVisibility(this.cameraIntrinsicsVisual3D, this.Visible);
        }
    }
}
