// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using HoloLensSerializers = HoloLensCaptureInterop.Serialization;

    /// <summary>
    /// Class containing UI debug information.
    /// </summary>
    public class UserInterfaceDebugInfo
    {
        /// <summary>
        /// Gets the serialization format.
        /// </summary>
        public static Format<UserInterfaceDebugInfo> Format => new (Write, Read);

        /// <summary>
        /// Gets or sets the outer step time.
        /// </summary>
        public int OuterStepTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the inner step time.
        /// </summary>
        public int InnerStepTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the update state time.
        /// </summary>
        public int UpdateStateTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the process CPU percent.
        /// </summary>
        public int ProcessCpuPercent { get; set; }

        /// <summary>
        /// Write <see cref="UserInterfaceDebugInfo"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="debugInfo"><see cref="UserInterfaceDebugInfo"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void Write(UserInterfaceDebugInfo debugInfo, BinaryWriter writer)
        {
            InteropSerialization.WriteInt32(debugInfo.OuterStepTimeMs, writer);
            InteropSerialization.WriteInt32(debugInfo.InnerStepTimeMs, writer);
            InteropSerialization.WriteInt32(debugInfo.UpdateStateTimeMs, writer);
            InteropSerialization.WriteInt32(debugInfo.ProcessCpuPercent, writer);
        }

        /// <summary>
        /// Read <see cref="UserInterfaceDebugInfo"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="UserInterfaceDebugInfo"/>.</returns>
        public static UserInterfaceDebugInfo Read(BinaryReader reader)
        {
            return new UserInterfaceDebugInfo
            {
                OuterStepTimeMs = InteropSerialization.ReadInt32(reader),
                InnerStepTimeMs = InteropSerialization.ReadInt32(reader),
                UpdateStateTimeMs = InteropSerialization.ReadInt32(reader),
                ProcessCpuPercent = InteropSerialization.ReadInt32(reader),
            };
        }
    }
}
