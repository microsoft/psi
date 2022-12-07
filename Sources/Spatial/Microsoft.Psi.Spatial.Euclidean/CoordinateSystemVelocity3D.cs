// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents a coordinate system velocity composing both a linear and angular velocity.
    /// </summary>
    [Serializer(typeof(CoordinateSystemVelocity3D.CustomSerializer))]
    public readonly struct CoordinateSystemVelocity3D : IEquatable<CoordinateSystemVelocity3D>
    {
        /// <summary>
        /// The angular velocity corresponding to the speed and direction of rotation.
        /// </summary>
        public readonly AngularVelocity3D Angular;

        /// <summary>
        /// The linear velocity corresponding to the speed and direction of translation.
        /// </summary>
        public readonly LinearVelocity3D Linear;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVelocity3D"/> struct.
        /// </summary>
        /// <param name="angularVelocity">The angular velocity component.</param>
        /// <param name="linearVelocity">The linear velocity component.</param>
        public CoordinateSystemVelocity3D(AngularVelocity3D angularVelocity, LinearVelocity3D linearVelocity)
        {
            this.Angular = angularVelocity;
            this.Linear = linearVelocity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVelocity3D"/> struct.
        /// </summary>
        /// <param name="originCoordinateSystem">The starting coordinate system.</param>
        /// <param name="rotationAxisDirection">The axis direction of rotation for the angular velocity.</param>
        /// <param name="angularVelocityMagnitude">The magnitude (per-second speed) of the angular velocity.</param>
        /// <param name="linearDirection">The direction of linear velocity.</param>
        /// <param name="linearVelocityMagnitude">The magnitude (per-second speed) of the linear velocity.</param>
        public CoordinateSystemVelocity3D(
            CoordinateSystem originCoordinateSystem,
            UnitVector3D rotationAxisDirection,
            Angle angularVelocityMagnitude,
            UnitVector3D linearDirection,
            double linearVelocityMagnitude)
            : this(
                  new AngularVelocity3D(originCoordinateSystem.GetRotationSubMatrix(), rotationAxisDirection, angularVelocityMagnitude),
                  new LinearVelocity3D(originCoordinateSystem.Origin, linearDirection, linearVelocityMagnitude))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVelocity3D"/> struct.
        /// </summary>
        /// <param name="originCoordinateSystem">The origin coordinate system.</param>
        /// <param name="destinationCoordinateSystem">A destination coordinate system.</param>
        /// <param name="time">The time it took to reach the destination coordinate system.</param>
        /// <param name="angleEpsilon">An optional angle epsilon parameter used to determine when
        /// the computed rotation matrix contains a zero-rotation (by default 0.01 degrees).</param>
        public CoordinateSystemVelocity3D(
            CoordinateSystem originCoordinateSystem,
            CoordinateSystem destinationCoordinateSystem,
            TimeSpan time,
            Angle? angleEpsilon = null)
        {
            this.Angular = new AngularVelocity3D(originCoordinateSystem.GetRotationSubMatrix(), destinationCoordinateSystem.GetRotationSubMatrix(), time, angleEpsilon);
            this.Linear = new LinearVelocity3D(originCoordinateSystem.Origin, destinationCoordinateSystem.Origin, time);
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
        public bool Equals(CoordinateSystemVelocity3D other) => this.Angular.Equals(other.Angular) && this.Linear.Equals(other.Linear);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CoordinateSystemVelocity3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Angular, this.Linear);

        /// <summary>
        /// Computes the destination coordinate system, if this velocity is followed for a given amount of time.
        /// </summary>
        /// <param name="time">The span of time to compute over.</param>
        /// <returns>The destination coordinate system.</returns>
        public CoordinateSystem ComputeDestination(TimeSpan time) =>
            CoordinateSystem.Translation(this.Linear.ComputeDestination(time).ToVector3D())
                            .SetRotationSubMatrix(this.Angular.ComputeDestination(time));

        /// <summary>
        /// Provides custom read- backcompat serialization for <see cref="CoordinateSystemVelocity3D"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatStructSerializer<CoordinateSystemVelocity3D>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<CoordinateSystem> coordinateSystemHandler;
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
                    this.coordinateSystemHandler = serializers.GetHandler<CoordinateSystem>();
                    this.vector3DHandler = serializers.GetHandler<Vector3D>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(CoordinateSystemVelocity3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref CoordinateSystemVelocity3D target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var originCoordinateSystem = default(CoordinateSystem);
                    var axisAngleVector = default(Vector3D);
                    var linearVector = default(Vector3D);
                    this.coordinateSystemHandler.Deserialize(reader, ref originCoordinateSystem, context);
                    this.vector3DHandler.Deserialize(reader, ref axisAngleVector, context);
                    this.vector3DHandler.Deserialize(reader, ref linearVector, context);
                    target = new CoordinateSystemVelocity3D(
                        originCoordinateSystem ?? new CoordinateSystem(),
                        axisAngleVector.Length >= float.Epsilon ? axisAngleVector.Normalize() : default,
                        Angle.FromRadians(axisAngleVector.Length),
                        linearVector.Length >= float.Epsilon ? linearVector.Normalize() : default,
                        linearVector.Length);
                }
                else
                {
                    throw new NotSupportedException($"{nameof(CoordinateSystemVelocity3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
