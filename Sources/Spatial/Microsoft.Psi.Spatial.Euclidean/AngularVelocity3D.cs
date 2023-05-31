// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using static Microsoft.Psi.Calibration.CalibrationExtensions;

    /// <summary>
    /// Represents an angular velocity, rotating around a particular axis from a starting rotation.
    /// </summary>
    [Serializer(typeof(AngularVelocity3D.CustomSerializer))]
    public readonly struct AngularVelocity3D : IEquatable<AngularVelocity3D>
    {
        /// <summary>
        /// The starting rotation of origin.
        /// </summary>
        public readonly Matrix<double> OriginRotation;

        /// <summary>
        /// The axis of the angular direction of motion for this velocity.
        /// </summary>
        public readonly UnitVector3D AxisDirection;

        /// <summary>
        /// Gets the magnitude (per-second speed) of the velocity.
        /// </summary>
        public readonly Angle Magnitude;

        /// <summary>
        /// Initializes a new instance of the <see cref="AngularVelocity3D"/> struct.
        /// </summary>
        /// <param name="originRotation">The starting rotation.</param>
        /// <param name="axisDirection">The axis of the angular direction of motion.</param>
        /// <param name="magnitude">The magnitude (per-second speed) of the velocity.</param>
        public AngularVelocity3D(Matrix<double> originRotation, UnitVector3D axisDirection, Angle magnitude)
        {
            if (originRotation.RowCount != 3 ||
                originRotation.ColumnCount != 3)
            {
                throw new ArgumentException("Rotation matrix must be 3x3.");
            }

            if (axisDirection == default(UnitVector3D) && magnitude.Radians != 0)
            {
                throw new ArgumentException("Axis direction cannot be (0,0,0) with a non-zero magnitude.");
            }

            this.OriginRotation = originRotation;
            this.AxisDirection = axisDirection;
            this.Magnitude = magnitude;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngularVelocity3D"/> struct.
        /// </summary>
        /// <param name="originRotation">The starting rotation.</param>
        /// <param name="destinationRotation">A destination rotation.</param>
        /// <param name="time">The time it took to reach the destination rotation.</param>
        /// <param name="angleEpsilon">An optional angle epsilon parameter used to determine when
        /// the computed rotation matrix contains a zero-rotation (by default 0.01 degrees).</param>
        public AngularVelocity3D(Matrix<double> originRotation, Matrix<double> destinationRotation, TimeSpan time, Angle? angleEpsilon = null)
        {
            if (originRotation.RowCount != 3 ||
                originRotation.ColumnCount != 3 ||
                destinationRotation.RowCount != 3 ||
                destinationRotation.ColumnCount != 3)
            {
                throw new ArgumentException("Rotation matrices must be 3x3.");
            }

            this.OriginRotation = originRotation;
            var axisVelocity = Vector3D.OfVector(MatrixToAxisAngle(destinationRotation * originRotation.Inverse(), angleEpsilon));
            this.Magnitude = Angle.FromRadians(axisVelocity.Length / time.TotalSeconds);
            this.AxisDirection = axisVelocity.Length < float.Epsilon ? default : axisVelocity.Normalize();
        }

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
        public bool Equals(AngularVelocity3D other) =>
            this.OriginRotation.Equals(other.OriginRotation) &&
            this.AxisDirection.Equals(other.AxisDirection) &&
            this.Magnitude.Equals(other.Magnitude);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is AngularVelocity3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.OriginRotation, this.AxisDirection, this.Magnitude);

        /// <summary>
        /// Computes the destination rotation, if this velocity is followed for a given amount of time.
        /// </summary>
        /// <param name="time">The span of time to compute over.</param>
        /// <returns>The destination rotation.</returns>
        public Matrix<double> ComputeDestination(TimeSpan time) =>
            AxisAngleToMatrix(this.AxisDirection.ScaleBy(this.Magnitude.Radians * time.TotalSeconds).ToVector()) * this.OriginRotation;

        /// <summary>
        /// Provides custom read- backcompat serialization for <see cref="AngularVelocity3D"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatStructSerializer<AngularVelocity3D>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<Matrix<double>> matrixHandler;
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
                    this.matrixHandler = serializers.GetHandler<Matrix<double>>();
                    this.vector3DHandler = serializers.GetHandler<Vector3D>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(AngularVelocity3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref AngularVelocity3D target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var originRotation = default(Matrix<double>);
                    var axisAngleVector = default(Vector3D);
                    this.matrixHandler.Deserialize(reader, ref originRotation, context);
                    this.vector3DHandler.Deserialize(reader, ref axisAngleVector, context);
                    target = new AngularVelocity3D(
                        originRotation,
                        axisAngleVector.Length >= float.Epsilon ? axisAngleVector.Normalize() : default,
                        Angle.FromRadians(axisAngleVector.Length));
                }
                else
                {
                    throw new NotSupportedException($"{nameof(AngularVelocity3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
