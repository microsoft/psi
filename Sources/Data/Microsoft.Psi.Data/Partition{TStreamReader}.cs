// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data.Converters;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a base class of partitions that can be added to a session.
    /// </summary>
    /// <typeparam name="TStreamReader">Type of IStreamReader used to read partition.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public sealed class Partition<TStreamReader> : IPartition, IDisposable
        where TStreamReader : IStreamReader
    {
        private static IEnumerable<IStreamMetadata> emptyStreamMetadataCollection = new List<IStreamMetadata>();
        private TStreamReader streamReader;
        private string name;
        private bool isStoreValid = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Partition{TStreamReader}"/> class.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="streamReader">Stream reader used to read partition.</param>
        /// <param name="name">The partition name.</param>
        public Partition(Session session, TStreamReader streamReader, string name)
        {
            this.Initialize(session, streamReader, name, streamReader.Name, streamReader.Path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Partition{TStreamReader}"/> class.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="streamReaderTypeName">Stream reader used to read partition.</param>
        /// <param name="name">The partition name.</param>
        [JsonConstructor]
        private Partition(Session session, string storeName, string storePath, string streamReaderTypeName, string name)
        {
            TStreamReader streamReader = default;
            try
            {
                streamReader = (TStreamReader)Data.StreamReader.Create(storeName, storePath, streamReaderTypeName);
            }
            catch
            {
                // Any exception when trying to create the stream reader will mean the partition is unreadable.
                //
                // - TargetInvocationException wrapping FileNotFoundException will be thrown if catalog file is not found.
                // - TargetInvocationException wrapping InvalidOperationException will be thrown if no data files exist.
                this.isStoreValid = false;
            }

            this.Initialize(session, streamReader, name, storeName, storePath);
        }

        /// <inheritdoc />
        [DataMember]
        public string Name
        {
            get => this.name;
            set
            {
                if (this.Session != null && this.Session.Partitions.Any(p => p.Name == value))
                {
                    // partition names must be unique
                    throw new InvalidOperationException($"Session already contains a partition named {value}");
                }

                this.name = value;
            }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public bool IsStoreValid => this.isStoreValid;

        /// <inheritdoc />
        [IgnoreDataMember]
        public TimeInterval MessageOriginatingTimeInterval { get; private set; } = TimeInterval.Empty;

        /// <inheritdoc />
        [IgnoreDataMember]
        public TimeInterval MessageCreationTimeInterval { get; private set; } = TimeInterval.Empty;

        /// <inheritdoc />
        [IgnoreDataMember]
        public TimeInterval TimeInterval { get; private set; } = TimeInterval.Empty;

        /// <inheritdoc />
        [IgnoreDataMember]
        public long? Size { get; private set; }

        /// <inheritdoc />
        [IgnoreDataMember]
        public int? StreamCount { get; private set; }

        /// <summary>
        /// Gets the data store reader for this partition.
        /// </summary>
        [IgnoreDataMember]
        public TStreamReader StreamReader
        {
            get => this.streamReader;
            private set
            {
                this.streamReader = value;
                if (this.streamReader != null)
                {
                    // Set originating time interval from the reader metadata
                    this.MessageOriginatingTimeInterval = this.streamReader.MessageOriginatingTimeInterval;
                    this.MessageCreationTimeInterval = this.streamReader.MessageCreationTimeInterval;
                    this.TimeInterval = this.streamReader.StreamTimeInterval;
                    this.Size = this.streamReader.Size;
                    this.StreamCount = this.streamReader.StreamCount;
                }
            }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public Session Session { get; set; }

        /// <inheritdoc />
        [DataMember]
        public string StoreName { get; private set; }

        /// <inheritdoc />
        [DataMember]
        [JsonConverter(typeof(RelativePathConverter))]
        public string StorePath { get; private set; }

        /// <inheritdoc />
        [DataMember]
        public string StreamReaderTypeName { get; private set; }

        /// <inheritdoc />
        [IgnoreDataMember]
        public IEnumerable<IStreamMetadata> AvailableStreams => this.IsStoreValid ? this.StreamReader.AvailableStreams : emptyStreamMetadataCollection;

        /// <inheritdoc />
        public void Dispose()
        {
            this.streamReader?.Dispose();
        }

        private void Initialize(Session session, TStreamReader streamReader, string name, string storeName, string storePath)
        {
            this.Session = session;
            this.Name = name;
            this.StoreName = storeName;
            this.StorePath = storePath;

            if (streamReader != null)
            {
                this.StreamReaderTypeName = streamReader.GetType().AssemblyQualifiedName;
                this.StreamReader = streamReader;
                this.Name = name ?? streamReader.Name;
            }
        }
    }
}
