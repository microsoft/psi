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
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for a spatial camera view, from an image, camera intrinsics, and spatial position.
    /// </summary>
    [VisualizationObject("3D Camera View")]
    public class SpatialCameraViewVisualizationObject : ModelVisual3DVisualizationObject<(Shared<Image>, ICameraIntrinsics, CoordinateSystem)>
    {
        private readonly MeshGeometryVisual3D imageModelVisual;
        private readonly DisplayImage displayImage;

        private SpatialCameraVisualizationObject spatialCamera;
        private Shared<Image> image = null;
        private CoordinateSystem position = null;
        private ICameraIntrinsics intrinsics = null;
        private int imageTransparency = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialCameraViewVisualizationObject"/> class.
        /// </summary>
        public SpatialCameraViewVisualizationObject()
        {
            // Instantiate the child visualizer for visualizing the camera frustum,
            // and register for its property changed notifications.
            this.spatialCamera = new SpatialCameraVisualizationObject();
            this.spatialCamera.RegisterChildPropertyChangedNotifications(this, nameof(this.SpatialCamera));

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
        /// Gets or sets the visualization object for the camera intrinsics as a frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Spatial Camera")]
        [Description("The frustum representing the spatial camera.")]
        public SpatialCameraVisualizationObject SpatialCamera
        {
            get { return this.spatialCamera; }
            set { this.Set(nameof(this.SpatialCamera), ref this.spatialCamera, value); }
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
        protected override System.Action<(Shared<Image>, ICameraIntrinsics, CoordinateSystem)> Deallocator => data => data.Item1?.Dispose();

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.image != null)
            {
                this.image.Dispose();
            }

            this.image = this.CurrentData.Item1?.AddRef();
            this.intrinsics = this.CurrentData.Item2;
            this.position = this.CurrentData.Item3;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.ImageTransparency))
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
            if (path == nameof(this.SpatialCamera) + "." + nameof(this.SpatialCamera.ImagePlaneDistanceCm))
            {
                this.UpdateVisualPosition();
            }

            base.OnChildPropertyChanged(path, value);
        }

        private void UpdateVisuals()
        {
            this.spatialCamera.SetCurrentValue(this.SynthesizeMessage((this.intrinsics, this.position)));
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
                var material = new Win3D.DiffuseMaterial(new ImageBrush(this.displayImage.Image) { Opacity = this.ImageTransparency * 0.01 });
                this.imageModelVisual.Material = material;
                this.imageModelVisual.BackMaterial = material;
            }
        }

        private void UpdateVisualPosition()
        {
            if (this.intrinsics != null && this.position != null)
            {
                var focalDistance = this.spatialCamera.ImagePlaneDistanceCm * 0.01;
                var leftWidth = focalDistance * this.intrinsics.PrincipalPoint.X / this.intrinsics.FocalLength;
                var rightWidth = focalDistance * (this.intrinsics.ImageWidth - this.intrinsics.PrincipalPoint.X) / this.intrinsics.FocalLength;
                var topHeight = focalDistance * this.intrinsics.PrincipalPoint.Y / this.intrinsics.FocalLength;
                var bottomHeight = focalDistance * (this.intrinsics.ImageHeight - this.intrinsics.PrincipalPoint.Y) / this.intrinsics.FocalLength;

                var pointingAxis = this.position.XAxis;
                var imageRightAxis = this.position.YAxis.Negate();
                var imageUpAxis = this.position.ZAxis;

                var principalPoint = this.position.Origin + pointingAxis.ScaleBy(focalDistance);

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
            var visible = this.Visible && this.CurrentData != default && this.image != null && this.image.Resource != null && this.intrinsics != null && this.position != null;
            this.UpdateChildVisibility(this.imageModelVisual, visible);
            this.UpdateChildVisibility(this.spatialCamera.ModelView, visible);
        }
    }
}
