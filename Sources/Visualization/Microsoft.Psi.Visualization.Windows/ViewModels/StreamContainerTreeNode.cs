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
    using System.Windows.Controls;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Implements a node in the dataset tree that represents a stream container.
    /// </summary>
    /// <remarks>
    /// This class also acts as the base class for the hierarchy of stream tree nodes.
    /// The <see cref="StreamTreeNode"/> class models a regular tree node corresponding
    /// to a stream in the partition, whereas other classes specialize these further to
    /// represent derived streams, such as <see cref="DerivedMemberStreamTreeNode"/>
    /// and <see cref="DerivedReceiverDiagnosticsStreamTreeNode{T}"/>.
    /// </remarks>
    public class StreamContainerTreeNode : ObservableTreeNodeObject
    {
        private readonly ObservableCollection<StreamContainerTreeNode> internalChildren;
        private readonly ReadOnlyObservableCollection<StreamContainerTreeNode> children;

        private string auxiliaryInfo = string.Empty;

        private RelayCommand<Grid> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamContainerTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the container tree node.</param>
        /// <param name="path">The path to the container tree node.</param>
        /// <param name="name">The name of the container tree node.</param>
        public StreamContainerTreeNode(PartitionViewModel partitionViewModel, string path, string name)
        {
            this.PartitionViewModel = partitionViewModel;
            this.PartitionViewModel.PropertyChanged += this.OnPartitionViewModelPropertyChanged;
            this.DatasetViewModel.PropertyChanged += this.OnDatasetViewModelPropertyChanged;

            this.Path = path;
            this.Name = name;

            this.internalChildren = new ObservableCollection<StreamContainerTreeNode>();
            this.children = new ReadOnlyObservableCollection<StreamContainerTreeNode>(this.internalChildren);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="StreamContainerTreeNode"/> class.
        /// </summary>
        ~StreamContainerTreeNode()
        {
            this.PartitionViewModel.PropertyChanged -= this.OnPartitionViewModelPropertyChanged;
            this.DatasetViewModel.PropertyChanged -= this.OnDatasetViewModelPropertyChanged;
        }

        /// <summary>
        /// Gets the dataset where this stream tree node can be found.
        /// </summary>
        [Browsable(false)]
        public DatasetViewModel DatasetViewModel => this.PartitionViewModel.SessionViewModel.DatasetViewModel;

        /// <summary>
        /// Gets the session where this stream tree node can be found.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel SessionViewModel => this.PartitionViewModel.SessionViewModel;

        /// <summary>
        /// Gets the partition where this stream tree node can be found.
        /// </summary>
        [Browsable(false)]
        public PartitionViewModel PartitionViewModel { get; private set; }

        /// <summary>
        /// Gets the path of the stream tree node.
        /// </summary>
        [Browsable(false)]
        public string Path { get; private set; }

        /// <summary>
        /// Gets the name of the stream tree node.
        /// </summary>
        [Browsable(false)]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the collection of children for this stream tree node.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<StreamContainerTreeNode> Children => this.children;

        /// <summary>
        /// Gets the time interval of the stream(s) subsumed by this stream container tree node.
        /// </summary>
        [Browsable(false)]
        public TimeInterval SubsumedTimeInterval
            => new TimeInterval(this.SubsumedOpenedTime, this.SubsumedClosedTime);

        /// <summary>
        /// Gets the command that executes when opening the stream tree node context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<Grid> ContextMenuOpeningCommand =>
            this.contextMenuOpeningCommand ??= new RelayCommand<Grid>(
                grid =>
                {
                    var contextMenu = new ContextMenu();
                    this.PopulateContextMenu(contextMenu);
                    grid.ContextMenu = contextMenu;
                });

        /// <summary>
        /// Gets the name to display in the stream tree.
        /// </summary>
        [Browsable(false)]
        public virtual string DisplayName => this.Name;

        /// <summary>
        /// Gets or sets the auxiliary info to display.
        /// </summary>
        [Browsable(false)]
        public virtual string AuxiliaryInfo
        {
            get => this.auxiliaryInfo;
            protected set => this.Set(nameof(this.AuxiliaryInfo), ref this.auxiliaryInfo, value);
        }

        /// <summary>
        /// Gets a value indicating whether this node is in the current session.
        /// </summary>
        [Browsable(false)]
        public bool IsInCurrentSession => this.SessionViewModel.IsCurrentSession;

        /// <summary>
        /// Gets the path to the stream's icon.
        /// </summary>
        [Browsable(false)]
        public virtual string IconSource => this.PartitionViewModel.IsLivePartition ? IconSourcePath.GroupLive : IconSourcePath.Group;

        /// <summary>
        /// Gets the opacity of the stream tree node. (Opacity is lowered for all nodes in sessions that are not the current session).
        /// </summary>
        [Browsable(false)]
        public virtual double UiElementOpacity => this.SessionViewModel.UiElementOpacity;

        /// <summary>
        /// Gets the color to use when rendering the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual Brush ForegroundBrush => new SolidColorBrush(Colors.White);

        /// <summary>
        /// Gets a value indicating whether this container has any non-derived children.
        /// </summary>
        [Browsable(false)]
        public bool HasNonDerivedChildren => this.children.Any(c => c is not DerivedStreamTreeNode);

        /// <summary>
        /// Gets the first opened time for the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SubsumedOpenedTime
        {
            get
            {
                var available = this.children.Where(c => c is not DerivedStreamTreeNode && c.SubsumedOpenedTime != DateTime.MinValue);
                return available.Any() ? available.Min(c => c.SubsumedOpenedTime) : DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the last closed time for the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SubsumedClosedTime
        {
            get
            {
                var available = this.children.Where(c => c is not DerivedStreamTreeNode && c.SubsumedClosedTime != DateTime.MaxValue);
                return available.Any() ? available.Max(c => c.SubsumedClosedTime) : DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Gets the originating time interval for the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual TimeInterval SubsumedOriginatingTimeInterval =>
            new TimeInterval(this.SubsumedFirstMessageOriginatingTime, this.SubsumedLastMessageOriginatingTime);

        /// <summary>
        /// Gets the originating time of the first message in the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SubsumedFirstMessageOriginatingTime
        {
            get
            {
                var available = this.children.Where(c => c is not DerivedStreamTreeNode && c.SubsumedFirstMessageOriginatingTime != DateTime.MinValue);
                return available.Any() ? available.Min(c => c.SubsumedFirstMessageOriginatingTime) : DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the creation time of the first message in the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SubsumedFirstMessageCreationTime
        {
            get
            {
                var available = this.children.Where(c => c is not DerivedStreamTreeNode && c.SubsumedFirstMessageCreationTime != DateTime.MinValue);
                return available.Any() ? available.Min(c => c.SubsumedFirstMessageCreationTime) : DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the originating time of the last message in the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SubsumedLastMessageOriginatingTime
        {
            get
            {
                var available = this.children.Where(c => c is not DerivedStreamTreeNode && c.SubsumedLastMessageOriginatingTime != DateTime.MaxValue);
                return available.Any() ? available.Max(c => c.SubsumedLastMessageOriginatingTime) : DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Gets the creation time of the last message in the stream(s) subsumed by the tree node.
        /// </summary>
        [Browsable(false)]
        public virtual DateTime SubsumedLastMessageCreationTime
        {
            get
            {
                var available = this.children.Where(c => c is not DerivedStreamTreeNode && c.SubsumedLastMessageCreationTime != DateTime.MaxValue);
                return available.Any() ? available.Max(c => c.SubsumedLastMessageCreationTime) : DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Gets the total number of messages in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed Message Count")]
        [Description("The total number of messages in the stream(s) subsumed by the tree node.")]
        public virtual long SubsumedMessageCount
            => this.children.Where(c => c is not DerivedStreamTreeNode).Sum(c => c.SubsumedMessageCount);

        /// <summary>
        /// Gets the total number of messages in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed Avg. Message Latency (ms)")]
        [Description("The average latency (in milliseconds) of messages in the stream(s) subsumed by the tree node.")]
        public virtual double SubsumedAverageMessageLatencyMs
            => this.children.Where(c => c is not DerivedStreamTreeNode).Sum(c => c.SubsumedMessageCount * c.SubsumedAverageMessageLatencyMs) / this.SubsumedMessageCount;

        /// <summary>
        /// Gets the total number of messages in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed Avg. Message Size")]
        [Description("The average size (in bytes) of messages in the stream(s) subsumed by the tree node.")]
        public virtual double SubsumedAverageMessageSize
            => this.children.Where(c => c is not DerivedStreamTreeNode).Sum(c => c.SubsumedMessageCount * c.SubsumedAverageMessageSize) / this.SubsumedMessageCount;

        /// <summary>
        /// Gets the total data size in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed Size")]
        [Description("The size (in bytes) of data in the stream(s) subsumed by the tree node.")]
        public virtual long SubsumedSize
            => this.children.Where(c => c is not DerivedStreamTreeNode).Sum(c => c.SubsumedSize);

        /// <summary>
        /// Gets a string representation of the first opened time for the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed OpenedTime")]
        [Description("The first opened time for the stream(s) subsumed by the tree node.")]
        public virtual string SubsumedOpenedTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SubsumedOpenedTime);

        /// <summary>
        /// Gets a string representation of the last closed time for the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed ClosedTime")]
        [Description("The last closed time for the stream(s) subsumed by the tree node.")]
        public virtual string SubsumedClosedTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SubsumedClosedTime);

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed First Message OriginatingTime")]
        [Description("The originating time of the first message in the stream(s) subsumed by the tree node.")]
        public virtual string SubsumedFirstMessageOriginatingTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SubsumedFirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the first message in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed First Message CreationTime")]
        [Description("The creation time of the first message in the stream(s) subsumed by the tree node.")]
        public virtual string SubsumedFirstMessageCreationTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SubsumedFirstMessageCreationTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed Last Message OriginatingTime")]
        [Description("The originating time of the last message in the stream(s) subsumed by the tree node.")]
        public virtual string SubsumedLastMessageOriginatingTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SubsumedLastMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the last message in the stream(s) subsumed by the tree node.
        /// </summary>
        [DisplayName("Subsumed Last Message CreationTime")]
        [Description("The creation time of the last message in the stream(s) subsumed by the tree node.")]
        public virtual string SubsumedLastMessageCreationTimeString
            => DateTimeFormatHelper.FormatDateTime(this.SubsumedLastMessageCreationTime);

        /// <summary>
        /// Gets the internal collection of children for the this stream tree node.
        /// </summary>
        [Browsable(false)]
        protected ObservableCollection<StreamContainerTreeNode> InternalChildren => this.internalChildren;

        /// <summary>
        /// Adds a new stream tree node based on the specified stream as child of this node.
        /// </summary>
        /// <param name="streamMetadata">The stream to add to the tree.</param>
        /// <returns>A reference to the new stream tree node.</returns>
        public StreamTreeNode AddStreamTreeNode(IStreamMetadata streamMetadata) =>
            this.AddStreamTreeNode(streamMetadata.Name.Split('.'), streamMetadata);

        /// <summary>
        /// Adds a new container tree node to the set of children.
        /// </summary>
        /// <param name="treeNode">The container tree node to add.</param>
        public void AddChildTreeNode(StreamContainerTreeNode treeNode) =>
            this.InternalChildren.Add(treeNode);

        /// <summary>
        /// Selects a tree node and expands all nodes on the path to it.
        /// </summary>
        /// <param name="nodePath">The path of the node to select.</param>
        /// <returns>True if the node was found, otherwise false.</returns>
        public bool SelectTreeNode(string nodePath) =>
            this.SelectNode(nodePath.Split('.'));

        /// <summary>
        /// Finds a stream tree node by stream name.
        /// </summary>
        /// <param name="streamName">The name of the stream to search for.</param>
        /// <returns>A stream tree node, or null if the stream was not found.</returns>
        public StreamTreeNode FindStreamTreeNode(string streamName) =>
            this.FindStreamContainerTreeNode(streamName.Split('.')) as StreamTreeNode;

        /// <summary>
        /// Finds a stream container tree node by full path.
        /// </summary>
        /// <param name="path">The path to the node.</param>
        /// <returns>A stream container tree node, or null if the node was not found.</returns>
        public StreamContainerTreeNode FindStreamContainerTreeNode(string path) =>
            this.FindStreamContainerTreeNode(path.Split('.'));

        /// <summary>
        /// Expands this node and all of its child nodes recursively.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var child in this.children)
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

            foreach (var child in this.children)
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
        protected virtual void OnPartitionViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
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
        protected virtual void OnDatasetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
            }
            else if (e.PropertyName == nameof(this.DatasetViewModel.ShowAuxiliaryStreamInfo))
            {
                this.UpdateAuxiliaryInfo();
            }
        }

        /// <summary>
        /// Updates the auxiliary info to be displayed.
        /// </summary>
        protected virtual void UpdateAuxiliaryInfo()
        {
            switch (this.DatasetViewModel.ShowAuxiliaryStreamInfo)
            {
                case AuxiliaryStreamInfo.None:
                    this.AuxiliaryInfo = string.Empty;
                    break;
                case AuxiliaryStreamInfo.Size:
                    this.AuxiliaryInfo = $"[{SizeFormatHelper.FormatSize(this.SubsumedSize)}]";
                    break;
                case AuxiliaryStreamInfo.MessageCount:
                    this.AuxiliaryInfo = this.SubsumedMessageCount == 0 ? "[0]" : $"[{this.SubsumedMessageCount:0,0}]";
                    break;
                case AuxiliaryStreamInfo.AverageMessageLatencyMs:
                    this.AuxiliaryInfo = this.SubsumedAverageMessageLatencyMs < 1 ? "<1 ms" : $"{this.SubsumedAverageMessageLatencyMs:0,0 ms}";
                    break;
                case AuxiliaryStreamInfo.AverageMessageSize:
                    this.AuxiliaryInfo = $"[{SizeFormatHelper.FormatSize((long)this.SubsumedAverageMessageSize)}]";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Populates a context menu with items for this tree node.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        protected virtual void PopulateContextMenu(ContextMenu contextMenu)
        {
            this.PopulateContextMenuWithExpandAndCollapseAll(contextMenu);
            this.PopulateContextMenuWithShowStreamInfo(contextMenu);
        }

        /// <summary>
        /// Populates a context menu with the commands for showing stream info.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        protected void PopulateContextMenuWithShowStreamInfo(ContextMenu contextMenu)
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
        /// Populates a context menu with the expand and collapse all items.
        /// </summary>
        /// <param name="contextMenu">The context menu to populate.</param>
        protected void PopulateContextMenuWithExpandAndCollapseAll(ContextMenu contextMenu)
        {
            if (!this.InternalChildren.Any())
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
                    isEnabled: this.InternalChildren.Any()));

            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.CollapseAllNodes,
                    ContextMenuName.CollapseAllNodes,
                    new VisualizationCommand(() => this.CollapseAll()),
                    isEnabled: this.InternalChildren.Any()));
        }

        /// <summary>
        /// Creates a stream tree node.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the stream tree node.</param>
        /// <param name="path">The path to the stream tree node.</param>
        /// <param name="name">The name of the stream tree node.</param>
        /// <param name="streamMetadata">The stream metadata.</param>
        /// <returns>The stream tree node.</returns>
        private StreamTreeNode CreateStreamTreeNode(PartitionViewModel partitionViewModel, string path, string name, IStreamMetadata streamMetadata)
        {
            var dataType = VisualizationContext.Instance.GetDataType(streamMetadata.TypeName);
            if (dataType == typeof(PipelineDiagnostics))
            {
                return new PipelineDiagnosticsStreamTreeNode(partitionViewModel, path, name, streamMetadata);
            }
            else
            {
                return new StreamTreeNode(partitionViewModel, path, name, streamMetadata);
            }
        }

        /// <summary>
        /// Helper method that adds a stream tree node to the container tree node, recursively.
        /// </summary>
        /// <param name="path">The path to the stream tree node.</param>
        /// <param name="streamMetadata">The stream metadata.</param>
        /// <returns>The resulting stream tree node.</returns>
        private StreamTreeNode AddStreamTreeNode(IEnumerable<string> path, IStreamMetadata streamMetadata)
        {
            var firstPathElement = path.First();

            if (this.InternalChildren.FirstOrDefault(p => p.Name == firstPathElement) is not StreamContainerTreeNode streamContainerTreeNode)
            {
                streamContainerTreeNode = new StreamContainerTreeNode(this.PartitionViewModel, this.Path == null ? firstPathElement : $"{this.Path}.{firstPathElement}", firstPathElement);
                this.InternalChildren.Add(streamContainerTreeNode);
            }

            // if we are at the last segment of the path name then we are at the leaf node
            if (path.Count() == 1)
            {
                Debug.Assert(streamContainerTreeNode is not StreamTreeNode, "There should never be two leaf nodes");

                // find the index of the streamContainer, remove it, and add a stream tree node at that index
                var index = this.InternalChildren.IndexOf(streamContainerTreeNode);
                this.InternalChildren.Remove(streamContainerTreeNode);

                var streamTreeNode = this.CreateStreamTreeNode(this.PartitionViewModel, streamContainerTreeNode.Path, streamContainerTreeNode.Name, streamMetadata);

                // add any children the previous container might have had to the stream tree node
                foreach (var child in streamContainerTreeNode.Children)
                {
                    streamTreeNode.AddChildTreeNode(child);
                }

                this.InternalChildren.Insert(index, streamTreeNode);

                return streamTreeNode;
            }

            // we are not at the last segment so recurse in
            return streamContainerTreeNode.AddStreamTreeNode(path.Skip(1), streamMetadata);
        }

        /// <summary>
        /// Recursively selects a node specified by a path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>True if the node was found.</returns>
        private bool SelectNode(IEnumerable<string> path)
        {
            var firstPathElement = path.First();
            var child = this.InternalChildren.FirstOrDefault(p => p.Name == firstPathElement);
            if (child == default)
            {
                return false;
            }

            if (path.Count() == 1)
            {
                child.IsTreeNodeSelected = true;
                this.IsTreeNodeExpanded = true;
                return true;
            }
            else
            {
                bool result = child.SelectNode(path.Skip(1));
                if (result)
                {
                    this.IsTreeNodeExpanded = true;
                }

                return result;
            }
        }

        /// <summary>
        /// Finds a stream container tree node specified by a path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The corresponding stream container tree node.</returns>
        private StreamContainerTreeNode FindStreamContainerTreeNode(IEnumerable<string> path)
        {
            var firstPathElement = path.First();
            var child = this.InternalChildren.FirstOrDefault(p => p.Name == firstPathElement);
            if (child == default)
            {
                return null;
            }

            if (path.Count() == 1)
            {
                return child as StreamContainerTreeNode;
            }
            else
            {
                return child.FindStreamContainerTreeNode(path.Skip(1));
            }
        }
    }
}
