// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Specifies custom flags for Psi data streams.
    /// </summary>
    /// <seealso cref="Metadata.CustomFlags"/>
    public enum StreamMetadataFlags : ushort
    {
        /// <summary>
        /// Flag indicating stream is being persisted.
        /// </summary>
        NotPersisted = 0x01,

        /// <summary>
        /// Flag indicating stream has been closed.
        /// </summary>
        Closed = 0x02,

        /// <summary>
        /// Flag indicating stream is indexed.
        /// </summary>
        Indexed = 0x04,

        /// <summary>
        /// Flag indicating stream contains polymorphic types.
        /// </summary>
        Polymorphic = 0x08,
    }

    /// <summary>
    /// Represents metadata used in storing stream data in a Psi store.
    /// </summary>
    public sealed class PsiStreamMetadata : Metadata, IStreamMetadata
    {
        private const int TicksPerMicrosecond = 10;

        internal PsiStreamMetadata(string name, int id, string typeName)
            : base(MetadataKind.StreamMetadata, name, id, typeName, 0, null, 0, 0)
        {
        }

        internal PsiStreamMetadata(string name, int id, string typeName, int version, string serializerTypeName, int serializerVersion, ushort customFlags)
           : base(MetadataKind.StreamMetadata, name, id, typeName, version, serializerTypeName, serializerVersion, customFlags)
        {
        }

        internal PsiStreamMetadata()
        {
        }

        /// <summary>
        /// Gets the time when the stream was opened.
        /// </summary>
        public DateTime Opened { get; internal set; }

        /// <summary>
        /// Gets the time when the stream was closed.
        /// </summary>
        public DateTime Closed { get; internal set; }

        /// <inheritdoc />
        public string PartitionName { get; internal set; }

        /// <inheritdoc />
        public string PartitionPath { get; internal set; }

        /// <inheritdoc />
        public DateTime FirstMessageTime { get; internal set; }

        /// <inheritdoc />
        public DateTime LastMessageTime { get; internal set; }

        /// <inheritdoc />
        public DateTime FirstMessageOriginatingTime { get; internal set; }

        /// <inheritdoc />
        public DateTime LastMessageOriginatingTime { get; internal set; }

        /// <inheritdoc />
        public int AverageMessageSize { get; internal set; }

        /// <inheritdoc />
        public int AverageLatency { get; internal set; }

        /// <inheritdoc />
        public int MessageCount { get; internal set; }

        /// <summary>
        /// Gets a dictionary of runtime type names referenced in stream.
        /// </summary>
        public Dictionary<int, string> RuntimeTypes { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the stream has been closed.
        /// </summary>
        public bool IsClosed
        {
            get { return this.GetFlag(StreamMetadataFlags.Closed); }
            internal set { this.SetFlag(StreamMetadataFlags.Closed, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the stream is persisted.
        /// </summary>
        public bool IsPersisted
        {
            get { return !this.GetFlag(StreamMetadataFlags.NotPersisted); }
            internal set { this.SetFlag(StreamMetadataFlags.NotPersisted, !value); }
        }

        /// <summary>
        /// Gets a value indicating whether the stream is indexed.
        /// </summary>
        public bool IsIndexed
        {
            get { return this.GetFlag(StreamMetadataFlags.Indexed); }
            internal set { this.SetFlag(StreamMetadataFlags.Indexed, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the stream is persisted.
        /// </summary>
        public bool IsPolymorphic
        {
            get
            {
                return this.GetFlag(StreamMetadataFlags.Polymorphic);
            }

            internal set
            {
                this.RuntimeTypes = this.RuntimeTypes ?? new Dictionary<int, string>();
                this.SetFlag(StreamMetadataFlags.Polymorphic, value);
            }
        }

        /// <summary>
        /// Gets the average frequency of messages written to this stream.
        /// </summary>
        public double AverageFrequency => (this.LastMessageTime - this.FirstMessageTime).TotalMilliseconds / (double)(this.MessageCount - 1);

        /// <summary>
        /// Gets the time interval this stream was opened (from open to close).
        /// </summary>
        public TimeInterval Lifetime => new TimeInterval(this.Opened, this.Closed);

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this stream.
        /// If the stream contains no messages, an empty interval is returned.
        /// </summary>
        public TimeInterval ActiveLifetime => this.MessageCount == 0 ? TimeInterval.Empty : new TimeInterval(this.FirstMessageTime, this.LastMessageTime);

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this stream.
        /// If the stream contains no messages, an empty interval is returned.
        /// </summary>
        public TimeInterval OriginatingLifetime => this.MessageCount == 0 ? TimeInterval.Empty : new TimeInterval(this.FirstMessageOriginatingTime, this.LastMessageOriginatingTime);

        /// <inheritdoc />
        public void Update(Envelope envelope, int size)
        {
            if (this.FirstMessageOriginatingTime == default(DateTime))
            {
                this.FirstMessageOriginatingTime = envelope.OriginatingTime;
                this.FirstMessageTime = envelope.Time;
                this.Opened = envelope.Time;
            }

            this.LastMessageOriginatingTime = envelope.OriginatingTime;
            this.LastMessageTime = envelope.Time;
            this.MessageCount++;
            this.AverageLatency = (int)((((long)this.AverageLatency * (this.MessageCount - 1)) + ((envelope.Time - envelope.OriginatingTime).Ticks / TicksPerMicrosecond)) / this.MessageCount);
            this.AverageMessageSize = (int)((((long)this.AverageMessageSize * (this.MessageCount - 1)) + size) / this.MessageCount);
        }

        /// <inheritdoc />
        public void Update(TimeInterval messagesTimeInterval, TimeInterval messagesOriginatingTimeInterval)
        {
            this.FirstMessageTime = messagesTimeInterval.Left;
            this.LastMessageTime = messagesTimeInterval.Right;

            this.FirstMessageOriginatingTime = messagesOriginatingTimeInterval.Left;
            this.LastMessageOriginatingTime = messagesOriginatingTimeInterval.Right;
        }

        // custom deserializer with no dependency on the Serializer subsystem
        // order of fields is important for backwards compat and must be the same as the order in Serialize, don't change!
        internal new void Deserialize(BufferReader metadataBuffer)
        {
            this.Opened = metadataBuffer.ReadDateTime();
            this.Closed = metadataBuffer.ReadDateTime();
            this.MessageCount = metadataBuffer.ReadInt32();
            this.FirstMessageTime = metadataBuffer.ReadDateTime();
            this.LastMessageTime = metadataBuffer.ReadDateTime();
            this.FirstMessageOriginatingTime = metadataBuffer.ReadDateTime();
            this.LastMessageOriginatingTime = metadataBuffer.ReadDateTime();
            this.AverageMessageSize = metadataBuffer.ReadInt32();
            this.AverageLatency = metadataBuffer.ReadInt32();
            if (this.IsPolymorphic)
            {
                var typeCount = metadataBuffer.ReadInt32();
                this.RuntimeTypes = this.RuntimeTypes ?? new Dictionary<int, string>(typeCount);
                for (int i = 0; i < typeCount; i++)
                {
                    this.RuntimeTypes.Add(metadataBuffer.ReadInt32(), metadataBuffer.ReadString());
                }
            }
        }

        internal override void Serialize(BufferWriter metadataBuffer)
        {
            base.Serialize(metadataBuffer);
            metadataBuffer.Write(this.Opened);
            metadataBuffer.Write(this.Closed);
            metadataBuffer.Write(this.MessageCount);
            metadataBuffer.Write(this.FirstMessageTime);
            metadataBuffer.Write(this.LastMessageTime);
            metadataBuffer.Write(this.FirstMessageOriginatingTime);
            metadataBuffer.Write(this.LastMessageOriginatingTime);
            metadataBuffer.Write(this.AverageMessageSize);
            metadataBuffer.Write(this.AverageLatency);
            if (this.IsPolymorphic)
            {
                metadataBuffer.Write(this.RuntimeTypes.Count);
                foreach (var pair in this.RuntimeTypes)
                {
                    metadataBuffer.Write(pair.Key);
                    metadataBuffer.Write(pair.Value);
                }
            }
        }

        private bool GetFlag(StreamMetadataFlags smflag)
        {
            var flag = (ushort)smflag;
            return (this.CustomFlags & flag) != 0;
        }

        private void SetFlag(StreamMetadataFlags smflag, bool value)
        {
            var flag = (ushort)smflag;
            this.CustomFlags = (ushort)((this.CustomFlags & ~flag) | (value ? flag : 0));
        }
    }
}
