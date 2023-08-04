// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents a linear velocity from a starting point in 3D space.
    /// </summary>
    [Serializer(typeof(LinearVelocity3D.CustomSerializer))]
    public readonly struct LinearVelocity3D : IEquatable<LinearVelocity3D>
    {
        /// <summary>
        /// The starting point of origin.
        /// </summary>
        public readonly Point3D Origin;

        /// <summary>
        /// The vector direction of motion of the velocity.
        /// </summary>
        public readonly UnitVector3D Direction;

        /// <summary>
        /// The scalar magnitude (per-second speed) of the velocity.
        /// </summary>
        public readonly double Magnitude;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearVelocity3D"/> struct.
        /// </summary>
        /// <param name="origin">The starting point of origin.</param>
        /// <param name="direction">The unit vector indicating the direction of velocity.</param>
        /// <param name="magnitude">The scalar magnitude (per-second speed) of the velocity.</param>
        public LinearVelocity3D(Point3D origin, UnitVector3D direction, double magnitude)
        {
            if (direction == default(UnitVector3D) && magnitude != 0)
            {
                throw new ArgumentException("Axis direction cannot be (0,0,0) with a non-zero magnitude.");
            }

            this.Origin = origin;
            this.Direction = direction;
            this.Magnitude = magnitude;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearVelocity3D"/> struct.
        /// </summary>
        /// <param name="origin">The starting point of origin.</param>
        /// <param name="destinationPoint">A destination point.</param>
        /// <param name="time">The time taken to reach that destination point.</param>
        public LinearVelocity3D(Point3D origin, Point3D destinationPoint, TimeSpan time)
        {
            this.Origin = origin;
            var directionVector = destinationPoint - origin;
            this.Magnitude = directionVector.Length / time.TotalSeconds;
            this.Direction = directionVector.Length >= float.Epsilon ? directionVector.Normalize() : default;
        }

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
        public bool Equals(LinearVelocity3D other) =>
            this.Origin.Equals(other.Origin) &&
            this.Direction.Equals(other.Direction) &&
            this.Magnitude.Equals(other.Magnitude);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is LinearVelocity3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Origin, this.Direction, this.Magnitude);

        /// <summary>
        /// Computes the destination point, if this velocity is followed for a given amount of time.
        /// </summary>
        /// <param name="time">The span of time to compute over.</param>
        /// <returns>The destination point.</returns>
        public Point3D ComputeDestination(TimeSpan time) =>
            this.Origin + this.Direction.ScaleBy(this.Magnitude * time.TotalSeconds);

        /// <summary>
        /// Provides backcompat serialization for <see cref="LinearVelocity3D"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatStructSerializer<LinearVelocity3D>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<Point3D> point3DHandler;
            private SerializationHandler<Vector3D> vector3DHandler;

            /// <summary>
            /// Initializes a new instance of the <see cref="CustomSerializer"/> class.
            /// </summary>
            public CustomSerializer()
                : base(LatestSchemaVersion)
            {
            }

            /// <inheritdoc/>
            public override void InitializeBackCompatSerializationHandlers(int schemaVersion, KnownSerializers serializers, TypeSchema targetSchema)
            {
                if (schemaVersion <= 2)
                {
                    this.point3DHandler = serializers.GetHandler<Point3D>();
                    this.vector3DHandler = serializers.GetHandler<Vector3D>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(LinearVelocity3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref LinearVelocity3D target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var origin = default(Point3D);
                    var vector = default(Vector3D);
                    this.point3DHandler.Deserialize(reader, ref origin, context);
                    this.vector3DHandler.Deserialize(reader, ref vector, context);
                    target = new LinearVelocity3D(
                        origin,
                        vector.Length >= float.Epsilon ? vector.Normalize() : default,
                        vector.Length);
                }
                else
                {
                    throw new NotSupportedException($"{nameof(LinearVelocity3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
