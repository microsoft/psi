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
    /// Represents a depth image 3D mesh visualization object.
    /// </summary>
    [VisualizationObject("Encoded Depth Image 3D Mesh with Intrinsics")]
    public class DepthImageWithIntrinsicsAsMeshVisualizationObject : ModelVisual3DVisualizationObject<(Shared<EncodedDepthImage>, ICameraIntrinsics)>
    {
        private readonly DepthImageMeshVisual3D depthImageMeshVisual3D;
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;

        private Color meshColor = Colors.Gray;
        private int meshTransparency = 50;
        private Color frustumColor = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageWithIntrinsicsAsMeshVisualizationObject"/> class.
        /// </summary>
        public DepthImageWithIntrinsicsAsMeshVisualizationObject()
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
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.depthImageMeshVisual3D.UpdateMesh(this.CurrentData.Item1?.Resource?.Decode(new DepthImageFromStreamDecoder()), this.CurrentData.Item2, new MathNet.Spatial.Euclidean.CoordinateSystem());
            this.cameraIntrinsicsVisual3D.Intrinsics = this.CurrentData.Item2;
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthImageMeshVisual3D, this.Visible && this.CurrentData.Item1 != default);
            this.UpdateChildVisibility(this.cameraIntrinsicsVisual3D, this.Visible);
        }
    }
}
