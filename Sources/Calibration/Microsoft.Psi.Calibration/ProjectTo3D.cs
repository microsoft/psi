// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that projects 2D color-space points into 3D camera-space points in the depth camera's coordinate system.
    /// </summary>
    /// <remarks>
    /// Inputs are the depth image, list of 2D points from the color image, and the camera calibration.
    /// Outputs the 3D points projected into the depth camera's coordinate system.
    /// </remarks>
    public sealed class ProjectTo3D : ConsumerProducer<(Shared<DepthImage>, List<Point2D>, IDepthDeviceCalibrationInfo), List<Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectTo3D"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public ProjectTo3D(Pipeline pipeline, string name = nameof(ProjectTo3D))
            : base(pipeline, name)
        {
        }

        /// <inheritdoc/>
        protected override void Receive((Shared<DepthImage>, List<Point2D>, IDepthDeviceCalibrationInfo) data, Envelope e)
        {
            var point2DList = data.Item2;
            var depthImage = data.Item1;
            var calibration = data.Item3;
            List<Point3D> point3DList = new List<Point3D>();

            if (calibration != null)
            {
                foreach (var point2D in point2DList)
                {
                    var result = CalibrationExtensions.ProjectToCameraSpace(calibration, point2D, depthImage);
                    if (result != null)
                    {
                        point3DList.Add(result.Value);
                    }
                }

                this.Out.Post(point3DList, e.OriginatingTime);
            }
        }
    }
}
