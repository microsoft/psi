// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using static Microsoft.Psi.Calibration.CalibrationExtensions;

    /// <summary>
    /// Represents a coordinate system velocity from a particular starting pose,
    /// composing both a linear and angular velocity.
    /// </summary>
    public readonly struct CoordinateSystemVelocity3D : IEquatable<CoordinateSystemVelocity3D>
    {
        /// <summary>
        /// The origin coordinate system.
        /// </summary>
        public readonly CoordinateSystem OriginCoordinateSystem;

        /// <summary>
        /// The axis of angular velocity, along with the radians/time speed (length of the vector).
        /// </summary>
        public readonly Vector3D AxisAngleVector;

        /// <summary>
        /// The linear velocity vector. Describes the direction of motion as well as the speed (length of the vector).
        /// </summary>
        public readonly Vector3D LinearVector;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVelocity3D"/> struct.
        /// </summary>
        /// <param name="originCoordinateSystem">The origin coordinate system.</param>
        /// <param name="axisAngleVector">The axis-angle representation of angular velocity.</param>
        /// <param name="linearVector">The linear velocity vector.</param>
        public CoordinateSystemVelocity3D(
            CoordinateSystem originCoordinateSystem,
            Vector3D axisAngleVector,
            Vector3D linearVector)
        {
            this.OriginCoordinateSystem = originCoordinateSystem;
            this.AxisAngleVector = axisAngleVector;
            this.LinearVector = linearVector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVelocity3D"/> struct.
        /// </summary>
        /// <param name="originCoordinateSystem">The origin coordinate system.</param>
        /// <param name="angularAxis">The axis of angular velocity.</param>
        /// <param name="angularSpeed">The angular speed around the axis.</param>
        /// <param name="linearDirection">The direction of linear velocity.</param>
        /// <param name="linearSpeed">The linear speed.</param>
        public CoordinateSystemVelocity3D(
            CoordinateSystem originCoordinateSystem,
            UnitVector3D angularAxis,
            double angularSpeed,
            UnitVector3D linearDirection,
            double linearSpeed)
            : this(originCoordinateSystem, angularAxis.ScaleBy(angularSpeed), linearDirection.ScaleBy(linearSpeed))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVelocity3D"/> struct.
        /// </summary>
        /// <param name="originCoordinateSystem">The origin coordinate system.</param>
        /// <param name="destinationCoordinateSystem">A destination coordinate system.</param>
        /// <param name="time">The time it took to reach the destination coordinate system.</param>
        public CoordinateSystemVelocity3D(
            CoordinateSystem originCoordinateSystem,
            CoordinateSystem destinationCoordinateSystem,
            TimeSpan time)
        {
            this.OriginCoordinateSystem = originCoordinateSystem;
            var coordinateDifference = destinationCoordinateSystem.TransformBy(originCoordinateSystem.Invert());
            var timeInSeconds = time.TotalSeconds;
            this.LinearVector = coordinateDifference.Origin.ToVector3D().ScaleBy(1.0 / timeInSeconds);
            var axisAngleDistance = Vector3D.OfVector(MatrixToAxisAngle(coordinateDifference.GetRotationSubMatrix()));
            var angularSpeed = axisAngleDistance.Length / timeInSeconds;
            this.AxisAngleVector = angularSpeed == 0 ? default : axisAngleDistance.Normalize().ScaleBy(angularSpeed);
        }

        /// <summary>
        /// Returns a value indicating whether the specified velocities are the same.
        /// </summary>
        /// <param name="left">The first velocity.</param>
        /// <param name="right">The second velocity.</param>
        /// <returns>True if the velocities are the same; otherwise false.</returns>
        public static bool operator ==(CoordinateSystemVelocity3D left, CoordinateSystemVelocity3D right) => left.Equals(right);

        /// <summary>
        /// Returns a value indicating whether the specified velocities are different.
        /// </summary>
        /// <param name="left">The first velocity.</param>
        /// <param name="right">The second velocity.</param>
        /// <returns>True if the velocities are different; otherwise false.</returns>
        public static bool operator !=(CoordinateSystemVelocity3D left, CoordinateSystemVelocity3D right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(CoordinateSystemVelocity3D other) => this.OriginCoordinateSystem.Equals(other.OriginCoordinateSystem) && this.AxisAngleVector == other.AxisAngleVector && this.LinearVector == other.LinearVector;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CoordinateSystemVelocity3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.OriginCoordinateSystem, this.AxisAngleVector, this.LinearVector);

        /// <summary>
        /// Get the linear velocity component of this coordinate system velocity.
        /// </summary>
        /// <returns>The linear velocity.</returns>
        public LinearVelocity3D GetLinearVelocity() => new (this.OriginCoordinateSystem.Origin, this.LinearVector);

        /// <summary>
        /// Get the angular velocity component of this coordinate system velocity.
        /// </summary>
        /// <returns>The angular velocity.</returns>
        public AngularVelocity3D GetAngularVelocity() => new (this.OriginCoordinateSystem.GetRotationSubMatrix(), this.AxisAngleVector);

        /// <summary>
        /// Computes the destination coordinate system, if this velocity is followed for a given amount of time.
        /// </summary>
        /// <param name="time">The span of time to compute over.</param>
        /// <returns>The destination coordinate system.</returns>
        /// <remarks>The unit of time should be the same as assumed for the linear and angular velocity vector (e.g., seconds).</remarks>
        public CoordinateSystem ComputeDestination(double time)
        {
            var destinationPoint = this.GetLinearVelocity().ComputeDestination(time);
            var destinationRotation = this.GetAngularVelocity().ComputeDestination(time);
            return new CoordinateSystem(destinationPoint, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis).SetRotationSubMatrix(destinationRotation);
        }
    }
}
