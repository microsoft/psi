// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;

    /// <summary>
    /// Represents a camera view of an object.
    /// </summary>
    /// <typeparam name="T">Type of view.</typeparam>
    public class CameraView<T>
    {
        /// <summary>
        /// The private pose (used to determine if the Pose has been mutated and update the inversePose).
        /// </summary>
        [NonSerialized]
        private CoordinateSystem cameraPose = null;

        /// <summary>
        /// The inverse pose (cached for efficiently resolving GetPixelPosition queries).
        /// </summary>
        [NonSerialized]
        private CoordinateSystem inverseCameraPose = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraView{T}"/> class.
        /// </summary>
        /// <param name="viewedObject">The viewed object.</param>
        /// <param name="cameraIntrinsics">The camera intrinsics.</param>
        /// <param name="cameraPose">The camera pose.</param>
        public CameraView(T viewedObject, ICameraIntrinsics cameraIntrinsics, CoordinateSystem cameraPose)
        {
            this.ViewedObject = viewedObject;
            this.CameraIntrinsics = cameraIntrinsics;
            this.CameraPose = cameraPose;
        }

        /// <summary>
        /// Gets the viewed object.
        /// </summary>
        public T ViewedObject { get; private set; }

        /// <summary>
        /// Gets the camera intrinsics.
        /// </summary>
        public ICameraIntrinsics CameraIntrinsics { get; private set; }

        /// <summary>
        /// Gets the camera pose.
        /// </summary>
        public CoordinateSystem CameraPose { get; private set; }

        /// <summary>
        /// Gets the corresponding pixel position for a point in 3D space.
        /// </summary>
        /// <param name="point3D">Point in 3D space, assuming MathNet basis (Forward=X, Left=Y, Up=Z), and the coordinate system of the camera.</param>
        /// <param name="distort">Indicates whether to apply distortion.</param>
        /// <param name="nullIfOutsideFieldOfView">Optional flag indicating whether to return null if point is outside the field of view (default true).</param>
        /// <returns>Point containing the pixel position.</returns>
        public Point2D? GetPixelPosition(Point3D point3D, bool distort, bool nullIfOutsideFieldOfView = true)
        {
            if (this.CameraIntrinsics == null || this.CameraPose == null)
            {
                return default;
            }
            else
            {
                if (!Equals(this.CameraPose, this.cameraPose))
                {
                    this.CameraPose.DeepClone(ref this.cameraPose);
                    this.CameraPose.Invert().DeepClone(ref this.inverseCameraPose);
                }

                return this.CameraIntrinsics.GetPixelPosition(this.inverseCameraPose.Transform(point3D), distort, nullIfOutsideFieldOfView);
            }
        }

        /// <summary>
        /// Creates a stream of views from a stream of objects, a stream of camera intrinsics, and a stream of poses.
        /// </summary>
        /// <typeparam name="TViewedObject">Type of viewed object.</typeparam>
        /// <typeparam name="TCameraView">Type of camera view.</typeparam>
        /// <param name="ctor">Camera view constructor.</param>
        /// <param name="viewedObject">A stream of viewed objects.</param>
        /// <param name="cameraIntrinsics">A stream of camera intrinsics.</param>
        /// <param name="cameraPose">A stream of camera poses.</param>
        /// <param name="cameraIntrinsicsInterpolator">The interpolator for the camera intrinsics stream.</param>
        /// <param name="cameraPoseInterpolator">The interpolator for the camera pose stream.</param>
        /// <param name="viewedObjectDeliveryPolicy">An optional delivery policy for the viewed object stream.</param>
        /// <param name="cameraIntrinsicsDeliveryPolicy">An optional delivery policy for the camera intrinsics stream.</param>
        /// <param name="cameraPoseDeliveryPolicy">An optional delivery policy for the camera pose stream.</param>
        /// <param name="viewedObjectAndCameraIntrinsicsDeliveryPolicy">An optional delivery policy for the tuple of viewed object and camera intrinsics stream.</param>
        /// <returns>Created camera view stream.</returns>
        protected static IProducer<TCameraView> CreateProducer<TViewedObject, TCameraView>(
            Func<TViewedObject, ICameraIntrinsics, CoordinateSystem, TCameraView> ctor,
            IProducer<TViewedObject> viewedObject,
            IProducer<ICameraIntrinsics> cameraIntrinsics,
            IProducer<CoordinateSystem> cameraPose,
            Interpolator<ICameraIntrinsics> cameraIntrinsicsInterpolator,
            Interpolator<CoordinateSystem> cameraPoseInterpolator,
            DeliveryPolicy<TViewedObject> viewedObjectDeliveryPolicy = null,
            DeliveryPolicy<ICameraIntrinsics> cameraIntrinsicsDeliveryPolicy = null,
            DeliveryPolicy<CoordinateSystem> cameraPoseDeliveryPolicy = null,
            DeliveryPolicy<(TViewedObject, ICameraIntrinsics)> viewedObjectAndCameraIntrinsicsDeliveryPolicy = null)
        {
            return viewedObject
                .Fuse(
                    cameraIntrinsics,
                    cameraIntrinsicsInterpolator,
                    viewedObjectDeliveryPolicy,
                    cameraIntrinsicsDeliveryPolicy)
                .Fuse(
                    cameraPose,
                    cameraPoseInterpolator,
                    viewedObjectAndCameraIntrinsicsDeliveryPolicy,
                    cameraPoseDeliveryPolicy)
                .Select(
                    tuple =>
                    {
                        (var image, var intrinsics, var pose) = tuple;
                        return ctor(image, intrinsics, pose);
                    },
                    DeliveryPolicy.SynchronousOrThrottle);
        }
    }
}
