// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a linear 3D velocity rooted at a point in space.
    /// </summary>
    public readonly struct LinearVelocity3D : IEquatable<LinearVelocity3D>
    {
        /// <summary>
        /// The point of origin.
        /// </summary>
        public readonly Point3D Origin;

        /// <summary>
        /// The velocity vector. Describes the direction of motion as well as the speed (length of the vector).
        /// </summary>
        public readonly Vector3D Vector;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearVelocity3D"/> struct.
        /// </summary>
        /// <param name="origin">The origin point.</param>
        /// <param name="vector">The velocity vector.</param>
        public LinearVelocity3D(Point3D origin, Vector3D vector)
        {
            this.Origin = origin;
            this.Vector = vector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearVelocity3D"/> struct.
        /// </summary>
        /// <param name="origin">The origin of the velocity.</param>
        /// <param name="unitVector">The unit vector indicating the direction of velocity.</param>
        /// <param name="speed">The speed in the specified direction.</param>
        public LinearVelocity3D(Point3D origin, UnitVector3D unitVector, double speed)
            : this(origin, unitVector.ScaleBy(speed))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearVelocity3D"/> struct.
        /// </summary>
        /// <param name="origin">The origin of the velocity.</param>
        /// <param name="destinationPoint">A destination point.</param>
        /// <param name="time">The time it took to reach that destination point.</param>
        public LinearVelocity3D(Point3D origin, Point3D destinationPoint, TimeSpan time)
        {
            this.Origin = origin;
            var directionVector = destinationPoint - origin;
            var speed = directionVector.Length / time.TotalSeconds;
            this.Vector = directionVector.Normalize().ScaleBy(speed);
        }

        /// <summary>
        /// Gets the magnitude of the velocity.
        /// </summary>
        public double Speed => this.Vector.Length;

        /// <summary>
        /// Returns a value indicating whether the specified velocities are the same.
        /// </summary>
        /// <param name="left">The first velocity.</param>
        /// <param name="right">The second velocity.</param>
        /// <returns>True if the velocities are the same; otherwise false.</returns>
        public static bool operator ==(LinearVelocity3D left, LinearVelocity3D right) => left.Equals(right);

        /// <summary>
        /// Returns a value indicating whether the specified velocities are different.
        /// </summary>
        /// <param name="left">The first velocity.</param>
        /// <param name="right">The second velocity.</param>
        /// <returns>True if the velocities are different; otherwise false.</returns>
        public static bool operator !=(LinearVelocity3D left, LinearVelocity3D right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(LinearVelocity3D other) => this.Origin == other.Origin && this.Vector == other.Vector;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is LinearVelocity3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Origin, this.Vector);

        /// <summary>
        /// Computes the destination point, if this velocity is followed for a given amount of time.
        /// </summary>
        /// <param name="time">The span of time to compute over.</param>
        /// <returns>The destination point.</returns>
        /// <remarks>The unit of time should be the same as assumed for the velocity vector (e.g., seconds).</remarks>
        public Point3D ComputeDestination(double time)
        {
            return this.Origin + this.Vector.ScaleBy(time);
        }
    }
}
