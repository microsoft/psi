// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

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
        /// <param name="simpleReaderType">The simple reader type for the underlying store.</param>
        /// <param name="streamAdapterType">The type of the stream adapter, null if there is none.</param>
        /// <param name="summarizerType">The type of the stream summarizer, null if there is none.</param>
        /// <param name="summarizerArgs">The arguments used when constructing the stream summarizer, null if ther is none.</param>
        public StreamBinding(
            string streamName,
            string partitionName,
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

            this.StreamName = streamName;
            this.PartitionName = partitionName;
            this.SimpleReaderType = simpleReaderType;
            this.StreamAdapterType = streamAdapterType;
            this.SummarizerType = summarizerType;
            this.SummarizerArgs = summarizerArgs;
        }

        private StreamBinding()
        {
            // Called only by JSON deserializer
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
        [IgnoreDataMember]
        public string StoreName { get; private set; }

        /// <summary>
        /// Gets store path.
        /// </summary>
        [IgnoreDataMember]
        public string StorePath { get; private set; }

        /// <summary>
        /// Gets the metadata for the underlying stream being bound to, or null.
        /// </summary>
        public IStreamMetadata StreamMetadata { get; private set; }

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
                    this.streamAdapterType = TypeResolutionHelper.GetVerifiedType(this.StreamAdapterTypeName);
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
                    this.simpleReaderType = TypeResolutionHelper.GetVerifiedType(this.SimpleReaderTypeName);
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
        /// Gets  the SimpleReader type name.
        /// </summary>
        [DataMember]
        public string SimpleReaderTypeName { get; private set; }

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
        /// Gets or sets the summarizer arguments.
        /// </summary>
        [DataMember]
        public object[] SummarizerArgs { get; set; }

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
                    this.summarizerType = TypeResolutionHelper.GetVerifiedType(this.SummarizerTypeName);
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
        /// Gets a value indicating whether the stream is bound to a data source.
        /// </summary>
        [IgnoreDataMember]
        public bool IsBound => !string.IsNullOrWhiteSpace(this.StoreName) && !string.IsNullOrWhiteSpace(this.StorePath);

        /// <summary>
        /// Gets or sets stream adapter type name.
        /// </summary>
        [DataMember]
        private string StreamAdapterTypeName { get; set; }

        /// <summary>
        /// Gets or sets summarizer type name.
        /// </summary>
        [DataMember]
        private string SummarizerTypeName { get; set; }

        /// <summary>
        /// Updates a stream binding in response to a session being opened or closed.
        /// </summary>
        /// <param name="session">The session to attempt to bind to.</param>
        /// <returns>The result of the binding update operation.</returns>
        public StreamBindingResult Update(Session session)
        {
            // If there's no session, then we have nothing to bind to
            if (session != null)
            {
                // Check that a partition with the required name exists in the session
                IPartition partition = session.Partitions.FirstOrDefault(p => p.Name == this.PartitionName);
                if (partition != null)
                {
                    // Check that the partition contains a stream with the same name as this binding object
                    IStreamMetadata streamMetadata = partition.AvailableStreams.FirstOrDefault(s => s.Name == this.StreamName);
                    if (streamMetadata != null)
                    {
                        // Check if the binding has actually changed
                        if ((this.StoreName == partition.StoreName) && (this.StorePath == partition.StorePath) && (this.StreamMetadata == streamMetadata))
                        {
                            return StreamBindingResult.BindingUnchanged;
                        }

                        this.StoreName = partition.StoreName;
                        this.StorePath = partition.StorePath;
                        this.StreamMetadata = streamMetadata;
                        return StreamBindingResult.BoundToNewSource;
                    }
                }
            }

            this.StoreName = null;
            this.StorePath = null;
            this.StreamMetadata = null;
            return StreamBindingResult.NoSourceToBindTo;
        }
    }
}
