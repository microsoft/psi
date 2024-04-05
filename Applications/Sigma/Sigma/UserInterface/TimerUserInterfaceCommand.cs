// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.IO;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a timer user interface command.
    /// </summary>
    public class TimerUserInterfaceCommand : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimerUserInterfaceCommand"/> class.
        /// </summary>
        public TimerUserInterfaceCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerUserInterfaceCommand"/> class.
        /// </summary>
        /// <param name="location">The 3D point at which to display the timer.</param>
        /// <param name="expiryDateTime">The expiry date/time.</param>
        public TimerUserInterfaceCommand(Point3D location, DateTime expiryDateTime)
        {
            this.Guid = Guid.NewGuid();
            this.Location = location;
            this.ExpiryDateTime = expiryDateTime;
        }

        /// <summary>
        /// Gets or sets the timer display command guid.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the location where to display the timer.
        /// </summary>
        public Point3D Location { get; set; }

        /// <summary>
        /// Gets or sets the expiry datetime.
        /// </summary>
        public DateTime ExpiryDateTime { get; set; }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteGuid(this.Guid, writer);
            Serialization.WritePoint3D(this.Location, writer);
            InteropSerialization.WriteDateTime(this.ExpiryDateTime, writer);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.Guid = InteropSerialization.ReadGuid(reader);
            this.Location = Serialization.ReadPoint3D(reader);
            this.ExpiryDateTime = InteropSerialization.ReadDateTime(reader);
        }
    }
}
