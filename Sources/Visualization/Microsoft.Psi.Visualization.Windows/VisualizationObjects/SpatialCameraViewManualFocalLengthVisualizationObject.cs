// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for a spatial camera view, from an image and spatial position.
    /// Camera intrinsics are determined from manually set focal length properties.
    /// </summary>
    [VisualizationObject("3D Camera View")]
    public class SpatialCameraViewManualFocalLengthVisualizationObject : ModelVisual3DVisualizationObject<(Shared<Image>, CoordinateSystem)>
    {
        private SpatialCameraViewVisualizationObject cameraView;

        private Shared<Image> image = null;
        private ICameraIntrinsics intrinsics = null;
        private CoordinateSystem position = null;
        private double focalLengthX = 500;
        private double focalLengthY = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialCameraViewManualFocalLengthVisualizationObject"/> class.
        /// </summary>
        public SpatialCameraViewManualFocalLengthVisualizationObject()
        {
            this.cameraView = new SpatialCameraViewVisualizationObject();
        }

        /// <summary>
        /// Gets or sets the visualization object for the camera view, including the image plane and frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Spatial Camera View")]
        [Description("The spatial camera view, including image plane and frustum.")]
        public SpatialCameraViewVisualizationObject CameraView
        {
            get { return this.cameraView; }
            set { this.Set(nameof(this.CameraView), ref this.cameraView, value); }
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
        protected override Action<(Shared<Image>, CoordinateSystem)> Deallocator => data => data.Item1?.Dispose();

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.image != null)
            {
                this.image.Dispose();
            }

            this.image = this.CurrentData.Item1?.AddRef();
            this.position = this.CurrentData.Item2;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.FocalLengthX) || propertyName == nameof(this.FocalLengthY))
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
            this.intrinsics = this.image?.Resource?.ComputeCameraIntrinsics(this.FocalLengthX, this.FocalLengthY);
            this.cameraView.SetCurrentValue(this.SynthesizeMessage((this.image, this.intrinsics, this.position)));
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.cameraView.ModelView, this.Visible && this.CurrentData != default);
        }
    }
}
