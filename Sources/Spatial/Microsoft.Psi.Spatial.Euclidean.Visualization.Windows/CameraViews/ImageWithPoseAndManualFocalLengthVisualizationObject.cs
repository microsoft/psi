// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for an image camera view, from an image and spatial position.
    /// Camera intrinsics are determined from manually set focal length properties.
    /// </summary>
    [VisualizationObject("Image Camera View with Manual Focal Length")]
    public class ImageWithPoseAndManualFocalLengthVisualizationObject : ModelVisual3DVisualizationObject<(Shared<Image>, CoordinateSystem)>
    {
        private ImageCameraViewVisualizationObject imageCameraView;

        private Shared<Image> image = null;
        private ICameraIntrinsics intrinsics = null;
        private CoordinateSystem position = null;
        private double focalLengthX = 500;
        private double focalLengthY = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageWithPoseAndManualFocalLengthVisualizationObject"/> class.
        /// </summary>
        public ImageWithPoseAndManualFocalLengthVisualizationObject()
        {
            this.imageCameraView = new ImageCameraViewVisualizationObject();
        }

        /// <summary>
        /// Gets or sets the visualization object for the camera view, including the image plane and frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Image Camera View")]
        [Description("The image camera view, including image plane and frustum.")]
        public ImageCameraViewVisualizationObject ImageCameraView
        {
            get { return this.imageCameraView; }
            set { this.Set(nameof(this.ImageCameraView), ref this.imageCameraView, value); }
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
            this.intrinsics = this.image?.Resource?.CreateCameraIntrinsics(this.FocalLengthX, this.FocalLengthY);
            this.imageCameraView.SetCurrentValue(this.SynthesizeMessage(new ImageCameraView(this.image, this.intrinsics, this.position)));
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.imageCameraView.ModelView, this.Visible && this.CurrentData != default);
        }
    }
}
