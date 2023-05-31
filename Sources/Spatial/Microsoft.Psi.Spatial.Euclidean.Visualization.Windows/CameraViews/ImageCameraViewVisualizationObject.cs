// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for a image view, from an <see cref="ImageCameraView"/>.
    /// </summary>
    [VisualizationObject("Image Camera View")]
    public class ImageCameraViewVisualizationObject : ModelVisual3DVisualizationObject<ImageCameraView>
    {
        private readonly MeshGeometryVisual3D imageModelVisual;
        private readonly DisplayImage displayImage;

        private CameraIntrinsicsWithPoseVisualizationObject frustum;
        private Shared<Image> image = null;
        private CoordinateSystem pose = null;
        private ICameraIntrinsics intrinsics = null;
        private int imageOpacity = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCameraViewVisualizationObject"/> class.
        /// </summary>
        public ImageCameraViewVisualizationObject()
        {
            // Instantiate the child visualizer for visualizing the camera frustum,
            // and register for its property changed notifications.
            this.frustum = new CameraIntrinsicsWithPoseVisualizationObject();
            this.frustum.RegisterChildPropertyChangedNotifications(this, nameof(this.Frustum));

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
        /// Gets or sets the visualization object for the frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Frustum")]
        [Description("The frustum representing the camera.")]
        public CameraIntrinsicsWithPoseVisualizationObject Frustum
        {
            get { return this.frustum; }
            set { this.Set(nameof(this.Frustum), ref this.frustum, value); }
        }

        /// <summary>
        /// Gets or sets the image transparency.
        /// </summary>
        [DataMember]
        [DisplayName("Image Opacity (%)")]
        [Description("The opacity level (percentage) for the image.")]
        public int ImageOpacity
        {
            get { return this.imageOpacity; }
            set { this.Set(nameof(this.ImageOpacity), ref this.imageOpacity, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.image != null)
            {
                this.image.Dispose();
                this.image = null;
            }

            this.image = this.CurrentData?.ViewedObject?.AddRef();
            this.intrinsics = this.CurrentData?.CameraIntrinsics;
            this.pose = this.CurrentData?.CameraPose;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.ImageOpacity))
            {
                this.UpdateImageContents();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <inheritdoc/>
        public override void OnChildPropertyChanged(string path, object value)
        {
            if (path == nameof(this.Frustum) + "." + nameof(this.Frustum.ImagePlaneDistanceCm))
            {
                this.UpdateVisualPosition();
            }

            base.OnChildPropertyChanged(path, value);
        }

        private void UpdateVisuals()
        {
            this.frustum.SetCurrentValue(this.SynthesizeMessage((this.intrinsics, this.pose)));
            this.UpdateImageContents();
            this.UpdateVisualPosition();
        }

        private void UpdateImageContents()
        {
            if (this.image != null && this.image.Resource != null)
            {
                // Update the display image
                this.displayImage.UpdateImage(this.image);

                // Render the display image
                var material = new Win3D.DiffuseMaterial(new ImageBrush(this.displayImage.Image) { Opacity = this.ImageOpacity * 0.01 });
                this.imageModelVisual.Material = material;
                this.imageModelVisual.BackMaterial = material;
            }
        }

        private void UpdateVisualPosition()
        {
            if (this.intrinsics != null && this.pose != null)
            {
                var focalDistance = this.frustum.ImagePlaneDistanceCm * 0.01;
                var leftWidth = focalDistance * this.intrinsics.PrincipalPoint.X / this.intrinsics.FocalLength;
                var rightWidth = focalDistance * (this.intrinsics.ImageWidth - this.intrinsics.PrincipalPoint.X) / this.intrinsics.FocalLength;
                var topHeight = focalDistance * this.intrinsics.PrincipalPoint.Y / this.intrinsics.FocalLength;
                var bottomHeight = focalDistance * (this.intrinsics.ImageHeight - this.intrinsics.PrincipalPoint.Y) / this.intrinsics.FocalLength;

                var pointingAxis = this.pose.XAxis;
                var imageRightAxis = this.pose.YAxis.Negate();
                var imageUpAxis = this.pose.ZAxis;

                var principalPoint = this.pose.Origin + pointingAxis.ScaleBy(focalDistance);

                var topLeftPoint3D = (principalPoint - imageRightAxis.ScaleBy(leftWidth) + imageUpAxis.ScaleBy(topHeight)).ToPoint3D();
                var topRightPoint3D = (principalPoint + imageRightAxis.ScaleBy(rightWidth) + imageUpAxis.ScaleBy(topHeight)).ToPoint3D();
                var bottomRightPoint3D = (principalPoint + imageRightAxis.ScaleBy(rightWidth) - imageUpAxis.ScaleBy(bottomHeight)).ToPoint3D();
                var bottomLeftPoint3D = (principalPoint - imageRightAxis.ScaleBy(leftWidth) - imageUpAxis.ScaleBy(bottomHeight)).ToPoint3D();

                this.imageModelVisual.MeshGeometry.Positions[0] = topLeftPoint3D;
                this.imageModelVisual.MeshGeometry.Positions[1] = topRightPoint3D;
                this.imageModelVisual.MeshGeometry.Positions[2] = bottomRightPoint3D;
                this.imageModelVisual.MeshGeometry.Positions[3] = bottomLeftPoint3D;
            }
        }

        private void UpdateVisibility()
        {
            var visible = this.Visible && this.CurrentData != default && this.image != null && this.image.Resource != null && this.intrinsics != null && this.pose != null;
            this.UpdateChildVisibility(this.imageModelVisual, visible);
            this.UpdateChildVisibility(this.frustum.ModelView, visible);
        }
    }
}
