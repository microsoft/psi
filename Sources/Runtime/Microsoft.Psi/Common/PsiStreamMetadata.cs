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
        private const int CurrentVersion = 1;
        private const int TicksPerMicrosecond = 10;
        private byte[] supplementalMetadataBytes = Array.Empty<byte>();

        internal PsiStreamMetadata(string name, int id, string typeName)
            : base(MetadataKind.StreamMetadata, name, id, typeName, CurrentVersion, null, 0, 0)
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
        public DateTime OpenedTime { get; internal set; }

        /// <summary>
        /// Gets the time when the stream was closed.
        /// </summary>
        public DateTime ClosedTime { get; internal set; }

        /// <inheritdoc />
        public string PartitionName { get; internal set; }

        /// <inheritdoc />
        public string PartitionPath { get; internal set; }

        /// <inheritdoc />
        public DateTime FirstMessageCreationTime { get; internal set; }

        /// <inheritdoc />
        public DateTime LastMessageCreationTime { get; internal set; }

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

        /// <inheritdoc />
        public string SupplementalMetadataTypeName { get; private set; }

        /// <summary>
        /// Gets the average frequency of messages written to this stream.
        /// </summary>
        public double AverageFrequency => (this.LastMessageCreationTime - this.FirstMessageCreationTime).TotalMilliseconds / (double)(this.MessageCount - 1);

        /// <summary>
        /// Gets the time interval this stream was in existence (from open to close).
        /// </summary>
        public TimeInterval StreamTimeInterval => new TimeInterval(this.OpenedTime, this.ClosedTime);

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
            if (this.FirstMessageOriginatingTime == default(DateTime))
            {
                this.FirstMessageOriginatingTime = envelope.OriginatingTime;
                this.FirstMessageCreationTime = envelope.CreationTime;
                this.OpenedTime = envelope.CreationTime;
            }

            this.LastMessageOriginatingTime = envelope.OriginatingTime;
            this.LastMessageCreationTime = envelope.CreationTime;
            this.MessageCount++;
            this.AverageLatency = (int)((((long)this.AverageLatency * (this.MessageCount - 1)) + ((envelope.CreationTime - envelope.OriginatingTime).Ticks / TicksPerMicrosecond)) / this.MessageCount);
            this.AverageMessageSize = (int)((((long)this.AverageMessageSize * (this.MessageCount - 1)) + size) / this.MessageCount);
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
        /// <typeparam name="T">Type of supplemental metadata.</typeparam>
        /// <param name="value">Supplemental metadata value.</param>
        /// <param name="serializers">Known serializers.</param>
        internal void SetSupplementalMetadata<T>(T value, KnownSerializers serializers)
        {
            this.SupplementalMetadataTypeName = typeof(T).AssemblyQualifiedName;
            var handler = serializers.GetHandler<T>();
            var writer = new BufferWriter(this.supplementalMetadataBytes);
            handler.Serialize(writer, value, new SerializationContext(serializers));
            this.supplementalMetadataBytes = writer.Buffer;
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
            this.MessageCount = metadataBuffer.ReadInt32();
            this.FirstMessageCreationTime = metadataBuffer.ReadDateTime();
            this.LastMessageCreationTime = metadataBuffer.ReadDateTime();
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

            if (this.Version >= 1)
            {
                this.SupplementalMetadataTypeName = metadataBuffer.ReadString();
                var len = metadataBuffer.ReadInt32();
                this.supplementalMetadataBytes = new byte[len];
                metadataBuffer.Read(this.supplementalMetadataBytes, len);
            }
        }

        internal override void Serialize(BufferWriter metadataBuffer)
        {
            base.Serialize(metadataBuffer);
            metadataBuffer.Write(this.OpenedTime);
            metadataBuffer.Write(this.ClosedTime);
            metadataBuffer.Write(this.MessageCount);
            metadataBuffer.Write(this.FirstMessageCreationTime);
            metadataBuffer.Write(this.LastMessageCreationTime);
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
