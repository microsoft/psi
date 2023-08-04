// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.Windows;
    using Microsoft.Win32;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for <see cref="DepthImageCameraView"/> as a point cloud.
    /// </summary>
    [VisualizationObject("Depth Camera View (Point Cloud)")]
    public class DepthImageCameraViewAsPointCloudVisualizationObject : ModelVisual3DVisualizationObject<DepthImageCameraView>
    {
        private Point3DListAsPointCloudVisualizationObject pointCloud;
        private CameraIntrinsicsWithPoseVisualizationObject frustum;

        private int sparsity = 3;
        private Shared<DepthImage> depthImage = null;
        private ICameraIntrinsics intrinsics = null;
        private Point3D[,] cameraSpaceMapping = null;
        private CoordinateSystem position = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageCameraViewAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public DepthImageCameraViewAsPointCloudVisualizationObject()
        {
            this.PointCloud = new Point3DListAsPointCloudVisualizationObject();
            this.SpatialCamera = new CameraIntrinsicsWithPoseVisualizationObject();
        }

        /// <summary>
        /// Gets or sets the visualization object for the frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Frustum")]
        [Description("The frustum representing the spatial depth camera.")]
        public CameraIntrinsicsWithPoseVisualizationObject SpatialCamera
        {
            get { return this.frustum; }
            set { this.Set(nameof(this.SpatialCamera), ref this.frustum, value); }
        }

        /// <summary>
        /// Gets or sets the visualization object for the point cloud.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Point Cloud")]
        [Description("The 3D point cloud representing the depth image.")]
        public Point3DListAsPointCloudVisualizationObject PointCloud
        {
            get { return this.pointCloud; }
            set { this.Set(nameof(this.PointCloud), ref this.pointCloud, value); }
        }

        /// <summary>
        /// Gets or sets the point cloud sparsity.
        /// </summary>
        [DataMember]
        [DisplayName("Point Cloud Sparsity")]
        [Description("The sparsity (in pixels) of the point cloud (min=1).")]
        public int Sparsity
        {
            get { return this.sparsity; }
            set { this.Set(nameof(this.Sparsity), ref this.sparsity, value > 0 ? value : 1); }
        }

        /// <inheritdoc/>
        protected override Action<DepthImageCameraView> Deallocator => data => data.ViewedObject?.Dispose();

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = base.ContextMenuItemsInfo();
            items.Insert(
                0,
                new ContextMenuItemInfo(
                    null,
                    "Export Point Cloud",
                    new VisualizationCommand(this.ExportPointCloudToPly),
                    isEnabled: this.PointCloud.CurrentData != null));
            return items;
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.depthImage != null)
            {
                this.depthImage.Dispose();
            }

            this.depthImage = this.CurrentData?.ViewedObject?.AddRef();
            if (!Equals(this.intrinsics, this.CurrentData?.CameraIntrinsics))
            {
                this.intrinsics = this.CurrentData?.CameraIntrinsics;
                this.cameraSpaceMapping = this.intrinsics?.GetPixelToCameraSpaceMapping(this.depthImage.Resource.DepthValueSemantics, true);
            }

            this.position = this.CurrentData?.CameraPose;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Sparsity))
            {
                this.UpdateDepthImagePointCloud();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.frustum.SetCurrentValue(this.SynthesizeMessage((this.intrinsics, this.position)));
            this.UpdateDepthImagePointCloud();
        }

        private void UpdateDepthImagePointCloud()
        {
            if (this.depthImage == null || this.depthImage.Resource == null || this.intrinsics == null || this.position == null)
            {
                this.PointCloud.SetCurrentValue(this.SynthesizeMessage<List<Windows.Point3D>>(null));
                return;
            }

            int width = this.depthImage.Resource.Width;
            int height = this.depthImage.Resource.Height;
            var pointsArray = new Windows.Point3D[width, height];
            var points = new List<Windows.Point3D>();

            double scale = this.depthImage.Resource.DepthValueToMetersScaleFactor;
            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)this.depthImage.Resource.ImageData.ToPointer());

                Parallel.For(0, height, iy =>
                {
                    var row = iy * width;
                    var point = Vector<double>.Build.Dense(4);
                    point[3] = 1;
                    for (int ix = 0; ix < width; ix += this.sparsity)
                    {
                        int i = row + ix;
                        var d = depthFrame[i];
                        if (d != 0)
                        {
                            var dscaled = d * scale;
                            var cameraSpacePoint = this.cameraSpaceMapping[ix, iy];
                            point[0] = dscaled * cameraSpacePoint.X;
                            point[1] = dscaled * cameraSpacePoint.Y;
                            point[2] = dscaled * cameraSpacePoint.Z;
                            var transformed = this.position.Multiply(point);
                            pointsArray[ix, iy] = new Windows.Point3D(transformed[0], transformed[1], transformed[2]);
                        }
                    }
                });

                for (int iy = 0; iy < height; iy += this.sparsity)
                {
                    var row = iy * width;
                    for (int ix = 0; ix < width; ix += this.sparsity)
                    {
                        int i = row + ix;
                        if (depthFrame[i] != 0)
                        {
                            points.Add(pointsArray[ix, iy]);
                        }
                    }
                }
            }

            this.PointCloud.SetCurrentValue(this.SynthesizeMessage(points));
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.pointCloud.ModelView, this.Visible && this.CurrentData != default && this.depthImage != null && this.depthImage.Resource != null && this.intrinsics != null && this.position != null);
            this.UpdateChildVisibility(this.frustum.ModelView, this.Visible && this.CurrentData != default && this.intrinsics != null && this.position != null);
        }

        private void ExportPointCloudToPly()
        {
            // Suggest filename of <StreamName>_<PointCloudTimeInTicks>.ply
            string exportFilePath = $"{this.StreamBinding.StreamName}_{this.PointCloud.CurrentOriginatingTime.Ticks}.ply";

            // Allow user to modify location and name of file
            var saveFileDialog = new SaveFileDialog
            {
                FileName = exportFilePath,
                DefaultExt = ".ply",
                Filter = "Polygon File Format|*.ply",
            };

            bool? result = saveFileDialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                exportFilePath = saveFileDialog.FileName;

                try
                {
                    var points = this.PointCloud.CurrentData;
                    using var writer = File.CreateText(exportFilePath);

                    // Header
                    writer.WriteLine("ply");
                    writer.WriteLine("format ascii 1.0");
                    writer.WriteLine($"element vertex {points.Count}");
                    writer.WriteLine("property float x");
                    writer.WriteLine("property float y");
                    writer.WriteLine("property float z");
                    writer.WriteLine("end_header");

                    // Vertices
                    foreach (var point in points)
                    {
                        writer.WriteLine($"{point.X} {point.Y} {point.Z}");
                    }

                    // Show where the file was written
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Export Point Cloud to PLY File",
                        $"Point cloud saved to {exportFilePath}",
                        "Close",
                        null).ShowDialog();
                }
                catch (Exception e)
                {
                    var exception = e.InnerException ?? e;
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Error",
                        exception.Message,
                        "Close",
                        null).ShowDialog();
                }
            }
        }
    }
}
