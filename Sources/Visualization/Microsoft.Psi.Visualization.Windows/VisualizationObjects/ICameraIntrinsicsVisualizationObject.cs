// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a visualization object for <see cref="ICameraIntrinsics"/>.
    /// </summary>
    [VisualizationObject("Camera Intrinsics")]
    public class ICameraIntrinsicsVisualizationObject : ModelVisual3DVisualizationObject<ICameraIntrinsics>
    {
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;

        private Color frustumColor = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICameraIntrinsicsVisualizationObject"/> class.
        /// </summary>
        public ICameraIntrinsicsVisualizationObject()
        {
            this.cameraIntrinsicsVisual3D = new CameraIntrinsicsVisual3D()
            {
                Color = this.frustumColor,
                ImagePlaneDistanceCm = this.imagePlaneDistanceCm,
            };
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
            if (propertyName == nameof(this.FrustumColor))
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
            this.cameraIntrinsicsVisual3D.Intrinsics = this.CurrentData;
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.cameraIntrinsicsVisual3D, this.Visible);
        }
    }
}
