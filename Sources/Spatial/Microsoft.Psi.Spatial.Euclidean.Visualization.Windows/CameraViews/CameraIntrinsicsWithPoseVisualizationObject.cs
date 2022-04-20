// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for camera frustrum described via camera intrinsics and position.
    /// </summary>
    [VisualizationObject("Camera Intrinsics with Pose")]
    public class CameraIntrinsicsWithPoseVisualizationObject : ModelVisual3DVisualizationObject<(ICameraIntrinsics, CoordinateSystem)>
    {
        private readonly List<LinesVisual3D> frustum = new ();
        private CoordinateSystem position = null;
        private ICameraIntrinsics intrinsics = null;
        private Color color = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraIntrinsicsWithPoseVisualizationObject"/> class.
        /// </summary>
        public CameraIntrinsicsWithPoseVisualizationObject()
        {
            // Create frustum lines
            for (int i = 0; i < 8; i++)
            {
                var linesVisual3D = new LinesVisual3D()
                {
                    Color = this.color,
                };

                for (int p = 0; p < 2; p++)
                {
                    linesVisual3D.Points.Add(default);
                }

                this.frustum.Add(linesVisual3D);
            }
        }

        /// <summary>
        /// Gets or sets the frustum color.
        /// </summary>
        [DataMember]
        [DisplayName("Color")]
        [Description("The color of the frustum.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
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
            this.intrinsics = this.CurrentData.Item1;
            this.position = this.CurrentData.Item2;
            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color))
            {
                this.UpdateColor();
            }
            else if (propertyName == nameof(this.ImagePlaneDistanceCm))
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
            if (this.intrinsics != null && this.position != null)
            {
                var focalDistance = this.ImagePlaneDistanceCm * 0.01;
                var leftWidth = focalDistance * this.intrinsics.PrincipalPoint.X / this.intrinsics.FocalLengthXY.X;
                var rightWidth = focalDistance * (this.intrinsics.ImageWidth - this.intrinsics.PrincipalPoint.X) / this.intrinsics.FocalLengthXY.X;
                var topHeight = focalDistance * this.intrinsics.PrincipalPoint.Y / this.intrinsics.FocalLengthXY.Y;
                var bottomHeight = focalDistance * (this.intrinsics.ImageHeight - this.intrinsics.PrincipalPoint.Y) / this.intrinsics.FocalLengthXY.Y;

                var cameraPoint3D = this.position.Origin.ToPoint3D();

                var pointingAxis = this.position.XAxis;
                var imageRightAxis = this.position.YAxis.Negate();
                var imageUpAxis = this.position.ZAxis;

                var principalPoint = this.position.Origin + pointingAxis.ScaleBy(focalDistance);

                var topLeftPoint3D = (principalPoint - imageRightAxis.ScaleBy(leftWidth) + imageUpAxis.ScaleBy(topHeight)).ToPoint3D();
                var topRightPoint3D = (principalPoint + imageRightAxis.ScaleBy(rightWidth) + imageUpAxis.ScaleBy(topHeight)).ToPoint3D();
                var bottomRightPoint3D = (principalPoint + imageRightAxis.ScaleBy(rightWidth) - imageUpAxis.ScaleBy(bottomHeight)).ToPoint3D();
                var bottomLeftPoint3D = (principalPoint - imageRightAxis.ScaleBy(leftWidth) - imageUpAxis.ScaleBy(bottomHeight)).ToPoint3D();

                this.frustum[0].Points[0] = topLeftPoint3D;
                this.frustum[0].Points[1] = topRightPoint3D;

                this.frustum[1].Points[0] = topRightPoint3D;
                this.frustum[1].Points[1] = bottomRightPoint3D;

                this.frustum[2].Points[0] = bottomRightPoint3D;
                this.frustum[2].Points[1] = bottomLeftPoint3D;

                this.frustum[3].Points[0] = bottomLeftPoint3D;
                this.frustum[3].Points[1] = topLeftPoint3D;

                this.frustum[4].Points[0] = cameraPoint3D;
                this.frustum[4].Points[1] = topLeftPoint3D;

                this.frustum[5].Points[0] = cameraPoint3D;
                this.frustum[5].Points[1] = topRightPoint3D;

                this.frustum[6].Points[0] = cameraPoint3D;
                this.frustum[6].Points[1] = bottomLeftPoint3D;

                this.frustum[7].Points[0] = cameraPoint3D;
                this.frustum[7].Points[1] = bottomRightPoint3D;
            }
        }

        private void UpdateVisibility()
        {
            foreach (var edge in this.frustum)
            {
                this.UpdateChildVisibility(edge, this.Visible && this.CurrentData != default && this.intrinsics != null && this.position != null);
            }
        }

        private void UpdateColor()
        {
            foreach (var edge in this.frustum)
            {
                edge.Color = this.color;
            }
        }
    }
}
