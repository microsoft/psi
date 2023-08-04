// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.WinRT
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents the tracked pose of the user's eye gaze, as produced by the WinRT-based EyesSensor component.
    /// </summary>
    [Serializer(typeof(Eyes.CustomSerializer))]
    public class Eyes
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Eyes"/> class.
        /// </summary>
        /// <param name="gazeRay">The 3D ray representing the eye-gaze pose.</param>
        /// <param name="calibrationValid">Value indicating whether or not the calibration was valid for this eye-gaze pose.</param>
        public Eyes(Ray3D? gazeRay, bool calibrationValid)
        {
            this.GazeRay = gazeRay;
            this.CalibrationValid = calibrationValid;
        }

        /// <summary>
        /// Gets the eye-gaze pose as a 3D ray.
        /// </summary>
        public Ray3D? GazeRay { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the calibration was valid for this eye-gaze pose.
        /// </summary>
        public bool CalibrationValid { get; private set; }

        /// <summary>
        /// Provides custom read- backcompat serialization for <see cref="Eyes"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatClassSerializer<Eyes>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<Ray3D> ray3DHandler;
            private SerializationHandler<bool> boolHandler;

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
                    this.ray3DHandler = serializers.GetHandler<Ray3D>();
                    this.boolHandler = serializers.GetHandler<bool>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Eyes.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref Eyes target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var gazeRay = default(Ray3D);
                    var calibrationValid = false;
                    this.ray3DHandler.Deserialize(reader, ref gazeRay, context);
                    this.boolHandler.Deserialize(reader, ref calibrationValid, context);
                    target = new Eyes(gazeRay, calibrationValid);
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Eyes.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
