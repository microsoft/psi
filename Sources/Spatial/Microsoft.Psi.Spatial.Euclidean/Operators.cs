// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Linq;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Plane = MathNet.Spatial.Euclidean.Plane;
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
                    if (point3D.HasValue &&
                        lastPoint3D.HasValue &&
                        !point3D.Value.ToVector().Any(d => double.IsNaN(d)) &&
                        !lastPoint3D.Value.ToVector().Any(d => double.IsNaN(d)) &&
                        lastDateTime > DateTime.MinValue)
                    {
                        var distance = point3D.Value.DistanceTo(lastPoint3D.Value);
                        UnitVector3D direction;
                        double magnitude;
                        if (distance < float.Epsilon)
                        {
                            direction = default;
                            magnitude = 0;
                        }
                        else
                        {
                            direction = (point3D.Value - lastPoint3D.Value).Normalize();
                            magnitude = distance / (envelope.OriginatingTime - lastDateTime).TotalSeconds;
                        }

                        var velocity3D = new LinearVelocity3D(point3D.Value, direction, magnitude);
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
                => source.Where(cs => cs != null, deliveryPolicy).Window(
                    -1,
                    0,
                    values => new CoordinateSystemVelocity3D(values.First().Data, values.Last().Data, values.Last().OriginatingTime - values.First().OriginatingTime),
                    DeliveryPolicy.SynchronousOrThrottle,
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
        /// Interpolates between two <see cref="Ray3D"/> rays.
        /// </summary>
        /// <param name="ray3D1">The first <see cref="Ray3D"/>.</param>
        /// <param name="ray3D2">The second <see cref="Ray3D"/>.</param>
        /// <param name="amount">The amount to interpolate between the two <see cref="Ray3D"/> objects.
        /// A value between 0 and 1 will return an interpolation between the two values.
        /// A value outside the 0-1 range will generate an extrapolated result.</param>
        /// <returns>The interpolated <see cref="Ray3D"/>.</returns>
        public static Ray3D InterpolateRay3D(Ray3D ray3D1, Ray3D ray3D2, double amount)
            => new (
                new Point3D(
                    (ray3D1.ThroughPoint.X * (1 - amount)) + (ray3D2.ThroughPoint.X * amount),
                    (ray3D1.ThroughPoint.Y * (1 - amount)) + (ray3D2.ThroughPoint.Y * amount),
                    (ray3D1.ThroughPoint.Z * (1 - amount)) + (ray3D2.ThroughPoint.Z * amount)),
                (ray3D1.Direction.ScaleBy(1 - amount) + ray3D2.Direction.ScaleBy(amount)).Normalize());

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

        /// <summary>
        /// Try to compute a plane from three points.
        /// </summary>
        /// <param name="point1">First point.</param>
        /// <param name="point2">Second point.</param>
        /// <param name="point3">Third point.</param>
        /// <param name="plane">(out) The computed plane.</param>
        /// <returns>True if successful, or false if the three points are collinear.</returns>
        public static bool TryComputePlane(Point3D point1, Point3D point2, Point3D point3, out Plane plane)
        {
            try
            {
                plane = Plane.FromPoints(point1, point2, point3);
                return true;
            }
            catch (ArgumentException)
            {
                // This will fail if all three points are on a line
                plane = default;
                return false;
            }
        }

        /// <summary>
        /// Use barycentric coordinates to determine whether a triangle contains a point.
        /// The triangle is represented as three vertices.
        /// </summary>
        /// <param name="queryPoint">The point to test.</param>
        /// <param name="triangleVertexA">Triangle's first vertex.</param>
        /// <param name="triangleVertexB">Triangle's second vertex.</param>
        /// <param name="triangleVertexC">Triangle's third vertex.</param>
        /// <returns>True if the triangle contains the point.</returns>
        public static bool TriangleContainsPoint(Point3D queryPoint, Point3D triangleVertexA, Point3D triangleVertexB, Point3D triangleVertexC)
        {
            var v0 = triangleVertexC - triangleVertexA;
            var v1 = triangleVertexB - triangleVertexA;
            var v2 = queryPoint - triangleVertexA;

            // Compute barycentric coordinates
            var dot00 = v0.DotProduct(v0);
            var dot01 = v0.DotProduct(v1);
            var dot02 = v0.DotProduct(v2);
            var dot11 = v1.DotProduct(v1);
            var dot12 = v1.DotProduct(v2);
            var denominator = dot00 * dot11 - dot01 * dot01;
            var u = (dot11 * dot02 - dot01 * dot12) / denominator;
            var v = (dot00 * dot12 - dot01 * dot02) / denominator;

            return u >= 0 && v >= 0 && (u + v <= 1);
        }
    }
}