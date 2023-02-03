// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Implements a node in the dataset tree that represents a stream.
    /// </summary>
    /// <remarks>
    /// Stream tree nodes can represent (1) source streams that are present in a store,
    /// (2) derived streams, i.e., streams computed from a source stream based on a
    /// derived stream adapter, or (3) stream containers (that subsume other streams).
    /// </remarks>
    public class StreamTreeNode : ObservableTreeNodeObject
    {
        private readonly ObservableCollection<StreamTreeNode> internalChildren;

        private string auxiliaryInfo = string.Empty;

        private RelayCommand<MouseButtonEventArgs> mouseDoubleClickCommand;
        private RelayCommand<Grid> contextMenuOpeningCommand;

        private bool isDirty = false;
        private bool? supplementalMetadataTypeIsKnown = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the stream tree node.</param>
        /// <param name="fullName">The full, hierarchical name of the stream tree node.</param>
        /// <param name="sourceStreamMetadata">The source stream metadata.</param>
        /// <param name="derivedStreamAdapterType">The adapter type for constructing a derived stream tree node.</param>
        /// <param name="derivedStreamAdapterArguments">The adapter arguments for constructing a derived stream tree node.</param>
        private StreamTreeNode(
            PartitionViewModel partitionViewModel,
            string fullName,
            IStreamMetadata sourceStreamMetadata,
            Type derivedStreamAdapterType,
            object[] derivedStreamAdapterArguments)
        {
            // Sanity check that if we have a derived stream, we have an adapter
            if (sourceStreamMetadata != null && ((sourceStreamMetadata.Name != fullName) != (derivedStreamAdapterType != null)))
            {
                throw new ArgumentException("Attempting to construct a derived stream tree node without an adapter or a regular stream tree node with an adapter.");
            }

            this.PartitionViewModel = partitionViewModel;
            this.PartitionViewModel.PropertyChanged += this.OnPartitionViewModelPropertyChanged;
            this.DatasetViewModel.PropertyChanged += this.OnDatasetViewModelPropertyChanged;

            this.FullName = fullName;
            this.Name = this.FullName?.Split('.').Last();

            this.internalChildren = new ();
            this.Children = new ReadOnlyObservableCollection<StreamTreeNode>(this.internalChildren);

            this.SourceStreamMetadata = sourceStreamMetadata;
            this.DerivedStreamAdapterType = derivedStreamAdapterType;
            this.DerivedStreamAdapterArguments = derivedStreamAdapterArguments;

            // Compute the data type for the node, based on whether or not we have a derived stream adapter
            if (this.DerivedStreamAdapterType != null)
            {
                var derivedStreamAdapter = (IStreamAdapter)Activator.CreateInstance(this.DerivedStreamAdapterType, this.DerivedStreamAdapterArguments);
                this.DataType = derivedStreamAdapter.DestinationType;
            }
            else if (sourceStreamMetadata != null)
            {
                this.DataType = VisualizationContext.Instance.GetDataType(this.SourceStreamMetadata.TypeName);
            }

            this.HasSourceStreamDescendants = false;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        ~StreamTreeNode()
        {
            this.PartitionViewModel.PropertyChanged -= this.OnPartitionViewModelPropertyChanged;
            this.DatasetViewModel.PropertyChanged -= this.OnDatasetViewModelPropertyChanged;
        }

        /// <summary>
        /// Gets the dataset for the stream tree node.
        /// </summary>
        [Browsable(false)]
        public DatasetViewModel DatasetViewModel => this.PartitionViewModel.SessionViewModel.DatasetViewModel;

        /// <summary>
        /// Gets the session for the stream tree node.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel SessionViewModel => this.PartitionViewModel.SessionViewModel;

        /// <summary>
        /// Gets the partition for the stream tree node.
        /// </summary>
        [Browsable(false)]
        public PartitionViewModel PartitionViewModel { get; }

        /// <summary>
        /// Gets the full, hierarchical name of the stream tree node.
        /// </summary>
        [Browsable(false)]
        public string FullName { get; }

        /// <summary>
        /// Gets the name of the stream tree node.
        /// </summary>
        [Browsable(false)]
        public string Name { get; }

        /// <summary>
        /// Gets the name to display in the stream tree.
        /// </summary>
        [Browsable(false)]
        public string DisplayString => this.IsDirty ? $"{this.Name}*" : this.Name;

        /// <summary>
        /// Gets the metadata of the source data stream.
        /// </summary>
        [Browsable(false)]
        public IStreamMetadata SourceStreamMetadata { get; }

        /// <summary>
        /// Gets the type of data represented by this stream tree node.
        /// </summary>
        [Browsable(false)]
        public Type DataType { get; }

        /// <summary>
        /// Gets a value indicating whether type of data represented by this stream tree node is nullable.
        /// </summary>
        [Browsable(false)]
        public bool IsNullableDataType => this.DataType != null && Nullable.GetUnderlyingType(this.DataType) != null;

        /// <summary>
        /// Gets a string representation of the type of data represented by this stream tree node.
        /// </summary>
        [DisplayName("Data Type")]
        [Description("The type of data for this stream.")]
        public string DataTypeDisplayString => this.DataType != null ? TypeSpec.Simplify(this.DataType.AssemblyQualifiedName) : "N/A";

        /// <summary>
        /// Gets the adapter type for constructing the derived stream.
        /// </summary>
        [Browsable(false)]
        public Type DerivedStreamAdapterType { get; }

        /// <summary>
        /// Gets a string representation of the adapter type for constructing the derived stream.
        /// </summary>
        [DisplayName("Derived Stream Adapter Type")]
        [Description("The stream adapter used to compute the derived stream.")]
        public string DerivedStreamAdapterTypeDisplayString => this.DerivedStreamAdapterType != null ? TypeSpec.Simplify(this.DerivedStreamAdapterType.AssemblyQualifiedName) : "N/A";

        /// <summary>
        /// Gets the adapter parameters for constructing the derived stream.
        /// </summary>
        [Browsable(false)]
        public object[] DerivedStreamAdapterArguments { get; }

        /// <summary>
        /// Gets the collection of children for this stream tree node.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<StreamTreeNode> Children { get; }

        /// <summary>
        /// Gets a value indicating whether this node corresponds to a stream.
        /// </summary>
        [Browsable(false)]
        public bool IsStream => this.SourceStreamMetadata != null;

        /// <summary>
        /// Gets a value indicating whether this node corresponds to a stream container.
        /// </summary>
        [Browsable(false)]
        public bool IsContainer => this.SourceStreamMetadata == null;

        /// <summary>
        /// Gets a value indicating whether this node corresponds to a stream backed by a \psi store.
        /// </summary>
        [Browsable(false)]
        public bool IsPsiStream =>
            this.IsStream &&
            this.SourceStreamMetadata is PsiStreamMetadata;

        /// <summary>
        /// Gets a value indicating whether this node corresponds to an indexed stream backed by a \psi store.
        /// </summary>
        [Browsable(false)]
        public bool IsIndexedPsiStream =>
            this.IsStream &&
            this.SourceStreamMetadata is PsiStreamMetadata psiStreamMetadata &&
            psiStreamMetadata.IsIndexed;

        /// <summary>
        /// Gets a value indicating whether this node corresponds to a source stream.
        /// </summary>
        [Browsable(false)]
        public bool IsSourceStream => this.IsStream && this.DerivedStreamAdapterType == null;

        /// <summary>
        /// Gets a value indicating whether this node corresponds to a derived stream.
        /// </summary>
        [DisplayName("Is Derived Stream")]
        [Description("Indicates whether this is a derived stream.")]
        public bool IsDerivedStream => this.IsStream && this.DerivedStreamAdapterType != null;

        /// <summary>
        /// Gets a value indicating whether this node corresponds to a script derived stream.
        /// </summary>
        [DisplayName("Is Script Derived Steam")]
        [Description("Indicates whether this is a script derived stream.")]
        public bool IsScriptDerivedStream
        {
            get
            {
                var lastAdapterType = this.DerivedStreamAdapterType;

                while (lastAdapterType != null && lastAdapterType.IsGenericType)
                {
                    if (lastAdapterType.GetGenericTypeDefinition() == typeof(ScriptAdapter<,>))
                    {
                        return true;
                    }
                    else if (lastAdapterType.GetGenericTypeDefinition() == typeof(ChainedStreamAdapter<,,,,>))
                    {
                        // continue searching from the second chained adapter
                        lastAdapterType = lastAdapterType.GetGenericArguments()[4];
                    }
                    else
                    {
                        lastAdapterType = null;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this node has source stream descendants.
        /// </summary>
        [Browsable(false)]
        public bool HasSourceStreamDescendants { get; private set; }

        /// <summary>
        /// Gets the path to the icon for the node.
        /// </summary>
        [Browsable(false)]
        public string IconSource
        {
            get
            {
                if (this.SourceStreamMetadata == null)
                {
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.GroupLive : IconSourcePath.Group;
                }
                else if (this.IsDerivedStream)
                {
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.DerivedStreamLive : IconSourcePath.DerivedStream;
                }
                else if (this.DataType == typeof(AudioBuffer))
                {
                    // Audio stream
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamAudioMutedLive : IconSourcePath.StreamAudioMuted;
                }
                else if (this.DataType == typeof(PipelineDiagnostics))
                {
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.DiagnosticsLive : IconSourcePath.Diagnostics;
                }
                else if (this.DataType == typeof(TimeIntervalAnnotation))
                {
                    // Annotation
                    return IconSourcePath.Annotation;
                }
                else if (this.internalChildren.Any())
                {
                    // Group node that's also a stream
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamGroupLive : IconSourcePath.StreamGroup;
                }
                else
                {
                    // Stream
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamLive : IconSourcePath.Stream;
                }
            }
        }

        /// <summary>
        /// Gets the brush to use when rendering the node.
        /// </summary>
        [Browsable(false)]
        public Brush ForegroundBrush => ((this.IsStream && !this.IsDerivedStream) || this.HasSourceStreamDescendants) ?
            new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.LightGray);

        /// <summary>
        /// Gets the opacity to use when rendering the node.
        /// </summary>
        /// <remarks>
        /// Opacity is lowered for all nodes in sessions that are not the current session.
        /// </remarks>
        [Browsable(false)]
        public double UiElementOpacity => this.SessionViewModel.UiElementOpacity;

        /// <summary>
        /// Gets the auxiliary info to display.
        /// </summary>
        [Browsable(false)]
        public string AuxiliaryInfo
        {
            get => this.auxiliaryInfo;
            private set => this.Set(nameof(this.AuxiliaryInfo), ref this.auxiliaryInfo, value);
        }

        /// <summary>
        /// Gets a value indicating whether this node is in the current session.
        /// </summary>
        [Browsable(false)]
        public bool IsInCurrentSession => this.SessionViewModel.IsCurrentSession;

        /// <summary>
        /// Gets the id of the source stream.
        /// </summary>
        [DisplayName("Source Stream Id")]
        [Description("The id of the source stream.")]
        public string SourceStreamIdString => this.SourceStreamMetadata != null ? this.SourceStreamMetadata.Id.ToString() : "N/A";

        /// <summary>
        /// Gets the name of the source stream.
        /// </summary>
        [DisplayName("Source Stream Name")]
        [Description("The name of the source stream.")]
        public string SourceStreamName => this.SourceStreamMetadata != null ? this.SourceStreamMetadata.Name : "N/A";

        /// <summary>
        /// Gets the type of messages in the source stream.
        /// </summary>
        [DisplayName("Source Stream Type")]
        [Description("The type of messages in the source stream.")]
        public string SourceStreamTypeDisplayString => this.SourceStreamMetadata != null ? TypeSpec.Simplify(this.SourceStreamMetadata.TypeName) : "N/A";

        /// <summary>
        /// Gets the type of messages in the source stream.
        /// </summary>
        [Browsable(false)]
        public string SourceStreamTypeFullNameDisplayString => this.SourceStreamMetadata != null ? this.SourceStreamMetadata.TypeName : "N/A";

        /// <summary>
        /// Gets the source stream opened time.
        /// </summary>
        [Browsable(false)]
        public DateTime? SourceStreamOpenedTime => this.SourceStreamMetadata?.OpenedTime;

        /// <summary>
        /// Gets a string representation of the source stream opened time.
        /// </summary>
        [DisplayName("Source Stream OpenedTime")]
        [Description("The opened time for the source stream.")]
        public string SourceStreamOpenedTimeDisplayString
            => this.SourceStreamOpenedTime != null ? DateTimeHelper.FormatDateTime(this.SourceStreamOpenedTime) : "N/A";

        /// <summary>
        /// Gets the source stream closed time.
        /// </summary>
        [Browsable(false)]
        public DateTime? SourceStreamClosedTime => this.SourceStreamMetadata?.ClosedTime;

        /// <summary>
        /// Gets a string representation of the source stream closed time.
        /// </summary>
        [DisplayName("Source Stream ClosedTime")]
        [Description("The closed time for the source stream.")]
        public string SourceStreamClosedTimeDisplayString
            => this.SourceStreamClosedTime != null ? DateTimeHelper.FormatDateTime(this.SourceStreamClosedTime) : "N/A";

        /// <summary>
        /// Gets the originating time of the first message in the source stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? SourceStreamFirstMessageOriginatingTime
            => this.SourceStreamMetadata != null && this.SourceStreamMetadata.FirstMessageOriginatingTime != DateTime.MinValue ?
            this.SourceStreamMetadata.FirstMessageOriginatingTime : null;

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the source stream.
        /// </summary>
        [DisplayName("Source Stream First Message OriginatingTime")]
        [Description("The originating time of the first message in the stream.")]
        public string SourceStreamFirstMessageOriginatingTimeDisplayString
            => this.SourceStreamFirstMessageOriginatingTime != null ? DateTimeHelper.FormatDateTime(this.SourceStreamFirstMessageOriginatingTime) : "N/A";

        /// <summary>
        /// Gets the creation time of the first message in the source stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? SourceStreamFirstMessageCreationTime
            => this.SourceStreamMetadata != null && this.SourceStreamMetadata.FirstMessageCreationTime != DateTime.MinValue ?
            this.SourceStreamMetadata.FirstMessageCreationTime : null;

        /// <summary>
        /// Gets a string representation of the time of the first message in the source stream.
        /// </summary>
        [DisplayName("Source Stream First Message CreationTime")]
        [Description("The creation time of the first message in the stream.")]
        public string SourceStreamFirstMessageCreationTimeDisplayString
            => this.SourceStreamFirstMessageCreationTime != null ? DateTimeHelper.FormatDateTime(this.SourceStreamFirstMessageCreationTime) : "N/A";

        /// <summary>
        /// Gets the originating time of the last message in the source stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? SourceStreamLastMessageOriginatingTime
            => this.SourceStreamMetadata != null && this.SourceStreamMetadata.LastMessageOriginatingTime != DateTime.MaxValue ?
            this.SourceStreamMetadata.LastMessageOriginatingTime : null;

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the source stream.
        /// </summary>
        [DisplayName("Source Stream Last Message OriginatingTime")]
        [Description("The originating time of the last message in the stream.")]
        public string SourceStreamLastMessageOriginatingTimeDisplayString
            => this.SourceStreamLastMessageOriginatingTime != null ? DateTimeHelper.FormatDateTime(this.SourceStreamLastMessageOriginatingTime) : "N/A";

        /// <summary>
        /// Gets the creation time of the last message in the source stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? SourceStreamLastMessageCreationTime
            => this.SourceStreamMetadata != null && this.SourceStreamMetadata.LastMessageCreationTime != DateTime.MaxValue ?
            this.SourceStreamMetadata?.LastMessageCreationTime : null;

        /// <summary>
        /// Gets a string representation of the time of the last message in the source stream.
        /// </summary>
        [DisplayName("Source Stream Last Message CreationTime")]
        [Description("The creation time of the last message in the stream.")]
        public string SourceStreamLastMessageCreationTimeDisplayString
            => this.SourceStreamLastMessageCreationTime != null ? DateTimeHelper.FormatDateTime(this.SourceStreamLastMessageCreationTime) : "N/A";

        /// <summary>
        /// Gets the number of messages in the source stream.
        /// </summary>
        [Browsable(false)]
        public long? SourceStreamMessageCount => this.SourceStreamMetadata?.MessageCount;

        /// <summary>
        /// Gets a string representation of the number of messages in the source stream.
        /// </summary>
        [DisplayName("Source Stream Message Count")]
        [Description("The total number of messages in the stream.")]
        public string SourceStreamMessageCountDisplayString =>
            this.SourceStreamMessageCount != null ? this.SourceStreamMessageCount.ToString() : "N/A";

        /// <summary>
        /// Gets the total size of the messages in the source stream.
        /// </summary>
        [Browsable(false)]
        public long? SourceStreamSize
        {
            get
            {
                if (this.SourceStreamMetadata is PsiStreamMetadata psiStreamMetadata)
                {
                    return psiStreamMetadata.MessageSizeCumulativeSum;
                }
                else if (this.SourceStreamMetadata != null)
                {
                    return (long)(this.SourceStreamMetadata.AverageMessageSize * this.SourceStreamMetadata.MessageCount);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a string representation of the total size of the messages in the source stream.
        /// </summary>
        [DisplayName("Source Stream Size")]
        [Description("The size (in bytes) of data in the stream.")]
        public string SourceStreamSizeDisplayString =>
            this.SourceStreamSize != null ? this.SourceStreamSize.ToString() : "N/A";

        /// <summary>
        /// Gets the average message size in the source stream.
        /// </summary>
        [Browsable(false)]
        public double? SourceStreamAverageMessageSize
        {
            get
            {
                if (this.SourceStreamMetadata is PsiStreamMetadata psiStreamMetadata)
                {
                    return this.SourceStreamMessageCount > 0 ? psiStreamMetadata.MessageSizeCumulativeSum / this.SourceStreamMessageCount : null;
                }
                else if (this.SourceStreamMetadata != null)
                {
                    return this.SourceStreamMessageCount > 0 ? this.SourceStreamMetadata.AverageMessageSize : null;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a string representation of the average message size in the source stream.
        /// </summary>
        [DisplayName("Source Stream Avg. Message Size")]
        [Description("The average size (in bytes) of messages in the source stream.")]
        public string SourceStreamAverageMessageSizeDisplayString
            => this.SourceStreamAverageMessageSize != null ? this.SourceStreamAverageMessageSize.ToString() : "N/A";

        /// <summary>
        /// Gets the average latency of messages in the source stream.
        /// </summary>
        [Browsable(false)]
        public double? SourceStreamAverageMessageLatencyMs
            => (this.SourceStreamMetadata != null && this.SourceStreamMessageCount > 0) ? this.SourceStreamMetadata.AverageMessageLatencyMs : null;

        /// <summary>
        /// Gets a string representation of the average latency of messages in the source stream.
        /// </summary>
        [DisplayName("Source Stream Avg. Message Latency (ms)")]
        [Description("The average latency (in milliseconds) of messages in the source stream.")]
        public string SourceStreamAverageMessageLatencyMsDisplayString
            => this.SourceStreamAverageMessageLatencyMs != null ? this.SourceStreamAverageMessageLatencyMs.ToString() : "N/A";

        /// <summary>
        /// Gets the opened time for the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public DateTime? SubsumedOpenedTime
            => DateTimeHelper.MinDateTime(
                this.SourceStreamOpenedTime,
                DateTimeHelper.MinDateTime(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedOpenedTime)));

        /// <summary>
        /// Gets a string representation of the first opened time for the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed OpenedTime")]
        [Description("The first opened time for the stream(s) subsumed by the node.")]
        public string SubsumedOpenedTimeDisplayString
            => this.SubsumedOpenedTime != null ? DateTimeHelper.FormatDateTime(this.SubsumedOpenedTime) : "N/A";

        /// <summary>
        /// Gets the last closed time for the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public DateTime? SubsumedClosedTime
            => DateTimeHelper.MaxDateTime(
                this.SourceStreamClosedTime,
                DateTimeHelper.MaxDateTime(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedClosedTime)));

        /// <summary>
        /// Gets a string representation of the last closed time for the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed ClosedTime")]
        [Description("The last closed time for the stream(s) subsumed by the node.")]
        public string SubsumedClosedTimeDisplayString
            => this.SubsumedClosedTime != null ? DateTimeHelper.FormatDateTime(this.SubsumedClosedTime) : "N/A";

        /// <summary>
        /// Gets the originating time of the first message in the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public DateTime? SubsumedFirstMessageOriginatingTime
            => DateTimeHelper.MinDateTime(
                this.SourceStreamFirstMessageOriginatingTime,
                DateTimeHelper.MinDateTime(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedFirstMessageOriginatingTime)));

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed First Message OriginatingTime")]
        [Description("The originating time of the first message in the stream(s) subsumed by the node.")]
        public string SubsumedFirstMessageOriginatingTimeDisplayString
            => this.SubsumedFirstMessageOriginatingTime != null ? DateTimeHelper.FormatDateTime(this.SubsumedFirstMessageOriginatingTime) : "N/A";

        /// <summary>
        /// Gets the creation time of the first message in the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public DateTime? SubsumedFirstMessageCreationTime
            => DateTimeHelper.MinDateTime(
                this.SourceStreamFirstMessageCreationTime,
                DateTimeHelper.MinDateTime(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedFirstMessageCreationTime)));

        /// <summary>
        /// Gets a string representation of the time of the first message in the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed First Message CreationTime")]
        [Description("The creation time of the first message in the stream(s) subsumed by the node.")]
        public string SubsumedFirstMessageCreationTimeDisplayString
            => this.SubsumedFirstMessageCreationTime != null ? DateTimeHelper.FormatDateTime(this.SubsumedFirstMessageCreationTime) : "N/A";

        /// <summary>
        /// Gets the originating time of the last message in the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public DateTime? SubsumedLastMessageOriginatingTime
            => DateTimeHelper.MaxDateTime(
                this.SourceStreamLastMessageOriginatingTime,
                DateTimeHelper.MaxDateTime(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedLastMessageOriginatingTime)));

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed Last Message OriginatingTime")]
        [Description("The originating time of the last message in the stream(s) subsumed by the node.")]
        public string SubsumedLastMessageOriginatingTimeDisplayString
            => this.SubsumedLastMessageOriginatingTime != null ? DateTimeHelper.FormatDateTime(this.SubsumedLastMessageOriginatingTime) : "N/A";

        /// <summary>
        /// Gets the creation time of the last message in the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public DateTime? SubsumedLastMessageCreationTime
            => DateTimeHelper.MaxDateTime(
                this.SourceStreamLastMessageCreationTime,
                DateTimeHelper.MaxDateTime(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedLastMessageCreationTime)));

        /// <summary>
        /// Gets a string representation of the time of the last message in the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed Last Message CreationTime")]
        [Description("The creation time of the last message in the stream(s) subsumed by the node.")]
        public string SubsumedLastMessageCreationTimeDisplayString
            => this.SubsumedLastMessageCreationTime != null ? DateTimeHelper.FormatDateTime(this.SubsumedLastMessageCreationTime) : "N/A";

        /// <summary>
        /// Gets the total number of messages in the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public long? SubsumedMessageCount =>
            SizeHelper.NullableSum(
                this.SourceStreamMessageCount,
                SizeHelper.NullableSum(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedMessageCount)));

        /// <summary>
        /// Gets a string representation of the total number of messages in the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed Message Count")]
        [Description("The total number of messages in the stream(s) subsumed by the node.")]
        public string SubsumedMessageCountDisplayString
            => this.SubsumedMessageCount != null ? this.SubsumedMessageCount.ToString() : "N/A";

        /// <summary>
        /// Gets the average message latency for the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public double? SubsumedAverageMessageLatencyMs
            => this.HasSourceStreamDescendants ?
                (this.SubsumedMessageCount != null && this.SubsumedMessageCount > 0) ?
                    ((this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Sum(c => c.SubsumedMessageCount == null || c.SubsumedMessageCount == 0 ? 0 : (c.SubsumedAverageMessageLatencyMs * c.SubsumedMessageCount)) +
                    (this.SourceStreamMessageCount == null || this.SourceStreamMessageCount == 0 ? 0 : (this.SourceStreamAverageMessageLatencyMs * this.SourceStreamMessageCount))) /
                    this.SubsumedMessageCount)
                    : default
                : this.SourceStreamAverageMessageLatencyMs;

        /// <summary>
        /// Gets a string representation of the average message latency for the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed Avg. Message Latency (ms)")]
        [Description("The average latency (in milliseconds) of messages in the stream(s) subsumed by the node.")]
        public string SubsumedAverageMessageLatencyMsDisplayString
            => this.SubsumedAverageMessageLatencyMs != null ? this.SubsumedAverageMessageLatencyMs.ToString() : "N/A";

        /// <summary>
        /// Gets the average message size for the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public double? SubsumedAverageMessageSize
            => this.HasSourceStreamDescendants ?
                (this.SubsumedMessageCount != null && this.SubsumedMessageCount > 0) ?
                    (this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Sum(c => c.SubsumedMessageCount == null || c.SubsumedMessageCount == 0 ? 0 : (c.SubsumedAverageMessageSize * c.SubsumedMessageCount)) +
                     (this.SourceStreamMessageCount == null || this.SourceStreamMessageCount == 0 ? 0 : (this.SourceStreamAverageMessageSize * this.SourceStreamMessageCount))) /
                    this.SubsumedMessageCount
                    : default
                : this.SourceStreamAverageMessageSize;

        /// <summary>
        /// Gets a string representation of the average message size for the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed Avg. Message Size")]
        [Description("The average size (in bytes) of messages in the stream(s) subsumed by the node.")]
        public string SubsumedAverageMessageSizeDisplayString
            => this.SubsumedAverageMessageSize != null ? this.SubsumedAverageMessageSize.ToString() : "N/A";

        /// <summary>
        /// Gets the total data size in the stream(s) subsumed by the node.
        /// </summary>
        [Browsable(false)]
        public long? SubsumedSize
            => SizeHelper.NullableSum(
                this.SourceStreamSize,
                SizeHelper.NullableSum(this.Children.Where(c => c.IsSourceStream || c.HasSourceStreamDescendants).Select(c => c.SubsumedSize)));

        /// <summary>
        /// Gets a string representation of the total data size in the stream(s) subsumed by the node.
        /// </summary>
        [DisplayName("Subsumed Size")]
        [Description("The size (in bytes) of data in the stream(s) subsumed by the node.")]
        public string SubsumedSizeDisplayString
            => this.SubsumedSize != null ? this.SubsumedSize.ToString() : "N/A";

        /// <summary>
        /// Gets or sets a value indicating whether the stream has unsaved changes.
        /// </summary>
        [Browsable(false)]
        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.RaisePropertyChanging(nameof(this.DisplayString));
                this.isDirty = value;
                this.RaisePropertyChanged(nameof(this.DisplayString));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the stream's supplemental metadata (if any) is known, readable type.
        /// </summary>
        [Browsable(false)]
        public bool SupplementalMetadataTypeIsKnown
        {
            get
            {
                // If there is no supplemental metadata type for the stream
                if (string.IsNullOrWhiteSpace(this.SourceStreamMetadata?.SupplementalMetadataTypeName))
                {
                    // Then return false
                    return false;
                }

                // If we've not tried to read the supplemental metadata yet, do so now.
                if (!this.supplementalMetadataTypeIsKnown.HasValue)
                {
                    try
                    {
                        // Attempt to read the supplemental metadata for the stream.
                        MethodInfo methodInfo = DataManager.Instance.GetType().GetMethod(
                            nameof(DataManager.GetSupplementalMetadataByName),
                            new Type[] { typeof(string), typeof(string), typeof(Type), typeof(string) });

                        MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(TypeResolutionHelper.GetVerifiedType(this.SourceStreamMetadata.SupplementalMetadataTypeName));

                        var parameters = new object[]
                            {
                                this.PartitionViewModel.StoreName,
                                this.PartitionViewModel.StorePath,
                                TypeResolutionHelper.GetVerifiedType(this.PartitionViewModel.Partition.StreamReaderTypeName),
                                this.SourceStreamName,
                            };
                        genericMethodInfo.Invoke(DataManager.Instance, parameters);

                        // Success
                        this.supplementalMetadataTypeIsKnown = true;
                    }
                    catch (Exception)
                    {
                        this.supplementalMetadataTypeIsKnown = false;
                    }
                }

                return this.supplementalMetadataTypeIsKnown.Value;
            }
        }

        /// <summary>
        /// Gets the command that executes when opening the stream tree node context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<Grid> ContextMenuOpeningCommand
            => this.contextMenuOpeningCommand ??= new RelayCommand<Grid>(
                grid =>
                {
                    var contextMenu = new ContextMenu();
                    this.PopulateContextMenu(contextMenu);
                    grid.ContextMenu = contextMenu;
                });

        /// <summary>
        /// Gets the command that executes when double clicking on the stream tree node.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseDoubleClickCommand
            => this.mouseDoubleClickCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.OnMouseDoubleClick(e));

        /// <summary>
        /// Creates the root stream tree node for a specified partition.
        /// </summary>
        /// <param name="partitionViewModel">The partition view model.</param>
        /// <returns>A root stream tree node for the specified partition.</returns>
        public static StreamTreeNode CreateRoot(PartitionViewModel partitionViewModel)
            => new (partitionViewModel, null, null, null, null);

        /// <summary>
        /// Creates a stream binding for this stream tree node and a specified visualizer.
        /// </summary>
        /// <param name="visualizerMetadata">The visualizer to create a stream binding for.</param>
        /// <returns>A corresponding stream binding.</returns>
        public StreamBinding CreateStreamBinding(VisualizerMetadata visualizerMetadata)
            => new (
                this.SourceStreamMetadata.Name,
                this.PartitionViewModel.Name,
                this.FullName,
                this.DerivedStreamAdapterType,
                this.DerivedStreamAdapterArguments,
                visualizerMetadata.StreamAdapterType,
                visualizerMetadata.StreamAdapterArguments,
                visualizerMetadata.SummarizerType,
                visualizerMetadata.SummarizerArguments);

        /// <summary>
        /// Selects the list of visualizers compatible with this stream tree node.
        /// </summary>
        /// <param name="visualizationPanel">The visualization panel where it is intended to visualize the data, or visualizers targeting any panels should be returned.</param>
        /// <param name="isUniversal">A nullable boolean indicating constraints on whether the visualizer should be a universal one (visualize messages, visualize latency etc).</param>
        /// <param name="isInNewPanel">A nullable boolean indicating constraints on whether the visualizer should be a "in new panel" one.</param>
        /// <returns>The matching list of visualizers.</returns>
        public List<VisualizerMetadata> GetCompatibleVisualizers(
            VisualizationPanel visualizationPanel = null,
            bool? isUniversal = null,
            bool? isInNewPanel = null)
        {
            var results = new List<VisualizerMetadata>();
            var comparer = new VisualizerMetadataComparer(this.DataType);

            // If we're looking for visualizers that fit in any panel
            if (visualizationPanel == null)
            {
                results.AddRange(VisualizationContext.Instance.PluginMap.Visualizers.Where(v =>
                    (this.DataType == v.DataType ||
                        this.DataType.IsSubclassOf(v.DataType) ||
                        (v.DataType.IsInterface && v.DataType.IsAssignableFrom(this.DataType))) &&
                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value) &&
                    (!isUniversal.HasValue || v.IsUniversalVisualizer == isUniversal)));
            }
            else
            {
                // o/w find out the compatible panel types
                results.AddRange(VisualizationContext.Instance.PluginMap.Visualizers.Where(v =>
                    visualizationPanel.CompatiblePanelTypes.Contains(v.VisualizationPanelType) &&
                    (this.DataType == v.DataType ||
                        this.DataType.IsSubclassOf(v.DataType) ||
                        (v.DataType.IsInterface && v.DataType.IsAssignableFrom(this.DataType))) &&
                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value) &&
                    (!isUniversal.HasValue || v.IsUniversalVisualizer == isUniversal)));
            }

            // Special-case: for streams of type Dictionary<TKey, numeric>, create the corresponding
            // numeric series visualizer, by using a dictionary-key-to-string adapter.
            if ((!isUniversal.HasValue || !isUniversal.Value) &&
                (visualizationPanel == null || visualizationPanel.CompatiblePanelTypes.Contains(VisualizationPanelType.Timeline)))
            {
                if (this.DataType.IsGenericType && this.DataType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var genericArguments = this.DataType.GetGenericArguments();
                    var numericSeriesVisualizationObjectType = NumericSeriesVisualizationObject.GetSeriesVisualizationObjectTypeByNumericType(genericArguments[1]);

                    if (numericSeriesVisualizationObjectType != null)
                    {
                        if (visualizationPanel == null)
                        {
                            var metadata = VisualizationContext.Instance.PluginMap.Visualizers
                                .FirstOrDefault(v =>
                                    (v.VisualizationObjectType == numericSeriesVisualizationObjectType) &&
                                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value))
                                .GetCloneWithNewStreamAdapterType(typeof(DictionaryKeyToStringAdapter<,>).MakeGenericType(genericArguments));
                            results.Add(metadata);
                        }
                        else
                        {
                            var metadata = VisualizationContext.Instance.PluginMap.Visualizers
                                .FirstOrDefault(v =>
                                    visualizationPanel.CompatiblePanelTypes.Contains(v.VisualizationPanelType) &&
                                    (v.VisualizationObjectType == numericSeriesVisualizationObjectType) &&
                                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value))
                                .GetCloneWithNewStreamAdapterType(typeof(DictionaryKeyToStringAdapter<,>).MakeGenericType(genericArguments));
                            results.Add(metadata);
                        }
                    }
                }
            }

            // Special-case: the latency visualizer b/c it's not detectable by data type
            // (the adapter to make it work will be added automatically later in
            // CustomizeVisualizerMetadata). Latency visualizer is only compatible with
            // timeline visualization panels.
            if (isUniversal.HasValue && isUniversal.Value)
            {
                if (isInNewPanel.HasValue && isInNewPanel.Value)
                {
                    results.Add(VisualizationContext.Instance.PluginMap.Visualizers.FirstOrDefault(v => v.CommandText == ContextMenuName.VisualizeLatencyInNewPanel));
                }
                else if (visualizationPanel is TimelineVisualizationPanel)
                {
                    results.Add(VisualizationContext.Instance.PluginMap.Visualizers.FirstOrDefault(v => v.CommandText == ContextMenuName.VisualizeLatency));
                }
            }

            // Customize each visualizer metadata.
            this.InsertCustomAdapters(results);

            return results.OrderBy(v => v, comparer).ToList();
        }

        /// <summary>
        /// Adds a new stream tree node as child of this node.
        /// </summary>
        /// <param name="childName">The child name, relative to this node.</param>
        /// <param name="sourceStreamMetadata">The source stream metadata.</param>
        /// <param name="derivedStreamAdapterType">The derived stream adapter type.</param>
        /// <param name="derivedStreamAdapterArguments">The derived stream adapter arguments.</param>
        /// <param name="sorted">Whether to add the new node in sorted order.</param>
        /// <returns>A reference to the child stream tree node if successfully created, or null otherwise.</returns>
        public StreamTreeNode AddChild(
            string childName,
            IStreamMetadata sourceStreamMetadata,
            Type derivedStreamAdapterType,
            object[] derivedStreamAdapterArguments,
            bool sorted = false)
        {
            // Add the child node by recursing over the child name items
            var streamTreeNode = this.AddChild(childName.Split('.'), sourceStreamMetadata, derivedStreamAdapterType, derivedStreamAdapterArguments, sorted);

            // If we have sucessfully added a child that corresponds to a source stream
            if (this.FullName != null && streamTreeNode != null && streamTreeNode.IsSourceStream)
            {
                // Then mark to the root that we have stream descendants
                var fullNamePathItems = this.FullName.Split('.');
                for (int i = 1; i <= fullNamePathItems.Length; i++)
                {
                    var ancestorName = fullNamePathItems.Take(i).EnumerableToString(".");
                    this.FindChild(ancestorName).HasSourceStreamDescendants = true;
                }
            }

            return streamTreeNode;
        }

        /// <summary>
        /// Selects a child tree node and expands all nodes on the path to it.
        /// </summary>
        /// <param name="childName">The child name, relative to this node.</param>
        /// <returns>True if the child was found, otherwise false.</returns>
        public bool SelectChild(string childName) => this.SelectChild(childName.Split('.'));

        /// <summary>
        /// Finds a child tree node by name.
        /// </summary>
        /// <param name="childName">The child name, relative to this node.</param>
        /// <returns>The stream tree node corresponding to the child if found, or null otherwise.</returns>
        public StreamTreeNode FindChild(string childName) => this.FindChild(childName.Split('.'));

        /// <summary>
        /// Expands this node and all of its child nodes recursively.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var child in this.Children)
            {
                child.ExpandAll();
            }

            this.IsTreeNodeExpanded = true;
        }

        /// <summary>
        /// Collapses this node and all of its child nodes recursively.
        /// </summary>
        public void CollapseAll()
        {
            this.IsTreeNodeExpanded = false;

            foreach (var child in this.Children)
            {
                child.CollapseAll();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"Node: {this.Name}";

        /// <summary>
        /// Event handler for partition property changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPartitionViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.PartitionViewModel.IsLivePartition))
            {
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        /// <summary>
        /// Event handler for dataset view model property changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDatasetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
                this.RaisePropertyChanged(nameof(this.IsInCurrentSession));
            }
            else if (e.PropertyName == nameof(this.DatasetViewModel.ShowAuxiliaryStreamInfo))
            {
                this.UpdateAuxiliaryInfo();
            }
        }

        /// <summary>
        /// Handler for a double-click event on the stream tree node.
        /// </summary>
        /// <param name="e">The mouse button event arguments.</param>
        private void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (this.IsStream && this.CanAddMemberDerivedStreams())
            {
                this.AddMemberDerivedStreams(out var membersNotAdded);
                this.ExpandAll();
                this.IsTreeNodeExpanded = true;
                e.Handled = true;

                if (membersNotAdded.Any())
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        new MessageBoxWindow(
                            Application.Current.MainWindow,
                            "Warning",
                            $"The following member(s) were not added because children with the same names already exist: {membersNotAdded.EnumerableToString(", ")}.",
                            "Close",
                            null).ShowDialog();
                    }));
                }
            }
        }

        /// <summary>
        /// Updates the auxiliary info to be displayed.
        /// </summary>
        private void UpdateAuxiliaryInfo()
        {
            var indexedMarker = this.IsIndexedPsiStream ? "*" : string.Empty;
            switch (this.PartitionViewModel.SessionViewModel.DatasetViewModel.ShowAuxiliaryStreamInfo)
            {
                case AuxiliaryStreamInfo.None:
                    this.AuxiliaryInfo = string.Empty;
                    break;
                case AuxiliaryStreamInfo.Size:
                    this.AuxiliaryInfo = this.HasSourceStreamDescendants && this.SubsumedSize != null ? $"[{SizeHelper.FormatSize(this.SubsumedSize.Value)}] " : string.Empty;
                    if (this.IsStream)
                    {
                        this.AuxiliaryInfo += " " + indexedMarker + SizeHelper.FormatSize(this.SourceStreamSize.Value);
                    }

                    break;
                case AuxiliaryStreamInfo.DataThroughputPerHour:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedClosedTime != null && this.SubsumedOpenedTime != null)
                    {
                        var subsumedThroughput = this.SubsumedSize != null ? (this.SubsumedSize.Value / (this.SubsumedClosedTime.Value - this.SubsumedOpenedTime.Value).TotalHours) : 0;
                        this.AuxiliaryInfo += $"[{SizeHelper.FormatThroughput(subsumedThroughput, "hour")}]";
                    }

                    if (this.IsStream)
                    {
                        var throughput = this.SourceStreamSize != null ? (this.SourceStreamSize.Value / (this.SourceStreamClosedTime.Value - this.SourceStreamOpenedTime.Value).TotalHours) : 0;
                        this.AuxiliaryInfo += " " + indexedMarker + SizeHelper.FormatThroughput(throughput, "hour");
                    }

                    break;
                case AuxiliaryStreamInfo.DataThroughputPerMinute:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedClosedTime != null && this.SubsumedOpenedTime != null)
                    {
                        var subsumedThroughput = this.SubsumedSize != null ? (this.SubsumedSize.Value / (this.SubsumedClosedTime.Value - this.SubsumedOpenedTime.Value).TotalMinutes) : 0;
                        this.AuxiliaryInfo += $"[{SizeHelper.FormatThroughput(subsumedThroughput, "min")}]";
                    }

                    if (this.IsStream)
                    {
                        var throughput = this.SourceStreamSize != null ? (this.SourceStreamSize.Value / (this.SourceStreamClosedTime.Value - this.SourceStreamOpenedTime.Value).TotalMinutes) : 0;
                        this.AuxiliaryInfo += " " + indexedMarker + SizeHelper.FormatThroughput(throughput, "min");
                    }

                    break;
                case AuxiliaryStreamInfo.DataThroughputPerSecond:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedClosedTime != null && this.SubsumedOpenedTime != null)
                    {
                        var subsumedThroughput = this.SubsumedSize != null ? (this.SubsumedSize.Value / (this.SubsumedClosedTime.Value - this.SubsumedOpenedTime.Value).TotalSeconds) : 0;
                        this.AuxiliaryInfo += $"[{SizeHelper.FormatThroughput(subsumedThroughput, "sec")}]";
                    }

                    if (this.IsStream)
                    {
                        var throughput = this.SourceStreamSize != null ? (this.SourceStreamSize.Value / (this.SourceStreamClosedTime.Value - this.SourceStreamOpenedTime.Value).TotalSeconds) : 0;
                        this.AuxiliaryInfo += " " + indexedMarker + SizeHelper.FormatThroughput(throughput, "sec");
                    }

                    break;
                case AuxiliaryStreamInfo.MessageCountThroughputPerHour:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedClosedTime != null && this.SubsumedOpenedTime != null)
                    {
                        var subsumedThroughput = this.SubsumedMessageCount != null ? (this.SubsumedMessageCount.Value / (this.SubsumedClosedTime.Value - this.SubsumedOpenedTime.Value).TotalHours) : 0;
                        this.AuxiliaryInfo += $"[{subsumedThroughput:0.01}]";
                    }

                    if (this.IsStream)
                    {
                        var throughput = this.SourceStreamMessageCount != null ? (this.SourceStreamMessageCount.Value / (this.SourceStreamClosedTime.Value - this.SourceStreamOpenedTime.Value).TotalHours) : 0;
                        this.AuxiliaryInfo += " " + indexedMarker + $"{throughput:0.01}";
                    }

                    break;
                case AuxiliaryStreamInfo.MessageCountThroughputPerMinute:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedClosedTime != null && this.SubsumedOpenedTime != null)
                    {
                        var subsumedThroughput = this.SubsumedMessageCount != null ? (this.SubsumedMessageCount.Value / (this.SubsumedClosedTime.Value - this.SubsumedOpenedTime.Value).TotalMinutes) : 0;
                        this.AuxiliaryInfo += $"[{subsumedThroughput:0.01}]";
                    }

                    if (this.IsStream)
                    {
                        var throughput = this.SourceStreamMessageCount != null ? (this.SourceStreamMessageCount.Value / (this.SourceStreamClosedTime.Value - this.SourceStreamOpenedTime.Value).TotalMinutes) : 0;
                        this.AuxiliaryInfo += " " + indexedMarker + $"{throughput:0.01}";
                    }

                    break;
                case AuxiliaryStreamInfo.MessageCountThroughputPerSecond:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedClosedTime != null && this.SubsumedOpenedTime != null)
                    {
                        var subsumedThroughput = this.SubsumedMessageCount != null ? (this.SubsumedMessageCount.Value / (this.SubsumedClosedTime.Value - this.SubsumedOpenedTime.Value).TotalSeconds) : 0;
                        this.AuxiliaryInfo += $"[{subsumedThroughput:0.01}]";
                    }

                    if (this.IsStream)
                    {
                        var throughput = this.SourceStreamMessageCount != null ? (this.SourceStreamMessageCount.Value / (this.SourceStreamClosedTime.Value - this.SourceStreamOpenedTime.Value).TotalSeconds) : 0;
                        this.AuxiliaryInfo += " " + indexedMarker + $"{throughput:0.01}";
                    }

                    break;
                case AuxiliaryStreamInfo.MessageCount:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedMessageCount != null)
                    {
                        this.AuxiliaryInfo += $"[{this.SubsumedMessageCount:0,0}]";
                    }

                    if (this.IsStream)
                    {
                        this.AuxiliaryInfo += $" {this.SourceStreamMessageCount:0,0}";
                    }

                    break;
                case AuxiliaryStreamInfo.AverageMessageLatencyMs:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedAverageMessageLatencyMs != null)
                    {
                        this.AuxiliaryInfo += $"[{SizeHelper.FormatLatencyMs(this.SubsumedAverageMessageLatencyMs.Value)}]";
                    }

                    if (this.IsStream && this.SourceStreamAverageMessageLatencyMs != null)
                    {
                        this.AuxiliaryInfo += SizeHelper.FormatLatencyMs(this.SourceStreamAverageMessageLatencyMs.Value);
                    }

                    break;

                case AuxiliaryStreamInfo.AverageMessageSize:
                    this.AuxiliaryInfo = string.Empty;

                    if (this.HasSourceStreamDescendants && this.SubsumedAverageMessageSize != null)
                    {
                        this.AuxiliaryInfo += $"[{SizeHelper.FormatSize(this.SubsumedAverageMessageSize.Value)}]";
                    }

                    if (this.IsStream && this.SourceStreamAverageMessageSize != null)
                    {
                        this.AuxiliaryInfo += indexedMarker + SizeHelper.FormatSize(this.SourceStreamAverageMessageSize.Value);
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Populates a context menu with items for this node.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenu(ContextMenu contextMenu)
        {
            if (this.IsStream)
            {
                this.PopulateContextMenuWithVisualizers(contextMenu);
                this.PopulateContextMenuWithModifyScript(contextMenu);
                this.PopulateContextMenuWithDerivedStreamExpansions(contextMenu);
            }

            this.PopulateContextMenuWithCopyToClipboard(contextMenu);

            if (this.IsStream)
            {
                this.PopulateContextMenuWithZoomToStream(contextMenu);
            }

            // Add the visualize session context menu if the stream is not in the currently visualized session
            if (!this.IsInCurrentSession)
            {
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(string.Empty, ContextMenuName.VisualizeSession, this.SessionViewModel.VisualizeSessionCommand));
            }

            this.PopulateContextMenuWithExpandAndCollapseAll(contextMenu);
            this.PopulateContextMenuWithShowStreamInfo(contextMenu);
        }

        /// <summary>
        /// Populates a specified context menu with visualizers.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithVisualizers(ContextMenu contextMenu)
        {
            var currentPanel = VisualizationContext.Instance.VisualizationContainer.CurrentPanel;

            // sectionStart and sectionEnd keep track of how many items were added in each
            // section of the menu, and can be used to reason about when to add a separator
            var previousIconPath = default(string);

            // get the type-specific visualizers that work in the current panel
            if (currentPanel != null)
            {
                var specificInCurrentPanel = this.GetCompatibleVisualizers(visualizationPanel: currentPanel, isUniversal: false, isInNewPanel: false);
                if (specificInCurrentPanel.Any() && contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                foreach (var visualizer in specificInCurrentPanel)
                {
                    // Create and add a menu item. Currently show icon only if different from previous
                    contextMenu.Items.Add(this.CreateVisualizeStreamMenuItem(visualizer, showIcon: visualizer.IconSourcePath != previousIconPath));
                    previousIconPath = visualizer.IconSourcePath;
                }
            }

            // get the type specific visualizers in a new panel
            previousIconPath = default;
            var specificInNewPanel = this.GetCompatibleVisualizers(visualizationPanel: null, isUniversal: false, isInNewPanel: true);

            // add a separator if necessary
            if (specificInNewPanel.Any() && contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            foreach (var visualizer in specificInNewPanel)
            {
                // Create and add a menu item. Currently show icon only for the first item in the section.
                contextMenu.Items.Add(this.CreateVisualizeStreamMenuItem(visualizer, showIcon: visualizer.IconSourcePath != previousIconPath));
                previousIconPath = visualizer.IconSourcePath;
            }

            if (currentPanel != null)
            {
                // get the universal visualizers that work in the current panel
                var universalInCurrentPanel = this.GetCompatibleVisualizers(visualizationPanel: currentPanel, isUniversal: true, isInNewPanel: false);

                // add a separator if necessary
                if (universalInCurrentPanel.Any() && contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                foreach (var visualizer in universalInCurrentPanel)
                {
                    // Create and add a menu item.
                    contextMenu.Items.Add(this.CreateVisualizeStreamMenuItem(visualizer, showIcon: true));
                }
            }

            // get the universal visualizer in new panel
            var universalInNewPanel = this.GetCompatibleVisualizers(visualizationPanel: null, isUniversal: true, isInNewPanel: true);

            // add a separator if necessary
            if (universalInNewPanel.Any() && contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            foreach (var visualizer in universalInNewPanel)
            {
                // Create and add a menu item.
                contextMenu.Items.Add(this.CreateVisualizeStreamMenuItem(visualizer, showIcon: true));
            }
        }

        /// <summary>
        /// Populates a specified context menu with stream expansion commands.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithDerivedStreamExpansions(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            this.PopulateContextMenuWithAddMemberDerivedStreams(contextMenu);
            this.PopulateContextMenuWithAddDictionaryKeyDerivedStreams(contextMenu);
            this.PopulateContextMenuWithAddScriptDerivedStream(contextMenu);
        }

        private void PopulateContextMenuWithCopyToClipboard(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            // Add copy to clipboard menu with sub-menu items
            var copyToClipboardMenuItem = MenuItemHelper.CreateMenuItem(
                string.Empty,
                "Copy to Clipboard",
                null);

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Session Name",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.SessionViewModel.Name));

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Partition Name",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.PartitionViewModel.Name));

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Partition Store Name",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.PartitionViewModel.StoreName));

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Partition Store Path",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.PartitionViewModel.StorePath));

            if (this.IsStream)
            {
                copyToClipboardMenuItem.Items.Add(new Separator());

                copyToClipboardMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Stream Name",
                        VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                        null,
                        true,
                        this.FullName));

                copyToClipboardMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Source Stream Name",
                        VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                        null,
                        true,
                        this.SourceStreamName));

                copyToClipboardMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Source Stream Type",
                        VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                        null,
                        true,
                        this.SourceStreamTypeDisplayString));

                copyToClipboardMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Source Stream Type (Assembly Qualified)",
                        VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                        null,
                        true,
                        this.SourceStreamTypeFullNameDisplayString));
            }

            contextMenu.Items.Add(copyToClipboardMenuItem);
        }

        /// <summary>
        /// Populates a specified context menu with the Add Members Derived Streams command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithAddMemberDerivedStreams(ContextMenu contextMenu)
        {
            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.DerivedStream,
                    ContextMenuName.AddMemberDerivedStreams,
                    new VisualizationCommand(() =>
                    {
                        this.AddMemberDerivedStreams(out var membersNotAdded);
                        this.ExpandAll();

                        if (membersNotAdded.Any())
                        {
                            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                            {
                                new MessageBoxWindow(
                                    Application.Current.MainWindow,
                                    "Warning",
                                    $"The following member(s) were not added because children with the same names already exist: {membersNotAdded.EnumerableToString(", ")}.",
                                    "Close",
                                    null).ShowDialog();
                            }));
                        }
                    }),
                    isEnabled: this.CanAddMemberDerivedStreams()));
        }

        /// <summary>
        /// Populates a specified context menu with expand dictionary command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithAddDictionaryKeyDerivedStreams(ContextMenu contextMenu)
        {
            // Only add dictionary expansion menu item if the stream is a dictionary
            if (this.CanAddDictionaryKeyDerivedStreams())
            {
                var typeParams = this.DataType.GetGenericArguments();
                var addDictionaryKeyDerivedStreams = typeof(StreamTreeNode).GetMethod(nameof(this.AddDictionaryKeyDerivedStreams), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(typeParams);
                contextMenu.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        IconSourcePath.DerivedStream,
                        ContextMenuName.AddDictionaryKeyDerivedStreams,
                        new VisualizationCommand(() =>
                        {
                            var parameters = new object[] { null };
                            addDictionaryKeyDerivedStreams.Invoke(this, parameters);
                            var keysNotAdded = (List<string>)parameters[0];
                            this.ExpandAll();

                            if (keysNotAdded.Any())
                            {
                                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    new MessageBoxWindow(
                                        Application.Current.MainWindow,
                                        "Warning",
                                        $"The following keys(s) were not added because children with the same names already exist: {keysNotAdded.EnumerableToString(", ")}.",
                                        "Close",
                                        null).ShowDialog();
                                }));
                            }
                        })));
            }
        }

        /// <summary>
        /// Populates a specified context menu with Add Script Derived Stream command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithAddScriptDerivedStream(ContextMenu contextMenu)
        {
            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.DerivedStream,
                    ContextMenuName.AddScriptDerivedStream,
                    new VisualizationCommand(() =>
                    {
                        if (this.AddScriptDerivedStream(out string errorString))
                        {
                            this.ExpandAll();
                        }

                        if (!string.IsNullOrEmpty(errorString))
                        {
                            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                            {
                                new MessageBoxWindow(
                                    Application.Current.MainWindow,
                                    "Error",
                                    errorString,
                                    "Close",
                                    null).ShowDialog();
                            }));
                        }
                    })));
        }

        /// <summary>
        /// Populates a specified context menu with edit script command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithModifyScript(ContextMenu contextMenu)
        {
            if (this.IsScriptDerivedStream)
            {
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                contextMenu.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        IconSourcePath.DerivedStream,
                        ContextMenuName.ModifyScript,
                        new VisualizationCommand(() =>
                        {
                            this.ModifyDerivedStreamScript(out string errorString);

                            if (!string.IsNullOrEmpty(errorString))
                            {
                                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    new MessageBoxWindow(
                                        Application.Current.MainWindow,
                                        "Error",
                                        errorString,
                                        "Close",
                                        null).ShowDialog();
                                }));
                            }
                        })));
            }
        }

        /// <summary>
        /// Populates a specified context menu with zoom to stream command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithZoomToStream(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.ZoomToStream,
                    ContextMenuName.ZoomToStreamExtents,
                    new VisualizationCommand<StreamTreeNode>(stn => VisualizationContext.Instance.ZoomToStreamExtents(stn)),
                    commandParameter: this));
        }

        /// <summary>
        /// Populates a context menu with the expand and collapse all items.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithExpandAndCollapseAll(ContextMenu contextMenu)
        {
            if (!this.internalChildren.Any())
            {
                return;
            }

            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.ExpandAllNodes,
                    ContextMenuName.ExpandAllNodes,
                    new VisualizationCommand(() => this.ExpandAll()),
                    isEnabled: this.internalChildren.Any()));

            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.CollapseAllNodes,
                    ContextMenuName.CollapseAllNodes,
                    new VisualizationCommand(() => this.CollapseAll()),
                    isEnabled: this.internalChildren.Any()));
        }

        /// <summary>
        /// Populates a context menu with the commands for showing stream info.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        private void PopulateContextMenuWithShowStreamInfo(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            // Add run batch processing task menu
            var showStreamInfoMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Show Streams Info", null);
            foreach (var auxiliaryStreamInfo in Enum.GetValues(typeof(AuxiliaryStreamInfo)))
            {
                var auxiliaryStreamInfoValue = (AuxiliaryStreamInfo)auxiliaryStreamInfo;
                var auxiliaryStreamInfoName = auxiliaryStreamInfoValue switch
                {
                    AuxiliaryStreamInfo.None => "None",
                    AuxiliaryStreamInfo.MessageCount => "Message Count",
                    AuxiliaryStreamInfo.AverageMessageSize => "Average Message Size",
                    AuxiliaryStreamInfo.AverageMessageLatencyMs => "Average Message Latency",
                    AuxiliaryStreamInfo.Size => "Size",
                    AuxiliaryStreamInfo.DataThroughputPerHour => "Throughput (bytes per hour)",
                    AuxiliaryStreamInfo.DataThroughputPerMinute => "Throughput (bytes per minute)",
                    AuxiliaryStreamInfo.DataThroughputPerSecond => "Throughput (bytes per second)",
                    AuxiliaryStreamInfo.MessageCountThroughputPerHour => "Throughput (messages per hour)",
                    AuxiliaryStreamInfo.MessageCountThroughputPerMinute => "Throughput (messages per minute)",
                    AuxiliaryStreamInfo.MessageCountThroughputPerSecond => "Throughput (messages per second)",
                    _ => throw new NotImplementedException(),
                };

                showStreamInfoMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        this.DatasetViewModel.ShowAuxiliaryStreamInfo == auxiliaryStreamInfoValue ? IconSourcePath.Checkmark : null,
                        auxiliaryStreamInfoName,
                        new VisualizationCommand<AuxiliaryStreamInfo>(s => this.DatasetViewModel.ShowAuxiliaryStreamInfo = s),
                        commandParameter: auxiliaryStreamInfoValue));
            }

            contextMenu.Items.Add(showStreamInfoMenuItem);
        }

        /// <summary>
        /// Creates a menu item for visualizing the stream.
        /// </summary>
        /// <param name="metadata">The visualizer metadata.</param>
        /// <param name="showIcon">Indicates whether to show the icon.</param>
        /// <returns>The menu item.</returns>
        private MenuItem CreateVisualizeStreamMenuItem(VisualizerMetadata metadata, bool showIcon = true)
            => MenuItemHelper.CreateMenuItem(
                showIcon ? metadata.IconSourcePath : string.Empty,
                metadata.CommandText,
                new VisualizationCommand<VisualizerMetadata>(
                    m => VisualizationContext.Instance.VisualizeStream(
                        this, m, VisualizationContext.Instance.VisualizationContainer.CurrentPanel)),
                tag: metadata,
                commandParameter: metadata);

        /// <summary>
        /// Adds a new stream tree node as child of this node.
        /// </summary>
        /// <param name="childPathItems">An enumeration containing the path to the child stream tree node to be added.</param>
        /// <param name="sourceStreamMetadata">The source stream metadata.</param>
        /// <param name="derivedStreamAdapterType">The derived stream adapter type.</param>
        /// <param name="derivedStreamAdapterArguments">The derived stream adapter arguments.</param>
        /// <param name="sorted">Whether to add the new node in sorted order.</param>
        /// <returns>A reference to the child stream tree node if successfully created, or null otherwise.</returns>
        private StreamTreeNode AddChild(
            IEnumerable<string> childPathItems,
            IStreamMetadata sourceStreamMetadata,
            Type derivedStreamAdapterType,
            object[] derivedStreamAdapterArguments,
            bool sorted = false)
        {
            var firstPathItem = childPathItems.First();

            // Add a container if one does not already exists for the first path element
            var firstPathItemNode = this.internalChildren.FirstOrDefault(p => p.Name == firstPathItem);
            if (firstPathItemNode == null)
            {
                firstPathItemNode = new StreamTreeNode(this.PartitionViewModel, this.FullName == null ? firstPathItem : $"{this.FullName}.{firstPathItem}", null, null, null);
                if (sorted)
                {
                    this.internalChildren.InsertSorted(firstPathItemNode, (node1, node2) => string.Compare(node1.Name, node2.Name));
                }
                else
                {
                    this.internalChildren.Add(firstPathItemNode);
                }
            }

            // If we are at the last segment of the path name then we are at the leaf node
            if (childPathItems.Count() == 1)
            {
                // If the first path item node is a container
                if (firstPathItemNode.IsContainer)
                {
                    // Then convert the container to a stream

                    // Find the index and remove the container node
                    var index = this.internalChildren.IndexOf(firstPathItemNode);
                    this.internalChildren.Remove(firstPathItemNode);

                    var streamTreeNode = new StreamTreeNode(
                        this.PartitionViewModel,
                        firstPathItemNode.FullName,
                        sourceStreamMetadata,
                        derivedStreamAdapterType,
                        derivedStreamAdapterArguments);

                    // Add any children the previous container might have had to the stream tree node
                    foreach (var child in firstPathItemNode.Children)
                    {
                        streamTreeNode.internalChildren.Add(child);
                    }

                    // Insert the new node at the same position as the container
                    this.internalChildren.Insert(index, streamTreeNode);

                    // Determine whether this node has source stream descendants as a result of this addition
                    if (firstPathItemNode.HasSourceStreamDescendants || streamTreeNode.IsSourceStream)
                    {
                        this.HasSourceStreamDescendants = true;
                    }

                    return streamTreeNode;
                }
                else
                {
                    // O/w a stream already exists at the desired position, so we cannot effect the addition
                    return null;
                }
            }

            // We are not at the last segment so recurse in
            var childStreamTreeNode = firstPathItemNode.AddChild(childPathItems.Skip(1), sourceStreamMetadata, derivedStreamAdapterType, derivedStreamAdapterArguments, sorted);

            // If we have successfully added a child which corresponds to a real stream
            if (childStreamTreeNode != null && childStreamTreeNode.IsSourceStream)
            {
                // Then mark that we have stream descendants.
                this.HasSourceStreamDescendants = true;
            }

            return childStreamTreeNode;
        }

        /// <summary>
        /// Selects a child tree node and expands all nodes on the path to it.
        /// </summary>
        /// <param name="childPathItems">An enumeration containing the path to the child stream tree node to be added.</param>
        /// <returns>True if the child was found, otherwise false.</returns>
        private bool SelectChild(IEnumerable<string> childPathItems)
        {
            var firstPathItem = childPathItems.First();
            var firstPathItemNode = this.internalChildren.FirstOrDefault(p => p.Name == firstPathItem);
            if (firstPathItemNode == default)
            {
                return false;
            }

            if (childPathItems.Count() == 1)
            {
                firstPathItemNode.IsTreeNodeSelected = true;
                this.IsTreeNodeExpanded = true;
                return true;
            }
            else
            {
                bool result = firstPathItemNode.SelectChild(childPathItems.Skip(1));
                if (result)
                {
                    this.IsTreeNodeExpanded = true;
                }

                return result;
            }
        }

        /// <summary>
        /// Finds a child tree node by name.
        /// </summary>
        /// <param name="childPathItems">An enumeration containing the path to the child stream tree node to be added.</param>
        /// <returns>The stream tree node corresponding to the child if found, or null otherwise.</returns>
        private StreamTreeNode FindChild(IEnumerable<string> childPathItems)
        {
            var firstPathItem = childPathItems.First();
            var firstPathItemNode = this.internalChildren.FirstOrDefault(p => p.Name == firstPathItem);
            if (firstPathItemNode == default)
            {
                return null;
            }

            if (childPathItems.Count() == 1)
            {
                return firstPathItemNode;
            }
            else
            {
                return firstPathItemNode.FindChild(childPathItems.Skip(1));
            }
        }

        /// <summary>
        /// Customizes a list of visualizers by inserting custom adapters where necessary.
        /// </summary>
        /// <param name="metadatas">The list of visualizers.</param>
        private void InsertCustomAdapters(List<VisualizerMetadata> metadatas)
        {
            // For each of the non-universal visualization objects, add a data adapter from the stream data type to the subfield data type
            for (int index = 0; index < metadatas.Count; index++)
            {
                // For message visualization object insert a custom object adapter so values can be displayed for known types.
                if (metadatas[index].VisualizationObjectType == typeof(MessageVisualizationObject))
                {
                    var objectAdapterType = typeof(ObjectAdapter<>).MakeGenericType(this.DataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(objectAdapterType);
                }
                else if (metadatas[index].VisualizationObjectType == typeof(LatencyVisualizationObject))
                {
                    // o/w for latency visualization object insert a custom object adapter so values can be displayed for known types.
                    var objectToLatencyAdapterType = typeof(ObjectToLatencyAdapter<>).MakeGenericType(this.DataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(objectToLatencyAdapterType);
                }
                else if (metadatas[index].StreamAdapterType == null && metadatas[index].DataType.IsInterface)
                {
                    // o/w for interface types inject an interface adapter
                    var interfaceAdapterType = typeof(InterfaceAdapter<,>).MakeGenericType(this.DataType, metadatas[index].DataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(interfaceAdapterType);
                }
            }
        }

        /// <summary>
        /// Attempts to add all member derived stream children.
        /// </summary>
        /// <param name="membersNotAdded">An output parameter containing all the members that were not added (b/c of already existing children).</param>
        private void AddMemberDerivedStreams(out List<string> membersNotAdded)
        {
            // Maintain the set of properties that could not be added.
            membersNotAdded = new List<string>();

            // Determine if the current node is a reference or nullable type
            var dataTypeIsReferenceOrNullable = this.IsReferenceOrNullableType(this.DataType);

            var dataType = this.DataType;
            string memberPathPrefix = string.Empty;

            // Member expansion of Nullable types needs to be handled slightly differently by adding a
            // [HasValue] node representing the Nullable.HasValue property, followed by the members of
            // the underlying Value if it can be further expanded. We wrap the HasValue node in []s to
            // prevent any possible clash if the underlying type also contains a member named HasValue.
            if (this.IsNullableDataType)
            {
                // HasValue always has a value, so we can simply expand it as a bool (generateNullableMemberType = false)
                this.AddMemberDerivedStream(this, "[HasValue]", "HasValue", typeof(bool), generateNullableMemberType: false);

                // This will cause the members of the underlying Nullable Value to be expanded below
                memberPathPrefix = "Value.";
                dataType = Nullable.GetUnderlyingType(this.DataType);
            }

            // Create a child node for each public instance property that takes no parameters.
            foreach (var propertyInfo in dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => !property.GetMethod.GetParameters().Any()))
            {
                var child = this.AddMemberDerivedStream(this, propertyInfo.Name, memberPathPrefix + propertyInfo.Name, propertyInfo.PropertyType, dataTypeIsReferenceOrNullable && !this.IsReferenceOrNullableType(propertyInfo.PropertyType));
                if (child == null)
                {
                    membersNotAdded.Add(propertyInfo.Name);
                }
            }

            // Create a child node for each public instance field
            foreach (var fieldInfo in dataType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var child = this.AddMemberDerivedStream(this, fieldInfo.Name, memberPathPrefix + fieldInfo.Name, fieldInfo.FieldType, dataTypeIsReferenceOrNullable && !this.IsReferenceOrNullableType(fieldInfo.FieldType));
                if (child == null)
                {
                    membersNotAdded.Add(fieldInfo.Name);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this stream tree node can add derived member streams.
        /// </summary>
        /// <returns>True if the stream tree node can add derived member streams.</returns>
        private bool CanAddMemberDerivedStreams()
            => this.DataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => !property.GetMethod.GetParameters().Any()).Any() ||
                this.DataType.GetFields(BindingFlags.Public | BindingFlags.Instance).Any();

        /// <summary>
        /// Gets a value indicating whether a specified type is a reference or nullable type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the specified type is a reference or nullable type.</returns>
        private bool IsReferenceOrNullableType(Type type)
            => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

        /// <summary>
        /// Adds a derived member stream child.
        /// </summary>
        /// <param name="parent">The parent stream tree node.</param>
        /// <param name="nodeName">The name of the child node to add.</param>
        /// <param name="memberPath">The path to the member.</param>
        /// <param name="memberType">The member type.</param>
        /// <param name="generateNullableMemberType">Indicates whether to generate a nullable member type expansion.</param>
        private StreamTreeNode AddMemberDerivedStream(StreamTreeNode parent, string nodeName, string memberPath, Type memberType, bool generateNullableMemberType)
        {
            // Compute the memberType
            if (generateNullableMemberType)
            {
                memberType = typeof(Nullable<>).MakeGenericType(memberType);
            }

            // Compute the member adapter type name and its parameters
            var derivedStreamMemberAdapterType = typeof(StreamMemberAdapter<,>).MakeGenericType(parent.DataType, memberType);
            var derivedStreamMemberAdapterArguments = new object[] { memberPath };

            // Now, if the parent is also a derived stream tree node
            if (parent.IsDerivedStream)
            {
                // Then update the stream adapter by chaining
                derivedStreamMemberAdapterType = typeof(ChainedStreamAdapter<,,,,>)
                    .MakeGenericType(
                        VisualizationContext.Instance.GetDataType(parent.SourceStreamMetadata.TypeName),
                        parent.DataType,
                        memberType,
                        parent.DerivedStreamAdapterType,
                        derivedStreamMemberAdapterType);

                // And the parameters
                derivedStreamMemberAdapterArguments = new object[] { parent.DerivedStreamAdapterArguments, derivedStreamMemberAdapterArguments };
            }

            // Construct the member stream node to add
            return this.AddChild(nodeName, this.SourceStreamMetadata, derivedStreamMemberAdapterType, derivedStreamMemberAdapterArguments, true);
        }

        /// <summary>
        /// Attempts to add all dictionary key stream children.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="keysNotAdded">An output parameter containing all the keys that were not added (b/c of already existing keys).</param>
        private void AddDictionaryKeyDerivedStreams<TKey, TValue>(out List<string> keysNotAdded)
        {
            var keysToAdd = new Dictionary<string, TKey>();
            keysNotAdded = new List<string>();

            // Create the progress dialog to displayed while we traverse the entire stream for keys
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, $"Extracting dictionary keys", showCancelButton: true);
            bool? dialogResult;

            void GetAllDictionaryKeys(Dictionary<TKey, TValue> dictionary, Envelope envelope)
            {
                string errorString = default;

                foreach (var kvp in dictionary)
                {
                    var keyString = kvp.Key.ToString();

                    // If we have already seen this key string, it had better represent the same key (in the Equals() sense)
                    if (keysToAdd.TryGetValue(keyString, out var existingKey))
                    {
                        if (!kvp.Key.Equals(existingKey))
                        {
                            errorString = $"Could not expand the dictionary keys as the dictionary contains multiple keys which map to the same key string: {keyString}.";
                        }
                    }
                    else if (keyString.Contains('.'))
                    {
                        errorString = $"Could not expand the dictionary keys as one of the key strings contains an illegal character [.]: {keyString}.";
                    }
                    else
                    {
                        // OK to add this new key
                        keysToAdd.Add(keyString, kvp.Key);
                    }

                    // If there was a problem with any key, abort the pipeline and display an error
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        // Abort without adding any keys
                        keysToAdd.Clear();

                        // Setting the DialogResult to false will close the modal dialog window and dispose the pipeline
                        Application.Current.Dispatcher.Invoke(() => progressWindow.DialogResult = false);

                        // Display an error dialog
                        Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            new MessageBoxWindow(
                                Application.Current.MainWindow,
                                "Error",
                                errorString,
                                "Close",
                                null).ShowDialog();
                        }));

                        return;
                    }
                }
            }

            // Create and run a pipeline to read all the keys from the store. This will also perform checks to
            // ensure that all keys map to a unique key string and that no key string contains a '.' character.
            using (var pipeline = Pipeline.Create(deliveryPolicy: DeliveryPolicy.SynchronousOrThrottle))
            {
                var reader = StreamReader.Create(this.PartitionViewModel.StoreName, this.PartitionViewModel.StorePath, this.PartitionViewModel.Partition.StreamReaderTypeName);
                var store = new Importer(pipeline, reader, usePerStreamReaders: true);

                if (!this.IsDerivedStream)
                {
                    // Apply the GetAllDictionaryKeys function directly to the source stream
                    var stream = store.OpenStream<Dictionary<TKey, TValue>>(this.SourceStreamMetadata.Name);
                    stream.Do(GetAllDictionaryKeys);
                }
                else
                {
                    // If this is a derived stream, we need to adapt the GetAllDictionaryKeys function using the derived stream adapter
                    dynamic derivedStreamAdapter = Activator.CreateInstance(this.DerivedStreamAdapterType, this.DerivedStreamAdapterArguments);
                    dynamic adaptedGetAllDictionaryKeys = derivedStreamAdapter.AdaptReceiver(new Action<Dictionary<TKey, TValue>, Envelope>(GetAllDictionaryKeys));

                    // Apply the adapted GetAllDictionaryKeys function to the source stream
                    var stream = store.OpenStream(this.SourceStreamMetadata.Name, derivedStreamAdapter.SourceAllocator, derivedStreamAdapter.SourceDeallocator);
                    Psi.Operators.Do(stream, adaptedGetAllDictionaryKeys);
                }

                IProgress<double> progress = new Progress<double>(p =>
                {
                    progressWindow.Progress = p;

                    if (p == 1.0 && !progressWindow.DialogResult.HasValue)
                    {
                        // close the progress window when the pipeline reports completion
                        progressWindow.DialogResult = true;
                    }
                });

                pipeline.RunAsync(ReplayDescriptor.ReplayAll, progress);
                dialogResult = progressWindow.ShowDialog();
            }

            // This means the key extraction pipeline ran to completion successfully and we can add the keys
            if (dialogResult == true)
            {
                // Create a child node for each key
                foreach (var keyString in keysToAdd.Keys)
                {
                    var child = this.AddDictionaryKeyDerivedStream<TKey, TValue>(this, keyString, !this.IsReferenceOrNullableType(typeof(TValue)));
                    if (child == null)
                    {
                        keysNotAdded.InsertSorted(keyString);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this stream tree node can add derived dictionary key streams.
        /// </summary>
        /// <returns>True if the stream tree node can add derived dictionary key streams.</returns>
        private bool CanAddDictionaryKeyDerivedStreams()
            => this.DataType.IsGenericType && this.DataType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

        /// <summary>
        /// Adds a dictionary key stream child.
        /// </summary>
        /// <param name="parent">The parent stream tree node.</param>
        /// <param name="keyString">The dictionary key string.</param>
        /// <param name="generateNullableValueType">Indicates whether to generate a nullable dictionary value type expansion.</param>
        private StreamTreeNode AddDictionaryKeyDerivedStream<TKey, TValue>(StreamTreeNode parent, string keyString, bool generateNullableValueType)
        {
            var destinationType = typeof(TValue);

            // Compute the memberType
            if (generateNullableValueType)
            {
                destinationType = typeof(Nullable<>).MakeGenericType(destinationType);
            }

            // Compute the dictionary key-to-value adapter type name and its parameters
            var derivedStreamKeyToValueAdapterType = typeof(DictionaryKeyToValueAdapter<,,>).MakeGenericType(typeof(TKey), typeof(TValue), destinationType);
            var derivedStreamKeyToValueAdapterArguments = new object[] { keyString };

            // Now, if the parent is also a derived stream tree node
            if (parent.IsDerivedStream)
            {
                // Then update the stream adapter by chaining
                derivedStreamKeyToValueAdapterType = typeof(ChainedStreamAdapter<,,,,>)
                    .MakeGenericType(
                        VisualizationContext.Instance.GetDataType(parent.SourceStreamMetadata.TypeName),
                        parent.DataType,
                        destinationType,
                        parent.DerivedStreamAdapterType,
                        derivedStreamKeyToValueAdapterType);

                // And the parameters
                derivedStreamKeyToValueAdapterArguments = new object[] { parent.DerivedStreamAdapterArguments, derivedStreamKeyToValueAdapterArguments };
            }

            // Construct the member stream node to add
            return this.AddChild($"[{keyString}]", this.SourceStreamMetadata, derivedStreamKeyToValueAdapterType, derivedStreamKeyToValueAdapterArguments, true);
        }

        /// <summary>
        /// Attempts to add a script derived stream.
        /// </summary>
        /// <param name="errorString">An output parameter representing an error string.</param>
        /// <returns>Whether or not the derived stream tree node was added.</returns>
        private bool AddScriptDerivedStream(out string errorString)
        {
            errorString = null;

            var scriptWindow = new ScriptWindow(Application.Current.MainWindow, this);

            // Show the script editor dialog
            if (scriptWindow.ShowDialog() == true)
            {
                var script = scriptWindow.ScriptText;
                var usings = scriptWindow.Usings;
                var scriptName = scriptWindow.ScriptDerivedStreamName;
                var returnType = scriptWindow.ReturnType;

                // Compute the scripting adapter type name and its parameters
                var derivedScriptedStreamAdapterType = typeof(ScriptAdapter<,>).MakeGenericType(this.DataType, returnType);
                var derivedScriptedStreamAdapterArguments = new object[] { script, usings };

                // Now, if this is also a derived stream tree node
                if (this.IsDerivedStream)
                {
                    // Then update the stream adapter by chaining
                    derivedScriptedStreamAdapterType = typeof(ChainedStreamAdapter<,,,,>)
                        .MakeGenericType(
                            VisualizationContext.Instance.GetDataType(this.SourceStreamMetadata.TypeName),
                            this.DataType,
                            returnType,
                            this.DerivedStreamAdapterType,
                            derivedScriptedStreamAdapterType);

                    // And the parameters
                    derivedScriptedStreamAdapterArguments = new object[] { this.DerivedStreamAdapterArguments, derivedScriptedStreamAdapterArguments };
                }

                // Construct the script stream node to add
                var child = this.AddChild($"{scriptName}", this.SourceStreamMetadata, derivedScriptedStreamAdapterType, derivedScriptedStreamAdapterArguments);
                if (child == null)
                {
                    errorString = $"This node already contains a script derived stream named {scriptName}.";
                }

                return child != null;
            }

            return false;
        }

        /// <summary>
        /// Edits the script of an existing script-derived stream.
        /// </summary>
        /// <param name="errorString">An output parameter representing an error string.</param>
        private void ModifyDerivedStreamScript(out string errorString)
        {
            if (this.TryGetScriptParameters(out string script, out var usings))
            {
                var parent = this.PartitionViewModel.FindStreamTreeNode(this.FullName.Substring(0, this.FullName.LastIndexOf('.')));
                var scriptWindow = new ScriptWindow(Application.Current.MainWindow, parent, false)
                {
                    ScriptText = script,
                    ScriptDerivedStreamName = this.Name,
                    ReturnType = this.DataType,
                    Usings = new ObservableCollection<string>(usings),
                };

                // Allow editing of existing script (but not the script name or return type as those are already baked into this node)
                if (scriptWindow.ShowDialog() == true)
                {
                    this.SetScriptParameters(scriptWindow.ScriptText, scriptWindow.Usings);
                }

                errorString = null;
            }
            else
            {
                errorString = "Failed to retrieve the script parameters from the adapter.";
            }
        }

        /// <summary>
        /// Attempts to get the script parameters from the last adapter in the chain.
        /// </summary>
        /// <param name="scriptText">A string containing the script code.</param>
        /// <param name="usings">An enumeration of usings required by the script.</param>
        /// <returns>A flag indicating whether the script parameters were found.</returns>
        private bool TryGetScriptParameters(out string scriptText, out IEnumerable<string> usings)
        {
            Type currentAdapterType = this.DerivedStreamAdapterType;
            object[] currentAdapterArguments = this.DerivedStreamAdapterArguments;

            // search for the final ScriptingAdapter
            while (currentAdapterType.IsGenericType)
            {
                if (currentAdapterType.GetGenericTypeDefinition() == typeof(ScriptAdapter<,>))
                {
                    scriptText = currentAdapterArguments[0] as string;
                    usings = currentAdapterArguments[1] as IEnumerable<string>;
                    return true;
                }
                else if (currentAdapterType.GetGenericTypeDefinition() == typeof(ChainedStreamAdapter<,,,,>))
                {
                    // continue searching with the second adapter in the chain
                    currentAdapterType = currentAdapterType.GetGenericArguments()[4];
                    currentAdapterArguments = currentAdapterArguments[1] as object[];
                }
                else
                {
                    break;
                }
            }

            // Not a derived script stream, or bad arguments.
            scriptText = null;
            usings = null;

            return false;
        }

        /// <summary>
        /// Sets the script parameters.
        /// </summary>
        /// <param name="scriptText">A string containing the script code.</param>
        /// <param name="usings">An enumeration of usings required by the script.</param>
        private void SetScriptParameters(string scriptText, IEnumerable<string> usings)
        {
            Type currentAdapterType = this.DerivedStreamAdapterType;
            object[] currentAdapterArguments = this.DerivedStreamAdapterArguments;

            while (currentAdapterType.IsGenericType)
            {
                if (currentAdapterType.GetGenericTypeDefinition() == typeof(ScriptAdapter<,>))
                {
                    currentAdapterArguments[0] = scriptText;
                    currentAdapterArguments[1] = usings;
                    return;
                }
                else if (currentAdapterType.GetGenericTypeDefinition() == typeof(ChainedStreamAdapter<,,,,>))
                {
                    currentAdapterType = currentAdapterType.GetGenericArguments()[4];
                    currentAdapterArguments = currentAdapterArguments[1] as object[];
                }
                else
                {
                    throw new InvalidOperationException($"No adapter of type {typeof(ScriptAdapter<,>).Name} found.");
                }
            }
        }
    }
}
