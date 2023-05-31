// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Specifies custom flags for Psi data streams.
    /// </summary>
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
        private const int LatestVersion = 2;
        private byte[] supplementalMetadataBytes = Array.Empty<byte>();

        internal PsiStreamMetadata(
            string name,
            int id,
            string typeName,
            int version = LatestVersion,
            int serializationSystemVersion = RuntimeInfo.LatestSerializationSystemVersion,
            ushort customFlags = 0)
            : base (MetadataKind.StreamMetadata, name, id, version, serializationSystemVersion)
        {
            this.TypeName = typeName;
            this.CustomFlags = customFlags;
        }

        /// <summary>
        /// Gets the name of the type of data contained in the stream.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the custom flags implemented in derived types.
        /// </summary>
        public ushort CustomFlags { get; internal set; }

        /// <summary>
        /// Gets the time when the stream was opened.
        /// </summary>
        public DateTime OpenedTime { get; internal set; } = DateTime.MinValue;

        /// <summary>
        /// Gets the time when the stream was closed.
        /// </summary>
        public DateTime ClosedTime { get; internal set; } = DateTime.MaxValue;

        /// <inheritdoc />
        public string StoreName { get; internal set; }

        /// <inheritdoc />
        public string StorePath { get; internal set; }

        /// <inheritdoc />
        public DateTime FirstMessageCreationTime { get; internal set; }

        /// <inheritdoc />
        public DateTime LastMessageCreationTime { get; internal set; }

        /// <inheritdoc />
        public DateTime FirstMessageOriginatingTime { get; internal set; }

        /// <inheritdoc />
        public DateTime LastMessageOriginatingTime { get; internal set; }

        /// <inheritdoc />
        public long MessageCount { get; internal set; }

        /// <summary>
        /// Gets the total size (bytes) of messages in the stream.
        /// </summary>
        public long MessageSizeCumulativeSum { get; private set; }

        /// <summary>
        /// Gets the cumulative sum of latencies of messages in the stream.
        /// </summary>
        public long LatencyCumulativeSum { get; private set; }

        /// <inheritdoc />
        public double AverageMessageSize => this.MessageCount > 0 ? (double)this.MessageSizeCumulativeSum / this.MessageCount : 0;

        /// <inheritdoc />
        public double AverageMessageLatencyMs => this.MessageCount > 0 ? (double)this.LatencyCumulativeSum / this.MessageCount / TimeSpan.TicksPerMillisecond : 0;

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
                this.RuntimeTypes ??= new Dictionary<int, string>();
                this.SetFlag(StreamMetadataFlags.Polymorphic, value);
            }
        }

        /// <inheritdoc />
        public string SupplementalMetadataTypeName { get; private set; }

        /// <summary>
        /// Gets the time interval this stream was in existence (from open to close).
        /// </summary>
        public TimeInterval StreamTimeInterval => new (this.OpenedTime, this.ClosedTime);

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this stream.
        /// If the stream contains no messages, an empty interval is returned.
        /// </summary>
        public TimeInterval MessageCreationTimeInterval => this.MessageCount == 0 ? TimeInterval.Empty : new TimeInterval(this.FirstMessageCreationTime, this.LastMessageCreationTime);

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this stream.
        /// If the stream contains no messages, an empty interval is returned.
        /// </summary>
        public TimeInterval MessageOriginatingTimeInterval => this.MessageCount == 0 ? TimeInterval.Empty : new TimeInterval(this.FirstMessageOriginatingTime, this.LastMessageOriginatingTime);

        /// <inheritdoc />
        public void Update(Envelope envelope, int size)
        {
            if (this.FirstMessageOriginatingTime == default)
            {
                this.FirstMessageOriginatingTime = envelope.OriginatingTime;
                this.FirstMessageCreationTime = envelope.CreationTime;
            }

            this.LastMessageOriginatingTime = envelope.OriginatingTime;
            this.LastMessageCreationTime = envelope.CreationTime;
            this.MessageCount++;
            this.MessageSizeCumulativeSum += size;
            this.LatencyCumulativeSum += (envelope.CreationTime - envelope.OriginatingTime).Ticks;
        }

        /// <inheritdoc />
        public void Update(TimeInterval messagesTimeInterval, TimeInterval messagesOriginatingTimeInterval)
        {
            this.FirstMessageCreationTime = messagesTimeInterval.Left;
            this.LastMessageCreationTime = messagesTimeInterval.Right;

            this.FirstMessageOriginatingTime = messagesOriginatingTimeInterval.Left;
            this.LastMessageOriginatingTime = messagesOriginatingTimeInterval.Right;
        }

        /// <summary>
        /// Gets supplemental stream metadata.
        /// </summary>
        /// <typeparam name="T">Type of supplemental metadata.</typeparam>
        /// <param name="serializers">Known serializers.</param>
        /// <returns>Supplemental metadata.</returns>
        public T GetSupplementalMetadata<T>(KnownSerializers serializers)
        {
            if (string.IsNullOrEmpty(this.SupplementalMetadataTypeName))
            {
                throw new InvalidOperationException("Stream does not contain supplemental metadata.");
            }

            if (typeof(T) != TypeResolutionHelper.GetVerifiedType(this.SupplementalMetadataTypeName))
            {
                throw new InvalidCastException($"Supplemental metadata type mismatch ({this.SupplementalMetadataTypeName}).");
            }

            var handler = serializers.GetHandler<T>();
            var reader = new BufferReader(this.supplementalMetadataBytes);
            var target = default(T);
            handler.Deserialize(reader, ref target, new SerializationContext(serializers));
            return target;
        }

        /// <inheritdoc />
        public T GetSupplementalMetadata<T>()
        {
            return this.GetSupplementalMetadata<T>(KnownSerializers.Default);
        }

        /// <summary>
        /// Sets supplemental stream metadata.
        /// </summary>
        /// <param name="supplementalMetadataTypeName">The serialized supplemental metadata bytes.</param>
        /// <param name="supplementalMetadataBytes">The supplemental metadata type name.</param>
        internal void SetSupplementalMetadata(string supplementalMetadataTypeName, byte[] supplementalMetadataBytes)
        {
            this.SupplementalMetadataTypeName = supplementalMetadataTypeName;
            this.supplementalMetadataBytes = new byte[supplementalMetadataBytes.Length];
            Array.Copy(supplementalMetadataBytes, this.supplementalMetadataBytes, supplementalMetadataBytes.Length);
        }

        /// <summary>
        /// Update supplemental stream metadata from another stream metadata.
        /// </summary>
        /// <param name="other">Other stream metadata from which to copy supplemental metadata.</param>
        /// <returns>Updated stream metadata.</returns>
        internal PsiStreamMetadata UpdateSupplementalMetadataFrom(PsiStreamMetadata other)
        {
            this.SupplementalMetadataTypeName = other.SupplementalMetadataTypeName;
            this.supplementalMetadataBytes = other.supplementalMetadataBytes;
            return this;
        }

        // custom deserializer with no dependency on the Serializer subsystem
        // order of fields is important for backwards compat and must be the same as the order in Serialize, don't change!
        internal new void Deserialize(BufferReader metadataBuffer)
        {
            this.OpenedTime = metadataBuffer.ReadDateTime();
            this.ClosedTime = metadataBuffer.ReadDateTime();

            if (this.Version >= 2)
            {
                this.MessageCount = metadataBuffer.ReadInt64(); // long in v2+
                this.MessageSizeCumulativeSum = metadataBuffer.ReadInt64(); // added in v2
                this.LatencyCumulativeSum = metadataBuffer.ReadInt64(); // added in v2
            }
            else
            {
                this.MessageCount = metadataBuffer.ReadInt32(); // < v1 int
                //// MessageSizeCumulativeSum computed below for old versions
                //// LatencyCumulativeSum computed below for old versions
            }

            this.FirstMessageCreationTime = metadataBuffer.ReadDateTime();
            this.LastMessageCreationTime = metadataBuffer.ReadDateTime();
            this.FirstMessageOriginatingTime = metadataBuffer.ReadDateTime();
            this.LastMessageOriginatingTime = metadataBuffer.ReadDateTime();
            if (this.Version < 2)
            {
                // AverageMessageSize/Latency migrated in v2+ to cumulative sums
                var avgMessageSize = metadataBuffer.ReadInt32();
                this.MessageSizeCumulativeSum = avgMessageSize * this.MessageCount;

                var avgLatency = metadataBuffer.ReadInt32() * 10; // convert microseconds to ticks
                this.LatencyCumulativeSum = avgLatency * this.MessageCount;
            }

            if (this.IsPolymorphic)
            {
                var typeCount = metadataBuffer.ReadInt32();
                this.RuntimeTypes ??= new Dictionary<int, string>(typeCount);
                for (int i = 0; i < typeCount; i++)
                {
                    this.RuntimeTypes.Add(metadataBuffer.ReadInt32(), metadataBuffer.ReadString());
                }
            }

            if (this.Version >= 1)
            {
                // supplemental metadata added in v1
                this.SupplementalMetadataTypeName = metadataBuffer.ReadString();
                var len = metadataBuffer.ReadInt32();
                this.supplementalMetadataBytes = new byte[len];
                metadataBuffer.Read(this.supplementalMetadataBytes, len);
            }

            this.Version = LatestVersion; // upgrade to current version format
        }

        internal override void Serialize(BufferWriter metadataBuffer)
        {
            // Serialization follows a legacy pattern of fields, as described
            // in the comments at the top of the Metadata.Deserialize method.
            metadataBuffer.Write(this.Name);
            metadataBuffer.Write(this.Id);
            metadataBuffer.Write(this.TypeName);
            metadataBuffer.Write(this.Version);
            metadataBuffer.Write(default(string));      // this metadata field is not used by PsiStreamMetadata
            metadataBuffer.Write(this.SerializationSystemVersion);
            metadataBuffer.Write(this.CustomFlags);
            metadataBuffer.Write((ushort)this.Kind);

            metadataBuffer.Write(this.OpenedTime);
            metadataBuffer.Write(this.ClosedTime);
            metadataBuffer.Write(this.MessageCount);
            metadataBuffer.Write(this.MessageSizeCumulativeSum);
            metadataBuffer.Write(this.LatencyCumulativeSum);
            metadataBuffer.Write(this.FirstMessageCreationTime);
            metadataBuffer.Write(this.LastMessageCreationTime);
            metadataBuffer.Write(this.FirstMessageOriginatingTime);
            metadataBuffer.Write(this.LastMessageOriginatingTime);
            if (this.IsPolymorphic)
            {
                metadataBuffer.Write(this.RuntimeTypes.Count);
                foreach (var pair in this.RuntimeTypes)
                {
                    metadataBuffer.Write(pair.Key);
                    metadataBuffer.Write(pair.Value);
                }
            }

            metadataBuffer.Write(this.SupplementalMetadataTypeName);
            metadataBuffer.Write(this.supplementalMetadataBytes.Length);
            if (this.supplementalMetadataBytes.Length > 0)
            {
                metadataBuffer.Write(this.supplementalMetadataBytes);
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
