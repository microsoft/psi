// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents information needed to uniquely identify and open a stream.
    /// </summary>
    [Serializable]
    [DataContract]
    public class StreamBinding
    {
        private IStreamAdapter streamAdapter;
        private Type streamAdapterType;
        private ISummarizer summarizer;
        private Type summarizerType;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBinding"/> class.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <param name="nodePath">The path of the node in the tree that has generated this stream binding.</param>
        /// <param name="streamAdapterType">The type of the stream adapter, null if there is none.</param>
        /// <param name="streamAdapterArguments">The arguments used when constructing the stream adapter, null if there are none.</param>
        /// <param name="summarizerType">The type of the stream summarizer, null if there is none.</param>
        /// <param name="summarizerArguments">The arguments used when constructing the stream summarizer, null if there are none.</param>
        /// <param name="isDerived">True if this stream binding represents a binding to a derived stream rather than to the stream itself, otherwise false.</param>
        public StreamBinding(
            string streamName,
            string partitionName,
            string nodePath,
            Type streamAdapterType = null,
            object[] streamAdapterArguments = null,
            Type summarizerType = null,
            object[] summarizerArguments = null,
            bool isDerived = false)
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
            this.NodePath = nodePath;
            this.StreamAdapterType = streamAdapterType;
            this.StreamAdapterArguments = streamAdapterArguments;
            this.SummarizerType = summarizerType;
            this.SummarizerArguments = summarizerArguments;
            this.IsDerived = isDerived;
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
        /// Gets the node path.
        /// </summary>
        [DataMember]
        public string NodePath { get; private set; }

        /// <summary>
        /// Gets stream adapter.
        /// </summary>
        [IgnoreDataMember]
        public IStreamAdapter StreamAdapter
        {
            get
            {
                if (this.streamAdapter == null)
                {
                    this.streamAdapter = this.StreamAdapterType != null ? (IStreamAdapter)Activator.CreateInstance(this.StreamAdapterType, this.StreamAdapterArguments) : null;
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
        /// Gets or sets the stream adapter arguments needed by the ctor of the stream adapter.
        /// </summary>
        [DataMember]
        public object[] StreamAdapterArguments { get; set; }

        /// <summary>
        /// Gets a value indicating whether this stream binding represents a binding to a member of the data in the stream rather than to the stream itself.
        /// </summary>
        [DataMember]
        public bool IsDerived { get; private set; }

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
                // update value and update type name
                this.streamAdapterType = value;

                // use assembly-qualified name as stream adapter may be in a different assembly
                this.StreamAdapterTypeName = this.streamAdapterType?.AssemblyQualifiedName;
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
                    this.summarizer = this.SummarizerType != null ? (ISummarizer)Activator.CreateInstance(this.SummarizerType, this.SummarizerArguments) : null;
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
        public object[] SummarizerArguments { get; set; }

        /// <summary>
        /// Gets summarizer type.
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

                // use assembly-qualified name as stream reader may be in a different assembly
                this.SummarizerTypeName = this.summarizerType?.AssemblyQualifiedName;
            }
        }

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
    }
}
