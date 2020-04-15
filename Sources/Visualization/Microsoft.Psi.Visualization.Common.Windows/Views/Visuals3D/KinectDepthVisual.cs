// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a Kinect depth visual.
    /// </summary>
    public class KinectDepthVisual : ModelVisual3D
    {
        private KinectDepth3DVisualizationObject visualizationObject;
        private MeshGeometry3D meshGeometry;
        private Point3D[] depthFramePoints;
        private int[] rawDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectDepthVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The Kinect depth 3D visualization object.</param>
        public KinectDepthVisual(KinectDepth3DVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        private void UpdateMesh(Image depthImage)
        {
            if (this.depthFramePoints?.Length != (depthImage.Width * depthImage.Height))
            {
                this.rawDepth = new int[depthImage.Width * depthImage.Height];
                this.depthFramePoints = new Point3D[depthImage.Width * depthImage.Height];
            }

            this.UpdateDepth(depthImage);
            this.CreateMesh(depthImage.Width, depthImage.Height);
        }

        private void UpdateTransform()
        {
            var resource = this.visualizationObject.CurrentValue.GetValueOrDefault().Data?.Resource;
            if (resource != null)
            {
                this.UpdateMesh(resource);
            }
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.visualizationObject.CurrentValue))
            {
                this.UpdateTransform();
            }
            else if (e.PropertyName == nameof(this.visualizationObject.Color))
            {
                var material = new DiffuseMaterial(new SolidColorBrush(this.visualizationObject.Color));
                (this.Content as GeometryModel3D).Material = material;
                (this.Content as GeometryModel3D).BackMaterial = material;
            }
        }

        private double ConvertRawDepthToMeters(int rawDepth)
        {
            // http://nicolas.burrus.name/index.php/Research/KinectCalibration
            // http://www.ros.org/wiki/kinect_node
            if (rawDepth < 2047)
            {
                return 1.0 / ((rawDepth * -0.0030711016) + 3.3309495161);
            }

            return 0;
        }

        private void CreateMesh(int width, int height, double depthDifferenceTolerance = 200)
        {
            this.meshGeometry = new MeshGeometry3D();
            var triangleIndices = new List<int>();
            for (int iy = 0; iy + 1 < height; iy++)
            {
                for (int ix = 0; ix + 1 < width; ix++)
                {
                    int i0 = (iy * width) + ix;
                    int i1 = (iy * width) + ix + 1;
                    int i2 = ((iy + 1) * width) + ix + 1;
                    int i3 = ((iy + 1) * width) + ix;

                    var d0 = this.rawDepth[i0];
                    var d1 = this.rawDepth[i1];
                    var d2 = this.rawDepth[i2];
                    var d3 = this.rawDepth[i3];

                    var dmax0 = Math.Max(Math.Max(d0, d1), d2);
                    var dmin0 = Math.Min(Math.Min(d0, d1), d2);
                    var dmax1 = Math.Max(d0, Math.Max(d2, d3));
                    var dmin1 = Math.Min(d0, Math.Min(d2, d3));

                    if (dmax0 - dmin0 < depthDifferenceTolerance && dmin0 != -1)
                    {
                        triangleIndices.Add(i0);
                        triangleIndices.Add(i1);
                        triangleIndices.Add(i2);
                    }

                    if (dmax1 - dmin1 < depthDifferenceTolerance && dmin1 != -1)
                    {
                        triangleIndices.Add(i0);
                        triangleIndices.Add(i2);
                        triangleIndices.Add(i3);
                    }
                }
            }

            this.meshGeometry.TriangleIndices = new Int32Collection(triangleIndices);
            this.meshGeometry.Positions = new Point3DCollection(this.depthFramePoints);

            var material = new DiffuseMaterial(new SolidColorBrush(this.visualizationObject.Color));
            this.Content = new GeometryModel3D(this.meshGeometry, material);
            (this.Content as GeometryModel3D).BackMaterial = material;
        }

        private void UpdateDepth(Image depthImage)
        {
            int width = depthImage.Width;
            int height = depthImage.Height;

            ushort tooNearDepth = 500;
            ushort tooFarDepth = 10000;
            ushort unknownDepth = 0;

            int cx = width / 2;
            int cy = height / 2;

            double fxinv = 1.0 / 366;
            double fyinv = 1.0 / 366;

            double scale = 0.001;

            unsafe
            {
                ushort* depthFrame = (ushort*)((byte*)depthImage.ImageData.ToPointer());

                Parallel.For(0, height, iy =>
                {
                    for (int ix = 0; ix < width; ix++)
                    {
                        int i = (iy * width) + ix;
                        this.rawDepth[i] = depthFrame[(iy * width) + ix];

                        if (this.rawDepth[i] == unknownDepth || this.rawDepth[i] < tooNearDepth || this.rawDepth[i] > tooFarDepth)
                        {
                            this.rawDepth[i] = -1;
                            this.depthFramePoints[i] = default(Point3D);
                        }
                        else
                        {
                            double zz = (double)this.rawDepth[i] * scale;
                            double x = (cx - ix) * zz * fxinv;
                            double y = zz;
                            double z = (cy - iy) * zz * fyinv;
                            this.depthFramePoints[i] = new Point3D(x, y, z);
                        }
                    }
                });
            }
        }
    }
}