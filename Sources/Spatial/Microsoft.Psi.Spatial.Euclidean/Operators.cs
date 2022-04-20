// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements various operators for manipulating euclidean entities.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Transforms a point cloud by a coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <param name="pointCloud3D">The point cloud.</param>
        /// <returns>The transformed point cloud.</returns>
        public static PointCloud3D Transform(this CoordinateSystem coordinateSystem, PointCloud3D pointCloud3D)
            => pointCloud3D.TransformBy(coordinateSystem);

        /// <summary>
        /// Transforms a rectangle by a coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <param name="rectangle3D">The rectangle.</param>
        /// <returns>The transformed rectangle.</returns>
        public static Rectangle3D Transform(this CoordinateSystem coordinateSystem, Rectangle3D rectangle3D)
        {
            var newBottomLeft = rectangle3D.BottomLeft.TransformBy(coordinateSystem);
            var newBottomRight = rectangle3D.BottomRight.TransformBy(coordinateSystem);
            var newTopLeft = rectangle3D.TopLeft.TransformBy(coordinateSystem);
            var widthAxis = newBottomRight - newBottomLeft;
            var heightAxis = newTopLeft - newBottomLeft;
            var width = widthAxis.Length;
            var height = heightAxis.Length;
            return new Rectangle3D(newBottomLeft, widthAxis.Normalize(), heightAxis.Normalize(), 0, 0, width, height);
        }

        /// <summary>
        /// Transforms a box by a coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <param name="box3D">The box.</param>
        /// <returns>The transformed box.</returns>
        public static Box3D Transform(this CoordinateSystem coordinateSystem, Box3D box3D)
            => box3D.TransformBy(coordinateSystem);

        /// <summary>
        /// Computes the linear velocity of a coordinate system.
        /// </summary>
        /// <param name="source">The source stream of coordinate systems.</param>
        /// <param name="deliveryPolicy">An optional delivery policy parameter.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the linear velocity of the specified point.</returns>
        public static IProducer<LinearVelocity3D?> GetLinearVelocity3D(this IProducer<CoordinateSystem> source, DeliveryPolicy<CoordinateSystem> deliveryPolicy = null, string name = nameof(GetLinearVelocity3D))
            => source.GetLinearVelocity3D(cs => cs?.Origin, deliveryPolicy, name);

        /// <summary>
        /// Computes the linear velocity of an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="source">The source stream of points.</param>
        /// <param name="getLocation">A function that specifies the location of the object.</param>
        /// <param name="deliveryPolicy">An optional delivery policy parameter.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the linear velocity of the specified point.</returns>
        public static IProducer<LinearVelocity3D?> GetLinearVelocity3D<T>(this IProducer<T> source, Func<T, Point3D?> getLocation, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(GetLinearVelocity3D))
        {
            var lastPoint3D = default(Point3D?);
            var lastDateTime = DateTime.MinValue;
            return source.Process<T, LinearVelocity3D?>(
                (t, envelope, emitter) =>
                {
                    var point3D = getLocation(t);
                    if (point3D.HasValue && lastPoint3D.HasValue && lastDateTime > DateTime.MinValue)
                    {
                        var velocity3D = new LinearVelocity3D(point3D.Value, (point3D.Value - lastPoint3D.Value).Normalize(), point3D.Value.DistanceTo(lastPoint3D.Value) / (envelope.OriginatingTime - lastDateTime).TotalSeconds);
                        emitter.Post(velocity3D, envelope.OriginatingTime);
                    }

                    lastPoint3D = point3D;
                    lastDateTime = envelope.OriginatingTime;
                },
                deliveryPolicy,
                name);
        }

        /// <summary>
        /// Gets a rotation matrix corresponding to a forward vector.
        /// </summary>
        /// <param name="forward">The specified forward vector.</param>
        /// <returns>The corresponding rotation matrix.</returns>
        /// <remarks>The X axis of the matrix will correspond to the specified forward vector.</remarks>
        public static Matrix<double> ToRotationMatrix(this Vector3D forward) => forward.Normalize().ToRotationMatrix();

        /// <summary>
        /// Gets a rotation matrix corresponding to a forward vector.
        /// </summary>
        /// <param name="forward">The specified forward vector.</param>
        /// <returns>The corresponding rotation matrix.</returns>
        /// <remarks>The X axis of the matrix will correspond to the specified forward vector.</remarks>
        public static Matrix<double> ToRotationMatrix(this UnitVector3D forward)
        {
            // Compute left and up directions from the given forward direction.
            var left = UnitVector3D.ZAxis.CrossProduct(forward);
            var up = forward.CrossProduct(left);

            // Create a corresponding 3x3 matrix from the 3 directions.
            var rotationMatrix = Matrix<double>.Build.Dense(3, 3);
            rotationMatrix.SetColumn(0, forward.ToVector());
            rotationMatrix.SetColumn(1, left.ToVector());
            rotationMatrix.SetColumn(2, up.ToVector());
            return rotationMatrix;
        }
    }
}
