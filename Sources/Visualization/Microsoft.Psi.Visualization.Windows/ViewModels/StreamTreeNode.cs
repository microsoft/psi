// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Controls;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a node in the dataset tree that represents a stream.
    /// </summary>
    public class StreamTreeNode : StreamContainerTreeNode
    {
        private bool isDirty = false;
        private bool? supplementalMetadataTypeIsKnown = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the stream tree node.</param>
        /// <param name="path">The path to the stream tree node.</param>
        /// <param name="name">The name of the stream tree node.</param>
        /// <param name="streamMetadata">The stream metadata.</param>
        public StreamTreeNode(PartitionViewModel partitionViewModel, string path, string name, IStreamMetadata streamMetadata)
            : base(partitionViewModel, path, name)
        {
            this.SourceStreamMetadata = streamMetadata;
            this.DataTypeName = this.SourceStreamMetadata.TypeName;
        }

        /// <summary>
        /// Gets or sets the metadata of the source data stream.
        /// </summary>
        [Browsable(false)]
        public IStreamMetadata SourceStreamMetadata { get; protected set; }

        /// <summary>
        /// Gets or sets the type of data represented by this stream tree node.
        /// </summary>
        [Browsable(false)]
        public string DataTypeName { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stream has unsaved changes.
        /// </summary>
        [Browsable(false)]
        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.RaisePropertyChanging(nameof(this.DisplayName));
                this.isDirty = value;
                this.RaisePropertyChanged(nameof(this.DisplayName));
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
                // If there is no supplemental metadata for the stream, then everything is fine.
                if (string.IsNullOrWhiteSpace(this.SourceStreamMetadata.SupplementalMetadataTypeName))
                {
                    return true;
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
        /// Gets a value indicating whether the node represends a stream backed by a \psi store.
        /// </summary>
        [Browsable(false)]
        public bool IsPsiStream => this.SourceStreamMetadata is PsiStreamMetadata;

        /// <summary>
        /// Gets a value indicating whether the node represends a indexed stream backed by a \psi store.
        /// </summary>
        [Browsable(false)]
        public bool IsIndexedPsiStream => this.SourceStreamMetadata is PsiStreamMetadata psiStreamMetadata && psiStreamMetadata.IsIndexed;

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages under this stream.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval => new TimeInterval(this.SourceStreamFirstMessageOriginatingTime, this.SourceStreamLastMessageOriginatingTime);

        /// <summary>
        /// Gets the id of the data stream.
        /// </summary>
        [DisplayName("Source Stream Id")]
        [Description("The id of the source stream.")]
        public int SourceStreamId => this.SourceStreamMetadata.Id;

        /// <summary>
        /// Gets the stream name of this stream tree node.
        /// </summary>
        [DisplayName("Source Stream Name")]
        [Description("The name of the source stream.")]
        public string SourceStreamName => this.SourceStreamMetadata.Name;

        /// <summary>
        /// Gets the type of messages in this stream tree node.
        /// </summary>
        [DisplayName("Source Stream Type")]
        [Description("The type of messages in the source stream.")]
        public string SourceStreamTypeNameSimplified => TypeSpec.Simplify(this.SourceStreamMetadata.TypeName);

        /// <summary>
        /// Gets the type of data represented by this stream tree node.
        /// </summary>
        [DisplayName("Data Type")]
        [Description("The type of data contained by this node.")]
        public string DataTypeNameSimplified => TypeSpec.Simplify(this.DataTypeName);

        /// <inheritdoc/>
        public override double SubsumedAverageMessageSize =>
            this.HasNonDerivedChildren ?
                ((base.SubsumedAverageMessageSize * base.SubsumedMessageCount) + (this.SourceStreamAverageMessageSize * this.SourceStreamMessageCount)) /
                (base.SubsumedMessageCount + this.SourceStreamMessageCount) :
                this.SourceStreamAverageMessageSize;

        /// <summary>
        /// Gets the average message size in the stream.
        /// </summary>
        [DisplayName("Source Stream Avg. Message Size")]
        [Description("The average size (in bytes) of messages in the source stream.")]
        public virtual double SourceStreamAverageMessageSize => this.SourceStreamMetadata.AverageMessageSize;

        /// <inheritdoc/>
        public override double SubsumedAverageMessageLatencyMs =>
            this.HasNonDerivedChildren ?
                ((base.SubsumedAverageMessageLatencyMs * base.SubsumedMessageCount) + (this.SourceStreamAverageMessageLatencyMs * this.SourceStreamMessageCount)) /
                    (base.SubsumedMessageCount + this.SourceStreamMessageCount) :
                this.SourceStreamAverageMessageLatencyMs;

        /// <summary>
        /// Gets the average latency of messages in the streams(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Source Stream Avg. Message Latency (ms)")]
        [Description("The average latency (in milliseconds) of messages in the source stream.")]
        public virtual double SourceStreamAverageMessageLatencyMs => this.SourceStreamMetadata.AverageMessageLatencyMs;

        /// <inheritdoc/>
        public override DateTime SubsumedOpenedTime =>
            this.HasNonDerivedChildren ?
                new DateTime(Math.Min(base.SubsumedOpenedTime.Ticks, this.SourceStreamOpenedTime.Ticks)) :
                this.SourceStreamOpenedTime;

        /// <summary>
        /// Gets the time at which the stream was opened.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SourceStreamOpenedTime => this.SourceStreamMetadata.OpenedTime;

        /// <inheritdoc/>
        public override DateTime SubsumedClosedTime =>
            this.HasNonDerivedChildren ?
                new DateTime(Math.Max(base.SubsumedClosedTime.Ticks, this.SourceStreamClosedTime.Ticks)) :
                this.SourceStreamClosedTime;

        /// <summary>
        /// Gets the time at which the stream was closed.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SourceStreamClosedTime => this.SourceStreamMetadata.ClosedTime;

        /// <inheritdoc/>
        public override DateTime SubsumedFirstMessageOriginatingTime =>
            this.HasNonDerivedChildren ?
                new DateTime(Math.Min(base.SubsumedFirstMessageOriginatingTime.Ticks, this.SourceStreamFirstMessageOriginatingTime.Ticks)) :
                this.SourceStreamFirstMessageOriginatingTime;

        /// <summary>
        /// Gets the originating time of the first message in the stream.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SourceStreamFirstMessageOriginatingTime => this.SourceStreamMetadata.FirstMessageOriginatingTime;

        /// <inheritdoc/>
        public override DateTime SubsumedFirstMessageCreationTime =>
            this.HasNonDerivedChildren ?
                new DateTime(Math.Min(base.SubsumedFirstMessageCreationTime.Ticks, this.SourceStreamFirstMessageCreationTime.Ticks)) :
                this.SourceStreamFirstMessageCreationTime;

        /// <summary>
        /// Gets the creation time of the first message in the stream.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SourceStreamFirstMessageCreationTime => this.SourceStreamMetadata.FirstMessageCreationTime;

        /// <inheritdoc/>
        public override DateTime SubsumedLastMessageOriginatingTime =>
            this.HasNonDerivedChildren ?
                new DateTime(Math.Max(base.SubsumedLastMessageOriginatingTime.Ticks, this.SourceStreamLastMessageOriginatingTime.Ticks)) :
                this.SourceStreamLastMessageOriginatingTime;

        /// <summary>
        /// Gets the originating time of the last message in the stream.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SourceStreamLastMessageOriginatingTime => this.SourceStreamMetadata.LastMessageOriginatingTime;

        /// <inheritdoc/>
        public override DateTime SubsumedLastMessageCreationTime =>
            this.HasNonDerivedChildren ?
                new DateTime(Math.Max(base.SubsumedLastMessageCreationTime.Ticks, this.SourceStreamLastMessageCreationTime.Ticks)) :
                this.SourceStreamLastMessageCreationTime;

        /// <summary>
        /// Gets the creation time of the last message in the stream.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SourceStreamLastMessageCreationTime => this.SourceStreamMetadata.LastMessageCreationTime;

        /// <inheritdoc/>
        public override long SubsumedMessageCount =>
            this.HasNonDerivedChildren ?
                base.SubsumedMessageCount + this.SourceStreamMessageCount :
                this.SourceStreamMessageCount;

        /// <summary>
        /// Gets the number of messages in the stream.
        /// </summary>
        [DisplayName("Source Stream Message Count")]
        [Description("The total number of messages in the stream.")]
        public virtual long SourceStreamMessageCount => this.SourceStreamMetadata.MessageCount;

        /// <inheritdoc/>
        public override long SubsumedSize =>
            this.HasNonDerivedChildren ?
                base.SubsumedSize + this.SourceStreamSize :
                this.SourceStreamSize;

        /// <summary>
        /// Gets the total size of the messages in the stream.
        /// </summary>
        [DisplayName("Source Stream Size")]
        [Description("The size (in bytes) of data in the stream.")]
        public virtual long SourceStreamSize
        {
            get
            {
                if (this.SourceStreamMetadata is PsiStreamMetadata psiStreamMetadata)
                {
                    return psiStreamMetadata.MessageSizeCumulativeSum;
                }
                else
                {
                    return (long)(this.SourceStreamMetadata.AverageMessageSize * this.SourceStreamMetadata.MessageCount);
                }
            }
        }

        /// <summary>
        /// Gets a string representation of the source stream opened time.
        /// </summary>
        [DisplayName("Source Stream OpenedTime")]
        [Description("The opened time for the source stream.")]
        public virtual string SourceStreamOpenedTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SourceStreamOpenedTime);

        /// <summary>
        /// Gets a string representation of the source stream closed time.
        /// </summary>
        [DisplayName("Source Stream ClosedTime")]
        [Description("The closed time for the source stream.")]
        public virtual string SourceStreamClosedTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SourceStreamClosedTime);

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the stream.
        /// </summary>
        [DisplayName("Source Stream First Message OriginatingTime")]
        [Description("The originating time of the first message in the stream.")]
        public virtual string SourceStreamFirstMessageOriginatingTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SourceStreamFirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the first message in the stream.
        /// </summary>
        [DisplayName("Source Stream First Message CreationTime")]
        [Description("The creation time of the first message in the stream.")]
        public virtual string SourceStreamFirstMessageCreationTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SourceStreamFirstMessageCreationTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the stream.
        /// </summary>
        [DisplayName("Source Stream Last Message OriginatingTime")]
        [Description("The originating time of the last message in the stream.")]
        public virtual string SourceStreamLastMessageOriginatingTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SourceStreamLastMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the last message in the stream.
        /// </summary>
        [DisplayName("Source Stream Last Message CreationTime")]
        [Description("The creation time of the last message in the stream.")]
        public virtual string SourceStreamLastMessageCreationTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SourceStreamLastMessageCreationTime);

        /// <inheritdoc/>
        public override string DisplayName => this.IsDirty ? $"{this.Name}*" : this.Name;

        /// <inheritdoc/>
        public override string IconSource
        {
            get
            {
                if (VisualizationContext.Instance.GetDataType(this.DataTypeName) == typeof(AudioBuffer))
                {
                    // Audio stream
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamAudioMutedLive : IconSourcePath.StreamAudioMuted;
                }
                else if (VisualizationContext.Instance.GetDataType(this.DataTypeName)?.Name == nameof(TimeIntervalAnnotation))
                {
                    // Annotation
                    return IconSourcePath.Annotation;
                }
                else if (this.InternalChildren.Any())
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
        /// Ensures that a derived stream exists as a child of this stream tree node.
        /// </summary>
        /// <param name="streamBinding">The stream binding for the derived stream.</param>
        public virtual void EnsureDerivedStreamExists(StreamBinding streamBinding)
        {
            var memberPath = default(string);
            if (streamBinding.NodePath.StartsWith(streamBinding.StreamName))
            {
                memberPath = streamBinding.NodePath.Substring(streamBinding.StreamName.Length + 1);
            }
            else
            {
                throw new Exception("Unexpected derived stream binding.");
            }

            if (this.FindStreamTreeNode(memberPath) == null)
            {
                this.AddDerivedMemberStreamChildren();
                this.ExpandAll();
            }

            if (memberPath.Contains('.'))
            {
                var pathItems = memberPath.Split('.');
                if (this.InternalChildren.FirstOrDefault(p => p.Name == pathItems.First()) is DerivedMemberStreamTreeNode memberChild)
                {
                    memberChild.EnsureDerivedStreamExists(streamBinding);
                }
            }
        }

        /// <summary>
        /// Selects the list of visualizers compatible with this stream tree node.
        /// </summary>
        /// <param name="visualizationPanel">The visualization panel where it is intended to visualize the data, or visualizers targeting any panels should be returned.</param>
        /// <param name="isUniversal">A nullable boolean indicating constraints on whether the visualizer should be a universal one (visualize messages, visualize latency etc).</param>
        /// <param name="isInNewPanel">A nullable boolean indicating constraints on whether the visualizer should be a "in new panel" one.</param>
        /// <returns>The matching list of visualizers.</returns>
        public virtual List<VisualizerMetadata> GetCompatibleVisualizers(
            VisualizationPanel visualizationPanel = null,
            bool? isUniversal = null,
            bool? isInNewPanel = null)
        {
            var results = new List<VisualizerMetadata>();
            var nodeDataType = VisualizationContext.Instance.GetDataType(this.DataTypeName);
            var comparer = new VisualizerMetadataComparer(nodeDataType);

            // If we're looking for visualizers that fit in any panel
            if (visualizationPanel == null)
            {
                results.AddRange(VisualizationContext.Instance.PluginMap.Visualizers.Where(v =>
                    (nodeDataType == v.DataType || nodeDataType.IsSubclassOf(v.DataType)) &&
                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value) &&
                    (!isUniversal.HasValue || v.IsUniversalVisualizer == isUniversal))
                    .OrderBy(v => v, comparer));
            }
            else
            {
                // o/w find out the compatible panel types
                results.AddRange(VisualizationContext.Instance.PluginMap.Visualizers.Where(v =>
                    visualizationPanel.CompatiblePanelTypes.Contains(v.VisualizationPanelType) &&
                    (nodeDataType == v.DataType || nodeDataType.IsSubclassOf(v.DataType)) &&
                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value) &&
                    (!isUniversal.HasValue || v.IsUniversalVisualizer == isUniversal))
                    .OrderBy(v => v, comparer));
            }

            // Special-case: for streams of type Dictionary<TKey, numeric>, create the corresponding
            // numeric series visualizer, by using a dictionary-key-to-string adapter.
            if ((!isUniversal.HasValue || !isUniversal.Value) &&
                (visualizationPanel == null || visualizationPanel.CompatiblePanelTypes.Contains(VisualizationPanelType.Timeline)))
            {
                if (nodeDataType.IsGenericType && nodeDataType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var genericArguments = nodeDataType.GetGenericArguments();
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

            return results;
        }

        /// <summary>
        /// Creates a stream binding for this stream tree node and a specified visualizer.
        /// </summary>
        /// <param name="visualizerMetadata">The visualizer to create a stream binding for.</param>
        /// <returns>A corresponding stream binding.</returns>
        public virtual StreamBinding CreateStreamBinding(VisualizerMetadata visualizerMetadata) =>
            new StreamBinding(
                this.SourceStreamMetadata.Name,
                this.PartitionViewModel.Name,
                this.Path,
                visualizerMetadata.StreamAdapterType,
                null,
                visualizerMetadata.SummarizerType,
                null,
                false);

        /// <summary>
        /// Customizes a list of visualizers by inserting custom adapters where necessary.
        /// </summary>
        /// <param name="metadatas">The list of visualizers.</param>
        protected virtual void InsertCustomAdapters(List<VisualizerMetadata> metadatas)
        {
            var streamSourceDataType = VisualizationContext.Instance.GetDataType(this.SourceStreamMetadata.TypeName);

            // For each of the non-universal visualization objects, add a data adapter from the stream data type to the subfield data type
            for (int index = 0; index < metadatas.Count; index++)
            {
                // For message visualization object insert a custom object adapter so values can be displayed for known types.
                if (metadatas[index].VisualizationObjectType == typeof(MessageVisualizationObject))
                {
                    var objectAdapterType = typeof(ObjectAdapter<>).MakeGenericType(streamSourceDataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(objectAdapterType);
                }

                // For latency visualization object insert a custom object adapter so values can be displayed for known types.
                if (metadatas[index].VisualizationObjectType == typeof(LatencyVisualizationObject))
                {
                    var objectToLatencyAdapterType = typeof(ObjectToLatencyAdapter<>).MakeGenericType(streamSourceDataType);
                    metadatas[index] = metadatas[index].GetCloneWithNewStreamAdapterType(objectToLatencyAdapterType);
                }
            }
        }

        /// <summary>
        /// Adds all derived member stream children.
        /// </summary>
        protected virtual void AddDerivedMemberStreamChildren()
        {
            // Get the type of this node.
            Type dataType = TypeResolutionHelper.GetVerifiedType(this.DataTypeName);

            if (dataType != null)
            {
                // Determine if the current node is a reference type
                var isReference = !dataType.IsValueType || Nullable.GetUnderlyingType(dataType) != null;

                // Create a child node for each public instance property that takes no parameters.
                foreach (PropertyInfo propertyInfo in dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => !property.GetMethod.GetParameters().Any()))
                {
                    this.AddDerivedMemberStreamChild(propertyInfo, propertyInfo.PropertyType, isReference && propertyInfo.PropertyType.IsValueType);
                }

                // Create a child node for each public instance field
                foreach (FieldInfo fieldInfo in dataType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    this.AddDerivedMemberStreamChild(fieldInfo, fieldInfo.FieldType, isReference && fieldInfo.FieldType.IsValueType);
                }
            }
        }

        /// <summary>
        /// Adds a derived member stream child.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="memberType">The member type.</param>
        /// <param name="generateNullable">Indicates whether we should do a nullable expansion.</param>
        protected void AddDerivedMemberStreamChild(MemberInfo memberInfo, Type memberType, bool generateNullable)
        {
            var child = new DerivedMemberStreamTreeNode(this, memberInfo, memberType, generateNullable);

            // Insert the child into the existing list, before all non-member sub-streams, and in alphabetical order
            var lastOrDefault = this.InternalChildren.LastOrDefault(stn => string.Compare(stn.Name, memberInfo.Name) < 0 && stn is StreamTreeNode);
            var index = lastOrDefault != null ? this.InternalChildren.IndexOf(lastOrDefault) + 1 : 0;
            this.InternalChildren.Insert(index, child);
        }

        /// <inheritdoc/>
        protected override void PopulateContextMenu(ContextMenu contextMenu)
        {
            this.PopulateContextMenuWithVisualizers(contextMenu);
            this.PopulateContextMenuWithExpandMembers(contextMenu);
            this.PopulateContextMenuWithZoomToStream(contextMenu);

            base.PopulateContextMenu(contextMenu);
        }

        /// <summary>
        /// Populates a specified context menu with visualizers.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        protected void PopulateContextMenuWithVisualizers(ContextMenu contextMenu)
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
        /// Populates a specified context menu with expand members command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        protected void PopulateContextMenuWithExpandMembers(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.StreamMember,
                    ContextMenuName.ExpandMembers,
                    new VisualizationCommand(() =>
                    {
                        this.AddDerivedMemberStreamChildren();
                        this.ExpandAll();
                    }),
                    isEnabled: this.CanExpandDerivedMemberStreams() && !this.InternalChildren.Any(c => c is DerivedMemberStreamTreeNode)));
        }

        /// <summary>
        /// Gets a value indicating whether this stream tree node can expand derived members.
        /// </summary>
        /// <returns>True if the stream tree node can expand derived members.</returns>
        protected virtual bool CanExpandDerivedMemberStreams()
        {
            Type nodeType = TypeResolutionHelper.GetVerifiedType(this.DataTypeName);

            if (nodeType != null)
            {
                return nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => !property.GetMethod.GetParameters().Any()).Any() ||
                    nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance).Any();
            }

            return false;
        }

        /// <summary>
        /// Populates a specified context menu with zoom to stream command.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        protected void PopulateContextMenuWithZoomToStream(ContextMenu contextMenu)
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
        /// Creates a menu item for visualizing the stream.
        /// </summary>
        /// <param name="metadata">The visualizer metadata.</param>
        /// <param name="showIcon">Indicates whether to show the icon.</param>
        /// <returns>The menu item.</returns>
        protected MenuItem CreateVisualizeStreamMenuItem(VisualizerMetadata metadata, bool showIcon = true)
        {
            return MenuItemHelper.CreateMenuItem(
                showIcon ? metadata.IconSourcePath : string.Empty,
                metadata.CommandText,
                new VisualizationCommand<VisualizerMetadata>(m => VisualizationContext.Instance.VisualizeStream(this, m, VisualizationContext.Instance.VisualizationContainer.CurrentPanel)),
                tag: metadata,
                commandParameter: metadata);
        }

        /// <inheritdoc/>
        protected override void OnDatasetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnDatasetViewModelPropertyChanged(sender, e);
            if (e.PropertyName == nameof(this.PartitionViewModel.SessionViewModel.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.IsInCurrentSession));
            }
        }

        /// <inheritdoc/>
        protected override void UpdateAuxiliaryInfo()
        {
            var indexedMarker = this.IsIndexedPsiStream ? "*" : string.Empty;
            var streamContainerPreamble = string.Empty;
            var hasNonDerivedChildren = this.InternalChildren.Where(c => c is not DerivedStreamTreeNode).Any();
            switch (this.PartitionViewModel.SessionViewModel.DatasetViewModel.ShowAuxiliaryStreamInfo)
            {
                case AuxiliaryStreamInfo.None:
                    this.AuxiliaryInfo = string.Empty;
                    break;
                case AuxiliaryStreamInfo.Size:
                    streamContainerPreamble = hasNonDerivedChildren ? $"[{SizeFormatHelper.FormatSize(this.SubsumedSize)}] " : string.Empty;
                    this.AuxiliaryInfo = streamContainerPreamble + indexedMarker + SizeFormatHelper.FormatSize(this.SourceStreamSize);
                    break;
                case AuxiliaryStreamInfo.MessageCount:
                    streamContainerPreamble = hasNonDerivedChildren ? this.SubsumedMessageCount == 0 ? "[0] " : $"[{this.SubsumedMessageCount:0,0}] " : string.Empty;
                    this.AuxiliaryInfo = streamContainerPreamble + (this.SourceStreamMessageCount == 0 ? "0" : $"{this.SourceStreamMessageCount:0,0}");
                    break;
                case AuxiliaryStreamInfo.AverageMessageLatencyMs:
                    streamContainerPreamble = hasNonDerivedChildren ? this.SubsumedAverageMessageLatencyMs < 1 ? "[<1 ms] " : $"[{this.SubsumedAverageMessageLatencyMs:0,0 ms}] " : string.Empty;
                    this.AuxiliaryInfo = streamContainerPreamble + (this.SourceStreamAverageMessageLatencyMs < 1 ? "<1 ms" : $"{this.SourceStreamAverageMessageLatencyMs:0,0 ms}");
                    break;
                case AuxiliaryStreamInfo.AverageMessageSize:
                    streamContainerPreamble = hasNonDerivedChildren ? $"[{SizeFormatHelper.FormatSize((long)this.SubsumedAverageMessageSize)}] " : string.Empty;
                    this.AuxiliaryInfo = streamContainerPreamble + indexedMarker + SizeFormatHelper.FormatSize((long)this.SourceStreamAverageMessageSize);
                    break;
                default:
                    break;
            }
        }
    }
}
