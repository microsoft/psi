// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents information needed to uniquely identify and open a stream.
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StreamBinding
    {
        private IStreamAdapter streamAdapter;
        private Type streamAdapterType;
        private ISummarizer summarizer;
        private Type summarizerType;
        private Type simpleReaderType;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBinding"/> class.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="storePath">The store path.</param>
        /// <param name="simpleReaderType">The simple reader type for the underlying store.</param>
        /// <param name="streamAdapterType">The type of the stream adapter, null if there is none.</param>
        /// <param name="summarizerType">The type of the stream summarizer, null if there is none.</param>
        /// <param name="summarizerArgs">The arguments used when constructing the stream summarizer, null if ther is none.</param>
        public StreamBinding(
            string streamName,
            string partitionName,
            string storeName,
            string storePath,
            Type simpleReaderType,
            Type streamAdapterType = null,
            Type summarizerType = null,
            object[] summarizerArgs = null)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            if (string.IsNullOrWhiteSpace(partitionName))
            {
                throw new ArgumentNullException(nameof(partitionName));
            }

            if (string.IsNullOrWhiteSpace(storeName))
            {
                throw new ArgumentNullException(nameof(storeName));
            }

            // storePath can be null, but not empty - this is to support volatile data stores
            if (storePath == string.Empty)
            {
                throw new ArgumentException("storePath must either be null (volatile data store) or contain a path", nameof(storeName));
            }

            this.StreamName = streamName;
            this.PartitionName = partitionName;
            this.StoreName = storeName;
            this.StorePath = storePath;
            this.SimpleReaderType = simpleReaderType;
            this.StreamAdapterType = streamAdapterType;
            this.SummarizerType = summarizerType;
            this.SummarizerArgs = summarizerArgs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBinding"/> class.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="storePath">The store path.</param>
        /// <param name="simpleReaderType">The simple reader type for the underlying store.</param>
        /// <param name="streamAdapterTypeName">The type name of the stream adapter, null if there is none.</param>
        /// <param name="summarizerTypeName">The type name of the stream summarizer, null if there is none.</param>
        /// <param name="summarizerArgs">The arguments used when constructing the stream summarizer, null if ther is none.</param>
        public StreamBinding(
            string streamName,
            string partitionName,
            string storeName,
            string storePath,
            Type simpleReaderType,
            string streamAdapterTypeName,
            string summarizerTypeName = null,
            object[] summarizerArgs = null)
            : this(streamName, partitionName, storeName, storePath, simpleReaderType)
        {
            this.StreamAdapterTypeName = streamAdapterTypeName;
            this.SummarizerTypeName = summarizerTypeName;
            this.SummarizerArgs = summarizerArgs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBinding"/> class.
        /// </summary>
        /// <param name="source">An existing stream binding to clone.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="storePath">The store path.</param>
        public StreamBinding(StreamBinding source, string storeName, string storePath)
            : this(source.StreamName, source.PartitionName, storeName, storePath, source.SimpleReaderType, source.StreamAdapterType, source.SummarizerType, source.SummarizerArgs)
        {
            this.streamAdapter = source.streamAdapter;
            this.summarizer = source.summarizer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBinding"/> class.
        /// </summary>
        /// <param name="source">An existing stream binding to clone.</param>
        /// <param name="summarizerType">The type of the stream summarizer, null if there is none.</param>
        /// <param name="summarizerArgs">The arguments used when constructing the stream summarizer, null if ther is none.</param>
        public StreamBinding(StreamBinding source, Type summarizerType, object[] summarizerArgs)
            : this(source.StreamName, source.PartitionName, source.StoreName, source.StorePath, source.SimpleReaderType, source.StreamAdapterType, summarizerType, summarizerArgs)
        {
            this.streamAdapter = source.streamAdapter;

            // Do not copy this over since the type or args may have changed
            this.summarizer = null;
        }

        private StreamBinding()
        {
        }

        /// <summary>
        /// Gets stream name.
        /// </summary>
        [DataMember]
        public string StreamName { get; internal set; }

        /// <summary>
        /// Gets partition name.
        /// </summary>
        [DataMember]
        public string PartitionName { get; private set; }

        /// <summary>
        /// Gets store name.
        /// </summary>
        [DataMember]
        public string StoreName { get; private set; }

        /// <summary>
        /// Gets store path.
        /// </summary>
        [DataMember]
        public string StorePath { get; private set; }

        /// <summary>
        /// Gets stream adapater.
        /// </summary>
        [IgnoreDataMember]
        public IStreamAdapter StreamAdapter
        {
            get
            {
                if (this.streamAdapter == null)
                {
                    this.streamAdapter = this.StreamAdapterType != null ? (IStreamAdapter)Activator.CreateInstance(this.StreamAdapterType) : null;
                }

                return this.streamAdapter;
            }

            private set
            {
                // update value and update type (and type name) as well
                this.streamAdapter = value;
                this.StreamAdapterType = this.streamAdapter?.GetType();
            }
        }

        /// <summary>
        /// Gets stream adapter type.
        /// </summary>
        [IgnoreDataMember]
        public Type StreamAdapterType
        {
            get
            {
                if (this.streamAdapterType == null && this.StreamAdapterTypeName != null)
                {
                    this.streamAdapterType = Type.GetType(this.StreamAdapterTypeName);
                    if (this.streamAdapterType == null)
                    {
                        var assembly = Assembly.GetEntryAssembly();
                        this.streamAdapterType = assembly.GetType(this.StreamAdapterTypeName);
                    }
                }

                return this.streamAdapterType;
            }

            private set
            {
                // validate type has proper constructor
                var ctor = value?.GetConstructor(new Type[] { });
                if (value != null && ctor == null)
                {
                    throw new ArgumentException("StreamAdapter derived types must have a public, zero parameter constructor.");
                }

                // update value and update type name
                this.streamAdapterType = value;
                this.StreamAdapterTypeName = this.streamAdapterType?.FullName;
            }
        }

        /// <summary>
        /// Gets stream adapter type.
        /// </summary>
        [IgnoreDataMember]
        public Type SimpleReaderType
        {
            get
            {
                if (this.simpleReaderType == null && this.SimpleReaderTypeName != null)
                {
                    this.simpleReaderType = Type.GetType(this.SimpleReaderTypeName);
                }

                return this.simpleReaderType;
            }

            private set
            {
                // validate type has proper constructor
                var ctor = value?.GetConstructor(new Type[] { });
                if (value != null && ctor == null)
                {
                    throw new ArgumentException("SimpleReader derived types must have a public, zero parameter constructor.");
                }

                // update value and update type name
                this.simpleReaderType = value;

                // use assembly-qualified name as simple reader may be in a different assembly
                this.SimpleReaderTypeName = this.simpleReaderType?.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets summarizer.
        /// </summary>
        [IgnoreDataMember]
        public ISummarizer Summarizer
        {
            get
            {
                if (this.summarizer == null)
                {
                    this.summarizer = this.SummarizerType != null ? (ISummarizer)Activator.CreateInstance(this.SummarizerType, this.SummarizerArgs) : null;
                }

                return this.summarizer;
            }

            private set
            {
                // update value and update type (and type name) as well
                this.summarizer = value;
                this.SummarizerType = this.summarizer?.GetType();
            }
        }

        /// <summary>
        /// Gets summarizer arguments.
        /// </summary>
        [DataMember]
        public object[] SummarizerArgs { get; private set; }

        /// <summary>
        /// Gets summaraizer type.
        /// </summary>
        [IgnoreDataMember]
        public Type SummarizerType
        {
            get
            {
                if (this.summarizerType == null && this.SummarizerTypeName != null)
                {
                    this.summarizerType = Type.GetType(this.SummarizerTypeName);
                    if (this.summarizerType == null)
                    {
                        var assembly = Assembly.GetEntryAssembly();
                        this.summarizerType = assembly.GetType(this.SummarizerTypeName);
                    }
                }

                return this.summarizerType;
            }

            private set
            {
                // update value and update type name
                this.summarizerType = value;
                this.SummarizerTypeName = this.summarizerType?.FullName;
            }
        }

        /// <summary>
        /// Gets or sets stream adapter type name.
        /// </summary>
        [DataMember]
        private string StreamAdapterTypeName { get; set; }

        /// <summary>
        /// Gets or sets the SimpleReader type name
        /// </summary>
        [DataMember]
        private string SimpleReaderTypeName { get; set; }

        /// <summary>
        /// Gets or sets summarizer type name.
        /// </summary>
        [DataMember]
        private string SummarizerTypeName { get; set; }
    }
}
