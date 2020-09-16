// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Defines a base class for nodes in a tree that hold information about data streams.
    /// </summary>
    public class StreamTreeNode : ObservableTreeNodeObject
    {
        private readonly ObservableCollection<StreamTreeNode> internalChildren;
        private readonly ReadOnlyObservableCollection<StreamTreeNode> children;

        private RelayCommand<StackPanel> contextMenuOpeningCommand;
        private bool isDirty = false;
        private bool? supplementalMetadataIsKnownType = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition where this stream tree node can be found.</param>
        public StreamTreeNode(PartitionViewModel partitionViewModel)
        {
            this.PartitionViewModel = partitionViewModel;
            this.PartitionViewModel.PropertyChanged += this.Partition_PropertyChanged;
            this.PartitionViewModel.SessionViewModel.DatasetViewModel.PropertyChanged += this.DatasetViewModel_PropertyChanged;
            this.internalChildren = new ObservableCollection<StreamTreeNode>();
            this.children = new ReadOnlyObservableCollection<StreamTreeNode>(this.internalChildren);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        ~StreamTreeNode()
        {
            this.PartitionViewModel.PropertyChanged -= this.Partition_PropertyChanged;
            this.PartitionViewModel.SessionViewModel.DatasetViewModel.PropertyChanged -= this.DatasetViewModel_PropertyChanged;
        }

        /// <summary>
        /// Gets the id of the data stream.
        /// </summary>
        [PropertyOrder(0)]
        [Description("The ID of the stream within the Store")]
        public int? Id => this.StreamMetadata?.Id;

        /// <summary>
        /// Gets or sets the name of this stream tree node.
        /// </summary>
        [PropertyOrder(1)]
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        [Browsable(false)]
        public string DisplayName => this.isDirty ? this.Name + "*" : this.Name;

        /// <summary>
        /// Gets or sets the stream name of this stream tree node.
        /// </summary>
        [PropertyOrder(2)]
        [DisplayName("Stream Name")]
        [Description("The name of the stream.")]
        public string StreamName { get; protected set; }

        /// <summary>
        /// Gets the type of messages in this stream tree node.
        /// </summary>
        [PropertyOrder(3)]
        [DisplayName("Message Type")]
        [Description("The type of messages in the stream.")]
        public string StreamTypeDisplayName => TypeSpec.Simplify(this.StreamTypeName);

        /// <summary>
        /// Gets the of this node relative to the node that represents the actual stream.
        /// If this stream tree node represents a stream and not some submember of the
        /// stream then this value will be nul..
        /// </summary>
        [PropertyOrder(4)]
        [DisplayName("Member Path")]
        [Description("The path from the messages in the stream to this property or field member")]
        public string MemberPath { get; private set; }

        /// <summary>
        /// Gets the type of data represented by this stream tree node.
        /// </summary>
        [PropertyOrder(5)]
        [DisplayName("Member Type")]
        [Description("The type of data represented by the member.")]
        public string NodeTypeDisplayName => TypeSpec.Simplify(this.NodeTypeName);

        /// <summary>
        /// Gets the number of messages in the data stream.
        /// </summary>
        [PropertyOrder(6)]
        [DisplayName("Message Count")]
        [Description("The number of messages in the stream.")]
        public int? MessageCount => this.StreamMetadata?.MessageCount;

        /// <summary>
        /// Gets the average message size of the data stream.
        /// </summary>
        [PropertyOrder(7)]
        [DisplayName("Average Message Size")]
        [Description("The average size (in bytes) of messages in the stream.")]
        public int? AverageMessageSize => this.StreamMetadata?.AverageMessageSize;

        /// <summary>
        /// Gets the average latency of the data stream.
        /// </summary>
        [PropertyOrder(8)]
        [DisplayName("Average Message Latency")]
        [Description("The average latency of all messages in the stream.")]
        public int? AverageLatency => this.StreamMetadata?.AverageLatency;

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the data stream.
        /// </summary>
        [PropertyOrder(9)]
        [DisplayName("First Message Originating Time")]
        [Description("The originating time of the first message in the stream.")]
        public string FirstMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the first message in the data stream.
        /// </summary>
        [PropertyOrder(10)]
        [DisplayName("First Message Creation Time")]
        [Description("The creation time of the first message in the stream.")]
        public string FirstMessageCreationTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageCreationTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the data stream.
        /// </summary>
        [PropertyOrder(11)]
        [DisplayName("Last Message Originating Time")]
        [Description("The originating time of the last message in the stream.")]
        public string LastMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the last message in the data stream.
        /// </summary>
        [PropertyOrder(12)]
        [DisplayName("Last Message Creation Time")]
        [Description("The creation time of the last message in the stream.")]
        public string LastMessageCreationTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageCreationTime);

        /// <summary>
        /// Gets or sets the type of messages in this stream tree node.
        /// </summary>
        [Browsable(false)]
        public string StreamTypeName { get; protected set; }

        /// <summary>
        /// Gets or sets the type of data represented by this stream tree node.
        /// </summary>
        [Browsable(false)]
        public string NodeTypeName { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this stream is an auto-generated nullable member.
        /// </summary>
        /// <remarks>When stream members are generated for value types, if any of the ancestor
        /// streams are reference type, we generate a corresponding nullable value type for the
        /// stream member (instead of the actual value type). This is done so that when the ancestor
        /// object is null, we can still produce values (in this case null as well) for the member.</remarks>
        [Browsable(false)]
        public bool IsAutoGeneratedNullableMember { get; protected set; }

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
        /// Gets the collection of children for the this stream tree node.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<StreamTreeNode> Children => this.children;

        /// <summary>
        /// Gets the originating time of the first message in the data stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? FirstMessageOriginatingTime => this.StreamMetadata?.FirstMessageOriginatingTime;

        /// <summary>
        /// Gets the time of the first message in the data stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? FirstMessageCreationTime => this.StreamMetadata?.FirstMessageCreationTime;

        /// <summary>
        /// Gets the originating time of the last message in the data stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageOriginatingTime => this.StreamMetadata?.LastMessageOriginatingTime;

        /// <summary>
        /// Gets the time of the last message in the data stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageCreationTime => this.StreamMetadata?.LastMessageCreationTime;

        /// <summary>
        /// Gets a value indicating whether the node represents a stream.
        /// </summary>
        [Browsable(false)]
        public bool IsStream => this.StreamMetadata != null;

        /// <summary>
        /// Gets the partition where this stream tree node can be found.
        /// </summary>
        [Browsable(false)]
        public PartitionViewModel PartitionViewModel { get; private set; }

        /// <summary>
        /// Gets the path of the data stream.
        /// </summary>
        [Browsable(false)]
        public string Path { get; private set; }

        /// <summary>
        /// Gets the stream metadata of the data stream.
        /// </summary>
        [Browsable(false)]
        public IStreamMetadata StreamMetadata { get; private set; }

        /// <summary>
        /// Gets the opacity of the stream tree node. (Opacity is lowered for all nodes in sessions that are not the current session).
        /// </summary>
        [Browsable(false)]
        public double UiElementOpacity => this.PartitionViewModel.SessionViewModel.UiElementOpacity;

        /// <summary>
        /// Gets a value indicating whether this StreamTreeNode can currently be visualized.
        /// </summary>
        [Browsable(false)]
        public bool CanVisualize => this.IsStream && this.PartitionViewModel.SessionViewModel.IsCurrentSession;

        /// <summary>
        /// Gets a value indicating whether the stream's supplemental metadata (if any) is known, readable type.
        /// </summary>
        [Browsable(false)]
        public bool SupplementalMetadataIsKnownType
        {
            get
            {
                // If there is no supplemental metadata for the stream, then everything is fine.
                if (string.IsNullOrWhiteSpace(this.StreamMetadata.SupplementalMetadataTypeName))
                {
                    return true;
                }

                // If we've not tried to read the supplemental metadata yet, do so now.
                if (!this.supplementalMetadataIsKnownType.HasValue)
                {
                    try
                    {
                        // Get the stream source for the stream.
                        StreamSource streamSource = this.PartitionViewModel.SessionViewModel.GetStreamSource(new StreamBinding(this.StreamName, this.PartitionViewModel.Name));

                        // Attempt to read the supplemental metadata for the stream.
                        MethodInfo methodInfo = DataManager.Instance.GetType().GetMethod(nameof(IStreamMetadata.GetSupplementalMetadata), new Type[] { typeof(StreamSource) });
                        MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(TypeResolutionHelper.GetVerifiedType(this.StreamMetadata.SupplementalMetadataTypeName));
                        genericMethodInfo.Invoke(DataManager.Instance, new object[] { streamSource });

                        // Success
                        this.supplementalMetadataIsKnownType = true;
                    }
                    catch (Exception)
                    {
                        this.supplementalMetadataIsKnownType = false;
                    }
                }

                return this.supplementalMetadataIsKnownType.Value;
            }
        }

        /// <summary>
        /// Gets the command that executes when opening the stream tree node context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<StackPanel> ContextMenuOpeningCommand => this.contextMenuOpeningCommand ??= new RelayCommand<StackPanel>(panel => panel.ContextMenu = this.CreateContextMenu());

        /// <summary>
        /// Gets the path to the stream's icon.
        /// </summary>
        [Browsable(false)]
        public virtual string IconSource
        {
            get
            {
                if (this.IsStream)
                {
                    if (VisualizationContext.Instance.GetDataType(this.NodeTypeName)?.Name == nameof(PipelineDiagnostics))
                    {
                        // Diagnostics stream
                        return this.PartitionViewModel.IsLivePartition ? IconSourcePath.DiagnosticsLive : IconSourcePath.Diagnostics;
                    }
                    else if (!string.IsNullOrWhiteSpace(this.MemberPath))
                    {
                        // Stream Member
                        return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamMemberLive : IconSourcePath.StreamMember;
                    }
                    else if (VisualizationContext.Instance.GetDataType(this.NodeTypeName)?.Name == nameof(TimeIntervalAnnotation))
                    {
                        // Annotation
                        return IconSourcePath.Annotation;
                    }
                    else if (this.InternalChildren.Any(c => string.IsNullOrWhiteSpace(c.MemberPath)))
                    {
                        // Group node that's also a stream
                        return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamGroupLive : IconSourcePath.StreamGroup;
                    }
                    else if (VisualizationContext.Instance.GetDataType(this.NodeTypeName) == typeof(AudioBuffer))
                    {
                        // Audio stream
                        return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamAudioMutedLive : IconSourcePath.StreamAudioMuted;
                    }
                    else
                    {
                        // Stream
                        return this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamLive : IconSourcePath.Stream;
                    }
                }
                else
                {
                    // Group node that's not a stream
                    return this.PartitionViewModel.IsLivePartition ? IconSourcePath.GroupLive : IconSourcePath.Group;
                }
            }
        }

        /// <summary>
        /// Gets the color to use when rendering the tree node.
        /// </summary>
        [Browsable(false)]
        public Brush ForegroundBrush
        {
            get
            {
                // If it's a stream member
                if (this.IsStream && !string.IsNullOrWhiteSpace(this.MemberPath))
                {
                    // Show it in gray
                    return new SolidColorBrush(Colors.Gray);
                }
                else
                {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval
        {
            get
            {
                if (this.IsStream)
                {
                    return new TimeInterval(this.FirstMessageOriginatingTime.Value, this.LastMessageOriginatingTime.Value);
                }
                else
                {
                    return TimeInterval.Coverage(
                        this.children
                            .Where(p => p.OriginatingTimeInterval.Left > DateTime.MinValue && p.OriginatingTimeInterval.Right < DateTime.MaxValue)
                            .Select(p => p.OriginatingTimeInterval));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this stream tree node can expand members.
        /// </summary>
        /// <returns>True if the node type has members and is not an auto-generated nullable type, otherwise false.</returns>
        [Browsable(false)]
        public bool CanExpandMembers
        {
            get
            {
                // Get the node type
                Type nodeType = TypeResolutionHelper.GetVerifiedType(this.NodeTypeName);

                // If it's an auto-generated nullable, we need to assess whether the inner value-type (inside the nullable)
                // can expand the members.
                if (this.IsAutoGeneratedNullableMember)
                {
                    nodeType = nodeType.GenericTypeArguments[0];
                }

                if (nodeType != null)
                {
                    return nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => !property.GetMethod.GetParameters().Any()).Any() || nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance).Any();
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this stream tree node has expanded members.
        /// </summary>
        /// <returns>True if the node has expanded members, false otherwise.</returns>
        [Browsable(false)]
        public bool HasExpandedMembers => this.InternalChildren.Any(c => !string.IsNullOrWhiteSpace(c.MemberPath));

        /// <summary>
        /// Gets the internal collection of children for the this stream tree node.
        /// </summary>
        [Browsable(false)]
        protected ObservableCollection<StreamTreeNode> InternalChildren => this.internalChildren;

        /// <summary>
        /// Adds a new store stream tree node based on the specified stream as child of this node.
        /// </summary>
        /// <param name="streamMetadata">The stream to add to the tree.</param>
        /// <returns>A reference to the new stream tree node.</returns>
        public StreamTreeNode AddPath(IStreamMetadata streamMetadata)
        {
            return this.AddPath(streamMetadata.Name.Split('.'), streamMetadata, 1);
        }

        /// <summary>
        /// Recursively searches for a stream with a given name, and if it is found selects it and ensures all parent nodes are expanded.
        /// </summary>
        /// <param name="nodeName">The name of the node to find.</param>
        /// <returns>True if the node was found, otherwise false.</returns>
        public bool SelectNode(string nodeName)
        {
            return this.SelectNode(nodeName.Split('.'), 1);
        }

        /// <summary>
        /// Finds a stream tree node by full name.
        /// </summary>
        /// <param name="streamName">The name of the stream to search for.</param>
        /// <returns>A stream tree node, or null if the stream was not found.</returns>
        public StreamTreeNode FindNode(string streamName)
        {
            return this.FindNode(streamName.Split('.'), 1);
        }

        /// <summary>
        /// Creates all child member nodes for a stream tree node.
        /// </summary>
        public void CreateMemberChildren()
        {
            // check that this nodes have not already been expanded
            if (this.HasExpandedMembers)
            {
                return;
            }

            // Get the type of this node.
            Type nodeType = TypeResolutionHelper.GetVerifiedType(this.NodeTypeName);

            // If this is already an auto-generated nullable, then the type we care to expand is
            // the value-type inside the nullable type.
            if (this.IsAutoGeneratedNullableMember)
            {
                nodeType = nodeType.GenericTypeArguments[0];
            }

            // Determine if the current node is a reference type
            var isReference = this.IsAutoGeneratedNullableMember || !nodeType.IsValueType || Nullable.GetUnderlyingType(nodeType) != null;

            if (nodeType != null)
            {
                // Create a child node for each public instance property that takes no parameters.
                foreach (PropertyInfo propertyInfo in nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => !property.GetMethod.GetParameters().Any()))
                {
                    this.CreateMemberChild(propertyInfo, propertyInfo.PropertyType, isReference);
                }

                // Create a child node for each public instance field
                foreach (FieldInfo fieldInfo in nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    this.CreateMemberChild(fieldInfo, fieldInfo.FieldType, isReference);
                }
            }
        }

        /// <summary>
        /// Ensures that the stream member exists as a child of the stream tree node.
        /// </summary>
        /// <param name="memberPath">The path to the stream member from the stream.</param>
        public void EnsureMemberChildExists(string memberPath)
        {
            this.EnsureMemberChildExists(memberPath.Split('.'), 1);
        }

        /// <summary>
        /// Expands this node and all of its child nodes recursively.
        /// </summary>
        public void ExpandAll()
        {
            foreach (StreamTreeNode child in this.Children)
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

            foreach (StreamTreeNode child in this.Children)
            {
                child.CollapseAll();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Node: " + this.Name;
        }

        /// <summary>
        /// Gets the menu for a stream tree node.
        /// </summary>
        /// <returns>The context menu for the stream tree node.</returns>
        internal ContextMenu CreateContextMenu()
        {
            // Create the context menu
            var contextMenu = new ContextMenu();

            if (this.IsStream)
            {
                var currentPanel = VisualizationContext.Instance.VisualizationContainer.CurrentPanel;

                // sectionStart and sectionEnd keep track of how many items were added in each
                // section of the menu, and can be used to reason about when to add a separator
                var previousIconPath = default(string);

                // get the type-specific visualizers that work in the current panel
                if (currentPanel != null)
                {
                    var specificInCurrentPanel = VisualizationContext.Instance.PluginMap.GetCompatibleVisualizers(
                        streamTreeNode: this,
                        visualizationPanel: currentPanel,
                        isUniversal: false,
                        isInNewPanel: false);
                    foreach (var visualizer in specificInCurrentPanel)
                    {
                        // Create and add a menu item. Currently show icon only if different from previous
                        contextMenu.Items.Add(this.CreateVisualizeStreamMenuItem(visualizer, showIcon: visualizer.IconSourcePath != previousIconPath));
                        previousIconPath = visualizer.IconSourcePath;
                    }
                }

                // get the type specific visualizers in a new panel
                previousIconPath = default;
                var specificInNewPanel = VisualizationContext.Instance.PluginMap.GetCompatibleVisualizers(
                    streamTreeNode: this,
                    visualizationPanel: null,
                    isUniversal: false,
                    isInNewPanel: true);

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
                    var universalInCurrentPanel = VisualizationContext.Instance.PluginMap.GetCompatibleVisualizers(
                        streamTreeNode: this,
                        visualizationPanel: currentPanel,
                        isUniversal: true,
                        isInNewPanel: false);

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
                var universalInNewPanel = VisualizationContext.Instance.PluginMap.GetCompatibleVisualizers(
                    streamTreeNode: this,
                    visualizationPanel: null,
                    isUniversal: true,
                    isInNewPanel: true);

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

                // Add the commands to add subnodes for each public property and field of the current node if the node's type is not simple
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                contextMenu.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        IconSourcePath.StreamMember,
                        ContextMenuName.ExpandMembers,
                        new VisualizationCommand<VisualizerMetadata>((s) =>
                        {
                            this.CreateMemberChildren();
                            this.ExpandAll();
                        }),
                        null,
                        this.CanExpandMembers && !this.HasExpandedMembers));

                // Add the "zoom to stream command"
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.ZoomToStream, ContextMenuName.ZoomToStreamExtents, new VisualizationCommand<VisualizerMetadata>((s) => VisualizationContext.Instance.ZoomToStreamExtents(this))));
            }

            return contextMenu;
        }

        private MenuItem CreateVisualizeStreamMenuItem(VisualizerMetadata metadata, bool showIcon = true)
        {
            return MenuItemHelper.CreateMenuItem(
                showIcon ? metadata.IconSourcePath : string.Empty,
                metadata.CommandText,
                new VisualizationCommand<VisualizerMetadata>((s) => VisualizationContext.Instance.VisualizeStream(this, metadata, VisualizationContext.Instance.VisualizationContainer.CurrentPanel)),
                metadata);
        }

        private StreamTreeNode AddPath(string[] path, IStreamMetadata streamMetadata, int depth)
        {
            var child = this.InternalChildren.FirstOrDefault(p => p.Name == path[depth - 1]) as StreamTreeNode;
            if (child == null)
            {
                child = new StreamTreeNode(this.PartitionViewModel)
                {
                    Path = string.Join(".", path.Take(depth)),
                    Name = path[depth - 1],
                };

                this.InternalChildren.Add(child);
            }

            // if we are at the last segment of the path name then we are at the leaf node
            if (path.Length == depth)
            {
                Debug.Assert(child.StreamMetadata == null, "There should never be two leaf nodes");
                child.StreamMetadata = streamMetadata;
                child.StreamName = streamMetadata.Name;
                child.StreamTypeName = streamMetadata.TypeName;
                child.NodeTypeName = streamMetadata.TypeName;
                return child;
            }

            // we are not at the last segment so recurse in
            return child.AddPath(path, streamMetadata, depth + 1);
        }

        private bool SelectNode(string[] path, int depth)
        {
            StreamTreeNode child = this.InternalChildren.FirstOrDefault(p => p.Name == path[depth - 1]);
            if (child == default)
            {
                return false;
            }

            if (path.Length == depth)
            {
                child.IsTreeNodeSelected = true;
                this.IsTreeNodeExpanded = true;
                return true;
            }
            else
            {
                bool result = child.SelectNode(path, depth + 1);
                if (result)
                {
                    this.IsTreeNodeExpanded = true;
                }

                return result;
            }
        }

        private StreamTreeNode FindNode(string[] path, int depth)
        {
            StreamTreeNode child = this.InternalChildren.FirstOrDefault(p => p.Name == path[depth - 1]);
            if (child == default)
            {
                return null;
            }

            if (path.Length == depth)
            {
                return child;
            }
            else
            {
                return child.FindNode(path, depth + 1);
            }
        }

        private void EnsureMemberChildExists(string[] path, int depth)
        {
            if (this.FindNode(path, depth) == null)
            {
                this.CreateMemberChildren();
            }

            if (path.Length > depth)
            {
                StreamTreeNode memberChild = this.InternalChildren.FirstOrDefault(p => p.Name == path[depth - 1]);
                if (memberChild != null)
                {
                    memberChild.EnsureMemberChildExists(path, depth + 1);
                }
            }
        }

        private StreamTreeNode CreateMemberChild(MemberInfo memberInfo, Type memberType, bool hasReferenceTypeAncestor)
        {
            // The stream tree node type is initialized to the member type
            var nodeTypeName = memberType.AssemblyQualifiedName;

            // If the member type is a struct and we have an ancestor that's a reference type
            var isAutoGeneratedNullableMember = false;
            if (hasReferenceTypeAncestor && memberType.IsValueType)
            {
                // then we need to auto-generate a nullable value member type, instead of the
                // actual member type
                nodeTypeName = typeof(Nullable<>).MakeGenericType(memberType).AssemblyQualifiedName;

                // and also mark is as such in the stream tree node
                isAutoGeneratedNullableMember = true;
            }

            var child = new StreamTreeNode(this.PartitionViewModel)
            {
                Path = string.IsNullOrWhiteSpace(this.Path) ? memberInfo.Name : $"{this.Path}.{memberInfo.Name}",
                MemberPath = string.IsNullOrWhiteSpace(this.MemberPath) ? memberInfo.Name : $"{this.MemberPath}.{memberInfo.Name}",
                Name = memberInfo.Name,
                StreamMetadata = this.StreamMetadata,
                StreamTypeName = this.StreamMetadata.TypeName,
                NodeTypeName = nodeTypeName,
                StreamName = this.StreamMetadata.Name,
                IsAutoGeneratedNullableMember = isAutoGeneratedNullableMember,
            };

            // Insert the child into the existing list, before all non-member sub-streams, and in alphabetical order
            var lastOrDefault = this.InternalChildren.LastOrDefault(stn => string.Compare(stn.Name, memberInfo.Name) < 0 && !string.IsNullOrEmpty(stn.MemberPath));
            var index = lastOrDefault != null ? this.InternalChildren.IndexOf(lastOrDefault) + 1 : 0;
            this.InternalChildren.Insert(index, child);
            return child;
        }

        private void Partition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.PartitionViewModel.IsLivePartition))
            {
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        private void DatasetViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.PartitionViewModel.SessionViewModel.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
                this.RaisePropertyChanged(nameof(this.CanVisualize));
            }
        }
    }
}
