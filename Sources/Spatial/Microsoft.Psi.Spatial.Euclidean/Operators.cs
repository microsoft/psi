// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Linq;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Quaternion = System.Numerics.Quaternion;

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
        /// Computes the velocity (with linear and angular components) of streaming <see cref="CoordinateSystem"/> poses.
        /// </summary>
        /// <param name="source">The source stream of <see cref="CoordinateSystem"/> poses.</param>
        /// <param name="deliveryPolicy">An optional delivery policy parameter.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the computed velocities.</returns>
        public static IProducer<CoordinateSystemVelocity3D> ComputeVelocity(
            this IProducer<CoordinateSystem> source,
            DeliveryPolicy<CoordinateSystem> deliveryPolicy = null,
            string name = nameof(ComputeVelocity))
                => source.Window(
                    -1,
                    0,
                    values => new CoordinateSystemVelocity3D(values.First().Data, values.Last().Data, values.Last().OriginatingTime - values.First().OriginatingTime),
                    deliveryPolicy,
                    name);

        /// <summary>
        /// Gets a rotation matrix corresponding to a forward vector.
        /// </summary>
        /// <param name="forwardVector3D">The specified forward vector.</param>
        /// <param name="worldUpVector3D">Optional world "up" vector to use as reference when computing the rotation.
        /// Defaults to <see cref="UnitVector3D.ZAxis"/>.</param>
        /// <returns>The corresponding rotation matrix.</returns>
        /// <remarks>The X axis of the matrix will correspond to the specified forward vector.</remarks>
        public static Matrix<double> ToRotationMatrix(this UnitVector3D forwardVector3D, UnitVector3D? worldUpVector3D = null)
        {
            worldUpVector3D ??= UnitVector3D.ZAxis;

            // Compute local left and up vectors from the given forward vector and world up vector.
            var left = worldUpVector3D.Value.CrossProduct(forwardVector3D);
            var up = forwardVector3D.CrossProduct(left);

            // Create a corresponding 3x3 matrix from the 3 directions.
            var rotationMatrix = Matrix<double>.Build.Dense(3, 3);
            rotationMatrix.SetColumn(0, forwardVector3D.ToVector());
            rotationMatrix.SetColumn(1, left.ToVector());
            rotationMatrix.SetColumn(2, up.ToVector());
            return rotationMatrix;
        }

        /// <summary>
        /// Convert a <see cref="CoordinateSystem"/> to the equivalent <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> to convert.</param>
        /// <returns>The equivalent <see cref="Matrix4x4"/>.</returns>
        public static Matrix4x4 ToSystemNumericsMatrix(this CoordinateSystem coordinateSystem)
            => new ((float)coordinateSystem.Values[0],
                    (float)coordinateSystem.Values[1],
                    (float)coordinateSystem.Values[2],
                    (float)coordinateSystem.Values[3],
                    (float)coordinateSystem.Values[4],
                    (float)coordinateSystem.Values[5],
                    (float)coordinateSystem.Values[6],
                    (float)coordinateSystem.Values[7],
                    (float)coordinateSystem.Values[8],
                    (float)coordinateSystem.Values[9],
                    (float)coordinateSystem.Values[10],
                    (float)coordinateSystem.Values[11],
                    (float)coordinateSystem.Values[12],
                    (float)coordinateSystem.Values[13],
                    (float)coordinateSystem.Values[14],
                    (float)coordinateSystem.Values[15]);

        /// <summary>
        /// Convert a <see cref="Matrix4x4"/> to the equivalent <see cref="CoordinateSystem"/>.
        /// </summary>
        /// <param name="matrix">The <see cref="Matrix4x4"/> to convert.</param>
        /// <returns>The equivalent <see cref="CoordinateSystem"/>.</returns>
        public static CoordinateSystem ToCoordinateSystem(this Matrix4x4 matrix)
        {
            var values = new double[]
            {
                matrix.M11,
                matrix.M12,
                matrix.M13,
                matrix.M14,
                matrix.M21,
                matrix.M22,
                matrix.M23,
                matrix.M24,
                matrix.M31,
                matrix.M32,
                matrix.M33,
                matrix.M34,
                matrix.M41,
                matrix.M42,
                matrix.M43,
                matrix.M44,
            };

            return new CoordinateSystem(Matrix<double>.Build.Dense(4, 4, values));
        }

        /// <summary>
        /// Interpolates between two <see cref="CoordinateSystem"/> poses, using spherical linear interpolation
        /// (<see cref="Quaternion.Slerp"/>) for rotation, and linear interpolation for translation.
        /// </summary>
        /// <param name="cs1">The first <see cref="CoordinateSystem"/>.</param>
        /// <param name="cs2">The second <see cref="CoordinateSystem"/>.</param>
        /// <param name="amount">The amount to interpolate between the two coordinate systems.
        /// A value between 0 and 1 will return an interpolation between the two values.
        /// A value outside the 0-1 range will generate an extrapolated result.</param>
        /// <returns>The interpolated <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>Returns null if either input value is null.</remarks>
        public static CoordinateSystem InterpolateCoordinateSystems(CoordinateSystem cs1, CoordinateSystem cs2, double amount)
        {
            if (cs1 is null || cs2 is null)
            {
                return null;
            }

            // Extract translation as vectors
            var t1 = cs1.Origin.ToVector3D();
            var t2 = cs2.Origin.ToVector3D();

            // Extract rotation as quaternions (zeroing out the translation)
            var r1 = Quaternion.CreateFromRotationMatrix(cs1.SetTranslation(new Vector3D(0, 0, 0)).ToSystemNumericsMatrix());
            var r2 = Quaternion.CreateFromRotationMatrix(cs2.SetTranslation(new Vector3D(0, 0, 0)).ToSystemNumericsMatrix());

            // interpolate
            var t = t1 + (amount * (t2 - t1));
            var r = Quaternion.Slerp(r1, r2, (float)amount);
            return Matrix4x4.CreateFromQuaternion(r).ToCoordinateSystem().SetTranslation(t);
        }
    }
}