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
    /// Represents a depth image 3D mesh visualization object.
    /// </summary>
    [VisualizationObject("3D Mesh with Manual Focal Length")]
    public class DepthImageAsMeshVisualizationObject : ModelVisual3DVisualizationObject<Shared<EncodedDepthImage>>
    {
        private readonly DepthImageMeshVisual3D depthImageMeshVisual3D;
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;

        private CameraIntrinsics intrinsics;
        private Color meshColor = Colors.Gray;
        private int meshTransparency = 50;
        private Color frustumColor = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;
        private double focalLengthX = 500;
        private double focalLengthY = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageAsMeshVisualizationObject"/> class.
        /// </summary>
        public DepthImageAsMeshVisualizationObject()
        {
            this.depthImageMeshVisual3D = new DepthImageMeshVisual3D()
            {
                Color = this.meshColor,
                Transparency = this.meshTransparency,
            };

            this.cameraIntrinsicsVisual3D = new CameraIntrinsicsVisual3D()
            {
                Color = this.frustumColor,
                ImagePlaneDistanceCm = this.imagePlaneDistanceCm,
            };
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
            if (propertyName == nameof(this.MeshColor))
            {
                this.depthImageMeshVisual3D.Color = this.MeshColor;
            }
            else if (propertyName == nameof(this.MeshTransparency))
            {
                this.depthImageMeshVisual3D.Transparency = this.MeshTransparency;
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
                this.depthImageMeshVisual3D.UpdateMesh(this.CurrentData?.Resource?.Decode(new DepthImageFromStreamDecoder()), this.intrinsics, new MathNet.Spatial.Euclidean.CoordinateSystem());
                this.cameraIntrinsicsVisual3D.Intrinsics = this.intrinsics;
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthImageMeshVisual3D, this.Visible && this.CurrentData != default && this.intrinsics != null);
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
