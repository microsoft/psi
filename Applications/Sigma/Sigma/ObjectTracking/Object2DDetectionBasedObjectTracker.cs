// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Component that implements an object tracker based on object 2D segmentation results.
    /// </summary>
    public class Object2DDetectionBasedObjectTracker : ConsumerProducer<(Object2DDetectionResults, ICameraIntrinsics, CoordinateSystem, DepthImageCameraView), Object3DTrackingResults>
    {
        private readonly int depthCloudSparsity;
        private readonly bool robustDepthPointsOnly;
        private readonly float scoreThreshold;
        private readonly double mergeInstanceThreshold;
        private readonly Object3DTrackingResults results = new ();

        private ICameraIntrinsics depthIntrinsics = null;
        private Point3D[,] depthIntrinsicsCameraSpaceMapping = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Object2DDetectionBasedObjectTracker"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="depthCloudSparsity">An optional parameter to specify how sparsely to sample depth pixels (by default 1).</param>
        /// <param name="robustDepthPointsOnly">An optional parameter that indicates to return only robust points (where the nearby depth estimates are not zero).</param>
        /// <param name="scoreThreshold">Threshold on detection score for considering results. Defaults to 0.5.</param>
        /// <param name="mergeInstanceThreshold">Distance threshold for merging two instance results together.</param>
        /// <param name="name">An optional name for the component.</param>
        public Object2DDetectionBasedObjectTracker(
            Pipeline pipeline,
            int depthCloudSparsity = 1,
            bool robustDepthPointsOnly = false,
            float scoreThreshold = 0.5f,
            double mergeInstanceThreshold = 0.2,
            string name = nameof(Object2DDetectionBasedObjectTracker))
            : base(pipeline, name)
        {
            this.depthCloudSparsity = depthCloudSparsity;
            this.robustDepthPointsOnly = robustDepthPointsOnly;
            this.scoreThreshold = scoreThreshold;
            this.mergeInstanceThreshold = mergeInstanceThreshold;
        }

        /// <inheritdoc />
        protected override void Receive((Object2DDetectionResults, ICameraIntrinsics, CoordinateSystem, DepthImageCameraView) data, Envelope envelope)
        {
            var (object2DDetectionResults, colorCameraIntrinsics, colorCameraPose, depthView) = data;

            // Check nulls...
            if (object2DDetectionResults?.Detections is null ||
                depthView?.ViewedObject?.Resource is null ||
                colorCameraIntrinsics is null ||
                colorCameraPose is null ||
                depthView.CameraIntrinsics is null ||
                depthView.CameraPose is null)
            {
                this.Out.Post(this.results, envelope.OriginatingTime);
                return;
            }

            // Update depth intrinsics if necessary
            if (!Equals(this.depthIntrinsics, depthView.CameraIntrinsics))
            {
                depthView.CameraIntrinsics.DeepClone(ref this.depthIntrinsics);
                this.depthIntrinsicsCameraSpaceMapping = depthView.CameraIntrinsics.GetPixelToCameraSpaceMapping(
                    depthView.ViewedObject.Resource.DepthValueSemantics,
                    true);
            }

            // Convert depth values into a point cloud in color camera space
            var depthPointsInColorCamera = PointCloud3D
                .FromDepthImage(depthView.ViewedObject, this.depthIntrinsicsCameraSpaceMapping, this.depthCloudSparsity, this.robustDepthPointsOnly)
                .TransformBy(depthView.CameraPose.TransformBy(colorCameraPose.Invert()));

            // Project to 2D; in future releases investigate whether we can construct this projection more efficiently
            var depthPointsProjected = depthPointsInColorCamera.Select(p => colorCameraIntrinsics.GetPixelPosition(p, true)).ToArray();

            foreach (var detection in object2DDetectionResults.Detections.Where(d => d.DetectionScore >= this.scoreThreshold))
            {
                // Compute a point cloud for the detection result
                var depthPointsInMask = depthPointsInColorCamera.Where((p, i) =>
                {
                    var projectedPoint = depthPointsProjected[i];
                    if (projectedPoint.HasValue)
                    {
                        var x = (int)(projectedPoint.Value.X - detection.BoundingBox.X);
                        var y = (int)(projectedPoint.Value.Y - detection.BoundingBox.Y);
                        if (y >= 0 && y < detection.Mask.Length &&
                            x >= 0 && x < detection.Mask[y].Length)
                        {
                            return detection.Mask[y][x] > 0.5;
                        }
                    }

                    return false;
                });

                if (depthPointsInMask.Any())
                {
                    var resultPointCloud = new PointCloud3D(depthPointsInMask).TransformBy(colorCameraPose);
                    var resultCentroid = resultPointCloud.Centroid;

                    // Check if there is already a close instance to this result
                    var minInstanceDistance = double.MaxValue;
                    var closestInstance = default(TrackedObject3D);
                    foreach (var instance in this.results.Detections.Where(d => d.Class == detection.Class))
                    {
                        var distance = resultCentroid.DistanceTo(instance.PointCloud.Centroid);
                        if (distance < minInstanceDistance)
                        {
                            minInstanceDistance = distance;
                            closestInstance = instance;
                        }
                    }

                    if (closestInstance != default)
                    {
                        // Merge with the close instance
                        closestInstance.PointCloud = resultPointCloud;
                    }
                    else
                    {
                        // Add a new instance
                        var newInstanceId = this.results.Detections.Count(d => d.Class == detection.Class).ToString();
                        this.results.Detections.Add(new TrackedObject3D()
                        {
                            Class = detection.Class,
                            InstanceId = newInstanceId,
                            PointCloud = resultPointCloud,
                            BoundingBox = default,
                            TrackingScore = detection.DetectionScore,
                        });
                    }
                }
            }

            this.Out.Post(this.results, envelope.OriginatingTime);
        }
    }
}
