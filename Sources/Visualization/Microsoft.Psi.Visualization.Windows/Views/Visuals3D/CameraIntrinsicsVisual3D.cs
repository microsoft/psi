// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.Collections.Generic;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Visualization.Extensions;

    /// <summary>
    /// Implements 3D visual for camera intrinsics.
    /// </summary>
    public class CameraIntrinsicsVisual3D : ModelVisual3D
    {
        private readonly List<LinesVisual3D> pyramid = new List<LinesVisual3D>();
        private CoordinateSystem position = new CoordinateSystem();
        private ICameraIntrinsics intrinsics = null;
        private Color color = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraIntrinsicsVisual3D"/> class.
        /// </summary>
        public CameraIntrinsicsVisual3D()
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

                this.pyramid.Add(linesVisual3D);
                this.Children.Add(linesVisual3D);
            }
        }

        /// <summary>
        /// Gets or sets the color of the frustum.
        /// </summary>
        public Color Color
        {
            get { return this.color; }

            set
            {
                this.color = value;
                this.UpdateColor();
            }
        }

        /// <summary>
        /// Gets or sets the distance from the camera to the rendered image plane.
        /// </summary>
        public double ImagePlaneDistanceCm
        {
            get { return this.imagePlaneDistanceCm; }

            set
            {
                this.imagePlaneDistanceCm = value;
                this.UpdateVisualPosition();
            }
        }

        /// <summary>
        /// Gets or sets the camera intrinsics.
        /// </summary>
        public ICameraIntrinsics Intrinsics
        {
            get { return this.intrinsics; }

            set
            {
                this.intrinsics = value;
                this.UpdateVisualPosition();
            }
        }

        /// <summary>
        /// Gets or sets the camera position.
        /// </summary>
        public CoordinateSystem Position
        {
            get { return this.position; }

            set
            {
                this.position = value;
                this.UpdateVisualPosition();
            }
        }

        private void UpdateColor()
        {
            for (int i = 0; i < this.pyramid.Count; i++)
            {
                this.pyramid[i].Color = this.color;
            }
        }

        private void UpdateVisualPosition()
        {
            if (this.intrinsics != null)
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

                this.pyramid[0].Points[0] = topLeftPoint3D;
                this.pyramid[0].Points[1] = topRightPoint3D;

                this.pyramid[1].Points[0] = topRightPoint3D;
                this.pyramid[1].Points[1] = bottomRightPoint3D;

                this.pyramid[2].Points[0] = bottomRightPoint3D;
                this.pyramid[2].Points[1] = bottomLeftPoint3D;

                this.pyramid[3].Points[0] = bottomLeftPoint3D;
                this.pyramid[3].Points[1] = topLeftPoint3D;

                this.pyramid[4].Points[0] = cameraPoint3D;
                this.pyramid[4].Points[1] = topLeftPoint3D;

                this.pyramid[5].Points[0] = cameraPoint3D;
                this.pyramid[5].Points[1] = topRightPoint3D;

                this.pyramid[6].Points[0] = cameraPoint3D;
                this.pyramid[6].Points[1] = bottomLeftPoint3D;

                this.pyramid[7].Points[0] = cameraPoint3D;
                this.pyramid[7].Points[1] = bottomRightPoint3D;
            }
        }
    }
}