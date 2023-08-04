// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Represents the envelope of a message published to a data stream.
    /// See <see cref="Message{T}"/> for details.
    /// </summary>
    public struct Envelope
    {
        /// <summary>
        /// The id of the stream that generated the message.
        /// </summary>
        public int SourceId;

        /// <summary>
        /// The sequence number of this message, unique within the stream identified by <see cref="SourceId"/>.
        /// </summary>
        public int SequenceId;

        /// <summary>
        /// The originating time of the message, representing the time of the real-world event that led to the creation of this message.
        /// This value is used as a key when synchronizing messages across streams.
        /// This value must be propagated with any message derived from this message.
        /// </summary>
        public DateTime OriginatingTime;

        /// <summary>
        /// The message creation time.
        /// </summary>
        public DateTime CreationTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> struct.
        /// </summary>
        /// <param name="originatingTime">The <see cref="OriginatingTime"/> of this message.</param>
        /// <param name="creationTime">The <see cref="CreationTime"/> of the message.</param>
        /// <param name="sourceId">The <see cref="SourceId"/> of the message.</param>
        /// <param name="sequenceId">The unique <see cref="SequenceId"/> of the message.</param>
        public Envelope(DateTime originatingTime, DateTime creationTime, int sourceId, int sequenceId)
        {
            this.SourceId = sourceId;
            this.SequenceId = sequenceId;
            this.OriginatingTime = originatingTime;
            this.CreationTime = creationTime;
        }

        /// <summary>
        /// Determines whether two instances are equal.
        /// </summary>
        /// <param name="first">The first object to compare.</param>
        /// <param name="second">The object to compare to.</param>
        /// <returns>True if the instances are equal.</returns>
        public static bool operator ==(Envelope first, Envelope second)
        {
            return
                first.SourceId == second.SourceId &&
                first.SequenceId == second.SequenceId &&
                first.CreationTime == second.CreationTime &&
                first.OriginatingTime == second.OriginatingTime;
        }

        /// <summary>
        /// Determines whether two instances are equal.
        /// </summary>
        /// <param name="first">The first object to compare.</param>
        /// <param name="second">The object to compare to.</param>
        /// <returns>True if the instances are equal.</returns>
        public static bool operator !=(Envelope first, Envelope second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Provide a string representation of this Timestamped instance.
        /// </summary>
        /// <returns>Payload preceded by originating time.</returns>
        public override string ToString()
        {
            return string.Format("{0}.{1} ({2})", this.SourceId, this.SequenceId, this.OriginatingTime);
        }

        /// <summary>
        /// Determines whether two instances are equal.
        /// </summary>
        /// <param name="other">The object to compare to.</param>
        /// <returns>True if the instances are equal.</returns>
        public override bool Equals(object other)
        {
            if (other is not Envelope)
            {
                return false;
            }

            return this == (Envelope)other;
        }

        /// <summary>
        /// Returns a hash code for this instance, obtained by combining the hash codes of the instance fields.
        /// </summary>
        /// <returns>A hashcode.</returns>
        public override int GetHashCode()
        {
            return this.SourceId ^ this.SequenceId ^ this.CreationTime.GetHashCode() ^ this.OriginatingTime.GetHashCode();
        }
    }
}
