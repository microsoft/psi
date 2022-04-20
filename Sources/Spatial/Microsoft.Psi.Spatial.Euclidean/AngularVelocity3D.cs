// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using static Microsoft.Psi.Calibration.CalibrationExtensions;

    /// <summary>
    /// Represents an angular 3D velocity, starting from an original rotation,
    /// around a particular axis of rotation.
    /// </summary>
    public readonly struct AngularVelocity3D : IEquatable<AngularVelocity3D>
    {
        /// <summary>
        /// The origin of rotation.
        /// </summary>
        public readonly Matrix<double> OriginRotation;

        /// <summary>
        /// The axis of angular velocity, along with the radians/time speed (length of the vector).
        /// </summary>
        public readonly Vector3D AxisAngleVector;

        /// <summary>
        /// Initializes a new instance of the <see cref="AngularVelocity3D"/> struct.
        /// </summary>
        /// <param name="originRotation">The origin of rotation.</param>
        /// <param name="axisAngleVector">The axis-angle representation of velocity.</param>
        public AngularVelocity3D(Matrix<double> originRotation, Vector3D axisAngleVector)
        {
            if (originRotation.RowCount != 3 ||
                originRotation.ColumnCount != 3)
            {
                throw new ArgumentException("Rotation matrix must be 3x3.");
            }

            this.OriginRotation = originRotation;
            this.AxisAngleVector = axisAngleVector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngularVelocity3D"/> struct.
        /// </summary>
        /// <param name="originRotation">The origin of rotation.</param>
        /// <param name="axis">The axis of velocity.</param>
        /// <param name="speed">The angular speed (radians/time).</param>
        public AngularVelocity3D(Matrix<double> originRotation, UnitVector3D axis, double speed)
            : this(originRotation, axis.ScaleBy(speed))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngularVelocity3D"/> struct.
        /// </summary>
        /// <param name="originRotation">The origin of rotation.</param>
        /// <param name="destinationRotation">A destination rotation.</param>
        /// <param name="time">The time it took to reach the destination rotation.</param>
        /// <param name="angleEpsilon">An optional angle epsilon parameter used to determine when the specified matrix contains a zero-rotation (by default 0.01 degrees).</param>
        public AngularVelocity3D(Matrix<double> originRotation, Matrix<double> destinationRotation, TimeSpan time, double angleEpsilon = 0.01 * Math.PI / 180)
        {
            if (originRotation.RowCount != 3 ||
                originRotation.ColumnCount != 3 ||
                destinationRotation.RowCount != 3 ||
                destinationRotation.ColumnCount != 3)
            {
                throw new ArgumentException("Rotation matrices must be 3x3.");
            }

            this.OriginRotation = originRotation;
            var axisAngleDistance = Vector3D.OfVector(MatrixToAxisAngle(destinationRotation * originRotation.Inverse(), angleEpsilon));
            var angularSpeed = axisAngleDistance.Length / time.TotalSeconds;
            this.AxisAngleVector = angularSpeed == 0 ? default : axisAngleDistance.Normalize().ScaleBy(angularSpeed);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngularVelocity3D"/> struct.
        /// </summary>
        /// <param name="originCoordinateSystem">The origin coordinate system.</param>
        /// <param name="destinationCoordinateSystem">The destination coordinate system.</param>
        /// <param name="time">The time it took to reach the destination coordinate system.</param>
        /// <param name="angleEpsilon">An optional angle epsilon parameter used to determine when the specified matrix contains a zero-rotation (by default 0.01 degrees).</param>
        public AngularVelocity3D(
            CoordinateSystem originCoordinateSystem,
            CoordinateSystem destinationCoordinateSystem,
            TimeSpan time,
            double angleEpsilon = 0.01 * Math.PI / 180)
            : this(originCoordinateSystem.GetRotationSubMatrix(), destinationCoordinateSystem.GetRotationSubMatrix(), time, angleEpsilon)
        {
        }

        /// <summary>
        /// Gets the magnitude of the velocity.
        /// </summary>
        public double Speed => this.AxisAngleVector.Length;

        /// <summary>
        /// Returns a value indicating whether the specified velocities are the same.
        /// </summary>
        /// <param name="left">The first velocity.</param>
        /// <param name="right">The second velocity.</param>
        /// <returns>True if the velocities are the same; otherwise false.</returns>
        public static bool operator ==(AngularVelocity3D left, AngularVelocity3D right) => left.Equals(right);

        /// <summary>
        /// Returns a value indicating whether the specified velocities are different.
        /// </summary>
        /// <param name="left">The first velocity.</param>
        /// <param name="right">The second velocity.</param>
        /// <returns>True if the velocities are different; otherwise false.</returns>
        public static bool operator !=(AngularVelocity3D left, AngularVelocity3D right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(AngularVelocity3D other) => this.OriginRotation.Equals(other.OriginRotation) && this.AxisAngleVector == other.AxisAngleVector;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is AngularVelocity3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.OriginRotation, this.AxisAngleVector);

        /// <summary>
        /// Computes the destination rotation, if this velocity is followed for a given amount of time.
        /// </summary>
        /// <param name="time">The span of time to compute over.</param>
        /// <returns>The destination rotation.</returns>
        /// <remarks>The unit of time should be the same as assumed for the axis-angle velocity vector (e.g., seconds).</remarks>
        public Matrix<double> ComputeDestination(double time)
        {
            var angularDistance = this.AxisAngleVector.Length * time;
            var axisAngleDistance = this.AxisAngleVector.Normalize().ScaleBy(angularDistance);
            return AxisAngleToMatrix(axisAngleDistance.ToVector()) * this.OriginRotation;
        }
    }
}
