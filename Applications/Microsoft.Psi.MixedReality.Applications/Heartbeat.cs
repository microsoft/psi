// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.IO;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents the compute server pipeline heartbeat.
    /// </summary>
    [Serializer(typeof(Heartbeat.CustomSerializer))]
    public struct Heartbeat
    {
        /// <summary>
        /// Gets the serialization format.
        /// </summary>
        public static Format<Heartbeat> Format => new (Write, Read);

        /// <summary>
        /// Gets or sets the auxiliary info.
        /// </summary>
        public string AuxiliaryInfo { get; set; }

        /// <summary>
        /// Gets or sets the latency.
        /// </summary>
        public double Latency { get; set; }

        /// <summary>
        /// Gets or sets the frame rate.
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        /// Write <see cref="Heartbeat"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="heartBeat"><see cref="Heartbeat"/> to write.</param>
        /// <param name="writer"><see cref="Heartbeat"/> to which to write.</param>
        public static void Write(Heartbeat heartBeat, BinaryWriter writer)
        {
            InteropSerialization.WriteString(heartBeat.AuxiliaryInfo, writer);
            writer.Write(heartBeat.Latency);
            writer.Write(heartBeat.FrameRate);
        }

        /// <summary>
        /// Read <see cref="Heartbeat"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Heartbeat"/>.</returns>
        public static Heartbeat Read(BinaryReader reader)
        {
            var auxiliaryInfo = InteropSerialization.ReadString(reader);
            var latency = reader.ReadDouble();
            var frameRate = reader.ReadDouble();
            return new Heartbeat { FrameRate = frameRate, AuxiliaryInfo = auxiliaryInfo, Latency = latency };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var auxiliaryInfoString = this.AuxiliaryInfo != null ? $" | {this.AuxiliaryInfo}" : string.Empty;
            return $"{this.FrameRate:0.0} FPS | {this.Latency:0.0} s{auxiliaryInfoString}";
        }

        /// <summary>
        /// Provides custom read- backcompat serialization for <see cref="Heartbeat"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatStructSerializer<Heartbeat>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<bool> teachingModeHandler;
            private SerializationHandler<double> latencyHandler;
            private SerializationHandler<double> frameRateHandler;

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
                    this.teachingModeHandler = serializers.GetHandler<bool>();
                    this.latencyHandler = serializers.GetHandler<double>();
                    this.frameRateHandler = serializers.GetHandler<double>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Heartbeat.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref Heartbeat target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var teachingMode = default(bool);
                    var latency = default(double);
                    var frameRate = default(double);

                    this.teachingModeHandler.Deserialize(reader, ref teachingMode, context);
                    this.latencyHandler.Deserialize(reader, ref latency, context);
                    this.frameRateHandler.Deserialize(reader, ref frameRate, context);

                    target = new Heartbeat()
                    {
                        AuxiliaryInfo = teachingMode ? "Teaching Mode" : string.Empty,
                        Latency = latency,
                        FrameRate = frameRate,
                    };
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Heartbeat.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
