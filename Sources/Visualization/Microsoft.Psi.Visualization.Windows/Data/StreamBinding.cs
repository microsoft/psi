// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Adapters;

    /// <summary>
    /// Represents information needed to uniquely identify a source or derived stream.
    /// </summary>
    /// <remarks>
    /// A stream binding contains an overall stream adapter that is formed by chaining
    /// a derived stream adapter (used to compute the values for the derived stream from
    /// the source stream) and a visualizer adapter (used to adapt the values produced by
    /// the stream to the visualizer that the binding connects to). In addition, the
    /// binding contains information about the summarizer used by the bound visualizer.
    /// </remarks>
    [Serializable]
    [DataContract]
    public class StreamBinding
    {
        private IStreamAdapter streamAdapter;
        private IStreamAdapter derivedStreamAdapter;
        private Type derivedStreamAdapterType;
        private IStreamAdapter visualizerStreamAdapter;
        private Type visualizerStreamAdapterType;
        private ISummarizer visualizerSummarizer;
        private Type visualizerSummarizerType;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamBinding"/> class.
        /// </summary>
        /// <param name="sourceStreamName">The source stream name.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <param name="streamName">An optional parameter specifying the name of the stream (if not specified, defaults to the source stream name).</param>
        /// <param name="derivedStreamAdapterType">An optional parameter specifying the type of the stream adapter used to compute the derived stream, null if there is none.</param>
        /// <param name="derivedStreamAdapterArguments">An optional parameter specifying the arguments for constructing the stream adapter used to compute the derived stream, null if there are none.</param>
        /// <param name="visualizerStreamAdapterType">An optional parameter specifying the type of the stream adapter used to couple the stream to the visualizer, null if there is none.</param>
        /// <param name="visualizerStreamAdapterArguments">An optional parameter specifying the arguments for constructing the stream adapter used to couple the stream to the visualizer, null if there are none.</param>
        /// <param name="visualizerSummarizerType">An optional parameter specifying the type of the stream summarizer used by the visualizer, null if there is none.</param>
        /// <param name="visualizerSummarizerArguments">An optional parameter specifying the arguments used when constructing the stream summarizer used by the visualizer, null if there are none.</param>
        public StreamBinding(
            string sourceStreamName,
            string partitionName,
            string streamName = null,
            Type derivedStreamAdapterType = null,
            object[] derivedStreamAdapterArguments = null,
            Type visualizerStreamAdapterType = null,
            object[] visualizerStreamAdapterArguments = null,
            Type visualizerSummarizerType = null,
            object[] visualizerSummarizerArguments = null)
        {
            if (string.IsNullOrWhiteSpace(sourceStreamName))
            {
                throw new ArgumentNullException(nameof(sourceStreamName));
            }

            if (string.IsNullOrWhiteSpace(partitionName))
            {
                throw new ArgumentNullException(nameof(partitionName));
            }

            this.PartitionName = partitionName;
            this.SourceStreamName = sourceStreamName;
            this.StreamName = streamName ?? sourceStreamName;
            this.DerivedStreamAdapterType = derivedStreamAdapterType;
            this.DerivedStreamAdapterArguments = derivedStreamAdapterArguments;
            this.VisualizerStreamAdapterType = visualizerStreamAdapterType;
            this.VisualizerStreamAdapterArguments = visualizerStreamAdapterArguments;
            this.VisualizerSummarizerType = visualizerSummarizerType;
            this.VisualizerSummarizerArguments = visualizerSummarizerArguments;
        }

        private StreamBinding()
        {
            // Called only by JSON deserializer
        }

        /// <summary>
        /// Gets the partition name.
        /// </summary>
        [DataMember]
        public string PartitionName { get; }

        /// <summary>
        /// Gets the source stream name.
        /// </summary>
        [DataMember]
        public string SourceStreamName { get; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        /// <remarks>
        /// In the case of derived streams, the stream name is different from the source
        /// stream name.
        /// </remarks>
        [DataMember]
        public string StreamName { get; }

        /// <summary>
        /// Gets a value indicating whether the binding is to a derived stream.
        /// </summary>
        [IgnoreDataMember]
        public bool IsBindingToDerivedStream => this.DerivedStreamAdapter != null;

        /// <summary>
        /// Gets the end-to-end stream adapter.
        /// </summary>
        /// <remarks>
        /// The end-to-end stream adapter for the binding composes any existing
        /// derived stream adapter and visualizer adapter.
        /// </remarks>
        [IgnoreDataMember]
        public IStreamAdapter StreamAdapter
        {
            get
            {
                if (this.streamAdapter == null)
                {
                    Type streamAdapterType;
                    object[] streamAdapterArguments;
                    if (this.DerivedStreamAdapterType != null)
                    {
                        if (this.VisualizerStreamAdapterType != null)
                        {
                            streamAdapterType = typeof(ChainedStreamAdapter<,,,,>).MakeGenericType(
                                this.DerivedStreamAdapter.SourceType,
                                this.DerivedStreamAdapter.DestinationType,
                                this.VisualizerStreamAdapter.DestinationType,
                                this.DerivedStreamAdapterType,
                                this.VisualizerStreamAdapterType);
                            streamAdapterArguments = new object[] { this.DerivedStreamAdapterArguments, this.VisualizerStreamAdapterArguments };
                        }
                        else
                        {
                            streamAdapterType = this.DerivedStreamAdapterType;
                            streamAdapterArguments = this.DerivedStreamAdapterArguments;
                        }
                    }
                    else
                    {
                        streamAdapterType = this.VisualizerStreamAdapterType;
                        streamAdapterArguments = this.VisualizerStreamAdapterArguments;
                    }

                    this.streamAdapter = streamAdapterType != null ? (IStreamAdapter)Activator.CreateInstance(streamAdapterType, streamAdapterArguments) : null;
                }

                return this.streamAdapter;
            }
        }

        /// <summary>
        /// Gets the derived stream adapter.
        /// </summary>
        /// <remarks>
        /// The derived stream adapter is used to compute values for the derived
        /// stream based on the source stream.
        /// </remarks>
        [IgnoreDataMember]
        public IStreamAdapter DerivedStreamAdapter
        {
            get
            {
                if (this.derivedStreamAdapter == null)
                {
                    this.derivedStreamAdapter = this.DerivedStreamAdapterType != null ? (IStreamAdapter)Activator.CreateInstance(this.DerivedStreamAdapterType, this.DerivedStreamAdapterArguments) : null;
                }

                return this.derivedStreamAdapter;
            }
        }

        /// <summary>
        /// Gets the derived stream adapter type.
        /// </summary>
        [IgnoreDataMember]
        public Type DerivedStreamAdapterType
        {
            get
            {
                if (this.derivedStreamAdapterType == null && this.DerivedStreamAdapterTypeName != null)
                {
                    this.derivedStreamAdapterType = TypeResolutionHelper.GetVerifiedType(this.DerivedStreamAdapterTypeName);
                }

                return this.derivedStreamAdapterType;
            }

            private set
            {
                // update value and update type name
                this.derivedStreamAdapterType = value;

                // use assembly-qualified name as stream adapter may be in a different assembly
                this.DerivedStreamAdapterTypeName = this.derivedStreamAdapterType?.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets the derived stream adapter arguments.
        /// </summary>
        [DataMember]
        public object[] DerivedStreamAdapterArguments { get; }

        /// <summary>
        /// Gets visualizer stream adapter.
        /// </summary>
        /// <remarks>
        /// The visualizer stream adapter is used to adapt the values of the stream
        /// to the visualizer used.
        /// </remarks>
        [IgnoreDataMember]
        public IStreamAdapter VisualizerStreamAdapter
        {
            get
            {
                if (this.visualizerStreamAdapter == null)
                {
                    this.visualizerStreamAdapter = this.VisualizerStreamAdapterType != null ? (IStreamAdapter)Activator.CreateInstance(this.VisualizerStreamAdapterType, this.VisualizerStreamAdapterArguments) : null;
                }

                return this.visualizerStreamAdapter;
            }
        }

        /// <summary>
        /// Gets visualizer stream adapter type.
        /// </summary>
        [IgnoreDataMember]
        public Type VisualizerStreamAdapterType
        {
            get
            {
                if (this.visualizerStreamAdapterType == null && this.VisualizerStreamAdapterTypeName != null)
                {
                    this.visualizerStreamAdapterType = TypeResolutionHelper.GetVerifiedType(this.VisualizerStreamAdapterTypeName);
                }

                return this.visualizerStreamAdapterType;
            }

            private set
            {
                // update value and update type name
                this.visualizerStreamAdapterType = value;

                // use assembly-qualified name as stream adapter may be in a different assembly
                this.VisualizerStreamAdapterTypeName = this.visualizerStreamAdapterType?.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets the visualizer stream adapter arguments.
        /// </summary>
        [DataMember]
        public object[] VisualizerStreamAdapterArguments { get; }

        /// <summary>
        /// Gets the summarizer.
        /// </summary>
        [IgnoreDataMember]
        public ISummarizer Summarizer
        {
            get
            {
                if (this.visualizerSummarizer == null)
                {
                    this.visualizerSummarizer = this.VisualizerSummarizerType != null ? (ISummarizer)Activator.CreateInstance(this.VisualizerSummarizerType, this.VisualizerSummarizerArguments) : null;
                }

                return this.visualizerSummarizer;
            }

            private set
            {
                // update value and update type (and type name) as well
                this.visualizerSummarizer = value;
                this.VisualizerSummarizerType = this.visualizerSummarizer?.GetType();
            }
        }

        /// <summary>
        /// Gets the summarizer type.
        /// </summary>
        [IgnoreDataMember]
        public Type VisualizerSummarizerType
        {
            get
            {
                if (this.visualizerSummarizerType == null && this.SummarizerTypeName != null)
                {
                    this.visualizerSummarizerType = TypeResolutionHelper.GetVerifiedType(this.SummarizerTypeName);
                }

                return this.visualizerSummarizerType;
            }

            private set
            {
                // update value and update type name
                this.visualizerSummarizerType = value;

                // use assembly-qualified name as stream reader may be in a different assembly
                this.SummarizerTypeName = this.visualizerSummarizerType?.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets the summarizer arguments.
        /// </summary>
        [DataMember]
        public object[] VisualizerSummarizerArguments { get; }

        /// <summary>
        /// Gets or sets the derived stream adapter type name.
        /// </summary>
        [DataMember]
        private string DerivedStreamAdapterTypeName { get; set; }

        /// <summary>
        /// Gets or sets the visualizer stream adapter type name.
        /// </summary>
        [DataMember]
        private string VisualizerStreamAdapterTypeName { get; set; }

        /// <summary>
        /// Gets or sets the summarizer type name.
        /// </summary>
        [DataMember]
        private string SummarizerTypeName { get; set; }
    }
}
