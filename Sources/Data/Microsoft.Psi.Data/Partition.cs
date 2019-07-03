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
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class Partition : IPartition
    {
        private ISimpleReader reader;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Partition"/> class.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="name">The partition name.</param>
        /// <param name="simpleReaderType">The SimpleReader type.</param>
        protected Partition(Session session, string storeName, string storePath, string name, Type simpleReaderType)
        {
            this.Session = session;
            this.StoreName = storeName;
            this.StorePath = storePath;
            this.Name = name ?? storeName;
            this.InitNew();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Partition"/> class.
        /// </summary>
        protected Partition()
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
        /// Gets or sets the data store reader for this partition.
        /// </summary>
        [IgnoreDataMember]
        public ISimpleReader Reader
        {
            get => this.reader;
            protected set
            {
                this.reader = value;
                if (this.reader != null)
                {
                    // Set originating time interval from the reader metadata
                    this.OriginatingTimeInterval = this.reader.OriginatingTimeRange();
                }
            }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public Session Session { get; set; }

        /// <inheritdoc />
        [DataMember]
        public string StoreName { get; protected set; }

        /// <inheritdoc />
        [DataMember]
        [JsonConverter(typeof(RelativePathConverter))]
        public string StorePath { get; protected set; }

        /// <inheritdoc />
        [IgnoreDataMember]
        public IEnumerable<IStreamMetadata> AvailableStreams => this.Reader?.AvailableStreams;

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
        }
    }
}
