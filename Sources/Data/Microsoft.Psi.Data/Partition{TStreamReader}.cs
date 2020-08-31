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
        private TStreamReader streamReader;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Partition{TStreamReader}"/> class.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="streamReader">Stream reader used to read partition.</param>
        /// <param name="name">The partition name.</param>
        public Partition(Session session, TStreamReader streamReader, string name)
        {
            this.Session = session;
            this.StreamReaderTypeName = streamReader.GetType().AssemblyQualifiedName;
            this.StreamReader = streamReader;
            this.Name = name ?? streamReader.Name;
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
            : this(session, (TStreamReader)Data.StreamReader.Create(storeName, storePath, streamReaderTypeName), name)
        {
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
        public TimeInterval OriginatingTimeInterval { get; private set; }

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
                    this.OriginatingTimeInterval = this.streamReader.MessageOriginatingTimeInterval;
                }
            }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public Session Session { get; set; }

        /// <inheritdoc />
        [DataMember]
        public string StoreName => this.StreamReader.Name;

        /// <inheritdoc />
        [DataMember]
        [JsonConverter(typeof(RelativePathConverter))]
        public string StorePath => this.StreamReader.Path;

        /// <inheritdoc />
        [DataMember]
        public string StreamReaderTypeName { get; private set; }

        /// <inheritdoc />
        [IgnoreDataMember]
        public IEnumerable<IStreamMetadata> AvailableStreams => this.StreamReader?.AvailableStreams;

        /// <inheritdoc />
        public void Dispose()
        {
            this.streamReader.Dispose();
        }
    }
}
