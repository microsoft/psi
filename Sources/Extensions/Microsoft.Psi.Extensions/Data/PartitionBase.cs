// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Extensions.Base;

    /// <summary>
    /// Defines a base calls of partitions that can be added to a session.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class PartitionBase : ObservableObject, IPartition
    {
        private ISimpleReader reader;
        private Session session;
        private string storeName;
        private string storePath;
        private string name;
        private Type simpleReaderType;
        private IStreamTreeNode streamTreeRoot;
        private TimeInterval originatingTimeInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionBase"/> class.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="name">The partition name.</param>
        /// <param name="simpleReaderType">The SimpleReader type</param>
        protected PartitionBase(Session session, string storeName, string storePath, string name, Type simpleReaderType)
        {
            this.session = session;
            this.storeName = storeName;
            this.storePath = storePath;
            this.name = name ?? storeName;
            this.simpleReaderType = simpleReaderType;
            this.InitNew();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionBase"/> class.
        /// </summary>
        protected PartitionBase()
        {
        }

        /// <inheritdoc />
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public TimeInterval OriginatingTimeInterval
        {
            get { return this.originatingTimeInterval; }
            protected set { this.Set(nameof(this.OriginatingTimeInterval), ref this.originatingTimeInterval, value); }
        }

        /// <summary>
        /// Gets or setst the data store reader for this partition.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ISimpleReader Reader
        {
            get => this.reader;
            protected set => this.reader = value;
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public Session Session
        {
            get { return this.session; }
            set { this.Set(nameof(this.Session), ref this.session, value); }
        }

        /// <inheritdoc />
        [DataMember]
        public string StoreName
        {
            get { return this.storeName; }
            protected set { this.Set(nameof(this.StoreName), ref this.storeName, value); }
        }

        /// <inheritdoc />
        [DataMember]
        public string StorePath
        {
            get { return this.storePath; }
            protected set { this.Set(nameof(this.StorePath), ref this.storePath, value); }
        }

        /// <summary>
        /// Gets or sets the root stream tree node of this partition.
        /// </summary>
        [Browsable(false)]
        public IStreamTreeNode StreamTreeRoot
        {
            get { return this.streamTreeRoot; }
            set { this.Set(nameof(this.StreamTreeRoot), ref this.streamTreeRoot, value); }
        }

        /// <inheritdoc />
        public Type SimpleReaderType => this.simpleReaderType;

        /// <inheritdoc />
        public void RemovePartition()
        {
            this.session.RemovePartition(this);
        }

        /// <summary>
        /// After initialization, called by InitNew to create stream tree from available streams.
        /// </summary>
        protected void CreateStreamTree()
        {
            this.OriginatingTimeInterval = this.Reader.OriginatingTimeRange();
            this.StreamTreeRoot = new StreamTreeNode(this);
            foreach (var stream in this.Reader.AvailableStreams)
            {
                this.StreamTreeRoot.AddPath(stream);
            }
        }

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
