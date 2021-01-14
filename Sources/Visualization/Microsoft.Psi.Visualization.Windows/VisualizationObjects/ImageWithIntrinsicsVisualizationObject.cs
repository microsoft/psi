// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.Views.Visuals3D;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a 3D image with intrinsics visualization object.
    /// </summary>
    [VisualizationObject("Image with Intrinsics")]
    public class ImageWithIntrinsicsVisualizationObject : ModelVisual3DVisualizationObject<(Shared<Image>, ICameraIntrinsics)>
    {
        private static readonly CoordinateSystem OriginCoordinateSystem = new CoordinateSystem();

        private readonly MeshGeometryVisual3D imageModelVisual;
        private readonly DisplayImage displayImage;
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;
        private ICameraIntrinsics currentIntrinsics = null;

        private Color frustumColor = Colors.DimGray;
        private double imagePlaneDistanceCm = 100;
        private int imageTransparency = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageWithIntrinsicsVisualizationObject"/> class.
        /// </summary>
        public ImageWithIntrinsicsVisualizationObject()
        {
            this.cameraIntrinsicsVisual3D = new CameraIntrinsicsVisual3D()
            {
                Color = this.frustumColor,
                ImagePlaneDistanceCm = this.imagePlaneDistanceCm,
            };

            // Create a rectangle mesh for the image
            this.displayImage = new DisplayImage();
            this.imageModelVisual = new MeshGeometryVisual3D
            {
                MeshGeometry = new Win3D.MeshGeometry3D(),
            };

            this.imageModelVisual.MeshGeometry.Positions.Add(default);
            this.imageModelVisual.MeshGeometry.Positions.Add(default);
            this.imageModelVisual.MeshGeometry.Positions.Add(default);
            this.imageModelVisual.MeshGeometry.Positions.Add(default);
            this.imageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 0));
            this.imageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 0));
            this.imageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(1, 1));
            this.imageModelVisual.MeshGeometry.TextureCoordinates.Add(new System.Windows.Point(0, 1));

            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(1);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(2);

            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(2);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(3);

            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(3);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(2);

            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(0);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(2);
            this.imageModelVisual.MeshGeometry.TriangleIndices.Add(1);
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
        /// Gets or sets the image transparency.
        /// </summary>
        [DataMember]
        [DisplayName("Image Transparency (%)")]
        [Description("The transparency level (percentage) for the image.")]
        public int ImageTransparency
        {
            get { return this.imageTransparency; }
            set { this.Set(nameof(this.ImageTransparency), ref this.imageTransparency, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.currentIntrinsics == null)
            {
                this.currentIntrinsics = this.CurrentData.Item2;
            }
            else
            {
                this.CurrentData.Item2.DeepClone(ref this.currentIntrinsics);
            }

            this.UpdateImageContents();
            this.UpdateVisualPosition();
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
                this.UpdateVisualPosition();
            }
            else if (propertyName == nameof(this.ImageTransparency))
            {
                this.UpdateImageContents();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateImageContents()
        {
            if (this.CurrentData.Item1 != null && this.CurrentData.Item1.Resource != null)
            {
                // Update the display image
                this.displayImage.UpdateImage(this.CurrentData.Item1);

                // Render the display image
                var material = new Win3D.DiffuseMaterial(new ImageBrush(this.displayImage.Image) { Opacity = this.ImageTransparency * 0.01 });
                this.imageModelVisual.Material = material;
                this.imageModelVisual.BackMaterial = material;
            }

            this.UpdateVisibility();
        }

        private void UpdateVisualPosition()
        {
            if (this.currentIntrinsics != null)
            {
                var focalDistance = this.ImagePlaneDistanceCm * 0.01;
                var leftWidth = focalDistance * this.currentIntrinsics.PrincipalPoint.X / this.currentIntrinsics.FocalLength;
                var rightWidth = focalDistance * (this.currentIntrinsics.ImageWidth - this.currentIntrinsics.PrincipalPoint.X) / this.currentIntrinsics.FocalLength;
                var topHeight = focalDistance * this.currentIntrinsics.PrincipalPoint.Y / this.currentIntrinsics.FocalLength;
                var bottomHeight = focalDistance * (this.currentIntrinsics.ImageHeight - this.currentIntrinsics.PrincipalPoint.Y) / this.currentIntrinsics.FocalLength;

                var pointingAxis = OriginCoordinateSystem.XAxis;
                var imageRightAxis = OriginCoordinateSystem.YAxis.Negate();
                var imageUpAxis = OriginCoordinateSystem.ZAxis;

                var principalPoint = OriginCoordinateSystem.Origin + pointingAxis.ScaleBy(focalDistance);

                var topLeftPoint3D = (principalPoint - imageRightAxis.ScaleBy(leftWidth) + imageUpAxis.ScaleBy(topHeight)).ToPoint3D();
                var topRightPoint3D = (principalPoint + imageRightAxis.ScaleBy(rightWidth) + imageUpAxis.ScaleBy(topHeight)).ToPoint3D();
                var bottomRightPoint3D = (principalPoint + imageRightAxis.ScaleBy(rightWidth) - imageUpAxis.ScaleBy(bottomHeight)).ToPoint3D();
                var bottomLeftPoint3D = (principalPoint - imageRightAxis.ScaleBy(leftWidth) - imageUpAxis.ScaleBy(bottomHeight)).ToPoint3D();

                this.cameraIntrinsicsVisual3D.ImagePlaneDistanceCm = this.imagePlaneDistanceCm;
                this.cameraIntrinsicsVisual3D.Intrinsics = this.currentIntrinsics;

                this.imageModelVisual.MeshGeometry.Positions[0] = topLeftPoint3D;
                this.imageModelVisual.MeshGeometry.Positions[1] = topRightPoint3D;
                this.imageModelVisual.MeshGeometry.Positions[2] = bottomRightPoint3D;
                this.imageModelVisual.MeshGeometry.Positions[3] = bottomLeftPoint3D;
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.imageModelVisual, this.Visible && this.CurrentData.Item1 != null && this.CurrentData.Item1.Resource != null);
            this.UpdateChildVisibility(this.cameraIntrinsicsVisual3D, this.Visible && this.CurrentData.Item1 != null && this.CurrentData.Item1.Resource != null);
        }
    }
}
