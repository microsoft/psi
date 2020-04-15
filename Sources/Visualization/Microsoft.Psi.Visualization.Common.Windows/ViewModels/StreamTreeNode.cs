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
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Defines a base class for nodes in a tree that hold information about data streams.
    /// </summary>
    public class StreamTreeNode : ObservableTreeNodeObject
    {
        private ObservableCollection<StreamTreeNode> internalChildren;
        private ReadOnlyObservableCollection<StreamTreeNode> children;
        private RelayCommand<StackPanel> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        /// <param name="partition">The partition where this stream tree node can be found.</param>
        public StreamTreeNode(PartitionViewModel partition)
        {
            this.Partition = partition;
            this.Partition.PropertyChanged += this.Partition_PropertyChanged;
            this.Partition.SessionViewModel.DatasetViewModel.PropertyChanged += this.DatasetViewModel_PropertyChanged;
            this.internalChildren = new ObservableCollection<StreamTreeNode>();
            this.children = new ReadOnlyObservableCollection<StreamTreeNode>(this.internalChildren);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        ~StreamTreeNode()
        {
            this.Partition.PropertyChanged -= this.Partition_PropertyChanged;
            this.Partition.SessionViewModel.DatasetViewModel.PropertyChanged -= this.DatasetViewModel_PropertyChanged;
        }

        /// <summary>
        /// Gets the id of the data stream.
        /// </summary>
        [PropertyOrder(0)]
        [Description("The ID of the stream within the Psi Store")]
        public int? Id => this.StreamMetadata?.Id;

        /// <summary>
        /// Gets or sets the name of this stream tree node.
        /// </summary>
        [PropertyOrder(1)]
        [DisplayName("ShortName")]
        [Description("The name of the stream.")]
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the stream name of this stream tree node.
        /// </summary>
        [PropertyOrder(2)]
        [DisplayName("FullName")]
        [Description("The full name of this stream including path information")]
        public string StreamName { get; protected set; }

        /// <summary>
        /// Gets the number of messages in the data stream.
        /// </summary>
        [PropertyOrder(3)]
        [Description("The number of messages in the stream.")]
        public int? MessageCount => this.StreamMetadata?.MessageCount;

        /// <summary>
        /// Gets the average message size of the data stream.
        /// </summary>
        [PropertyOrder(4)]
        [Description("The average size (in bytes) of messages in the stream.")]
        public int? AverageMessageSize => this.StreamMetadata?.AverageMessageSize;

        /// <summary>
        /// Gets the average latency of the data stream.
        /// </summary>
        [PropertyOrder(5)]
        [Description("The average latency of all messages in the stream.")]
        public int? AverageLatency => this.StreamMetadata?.AverageLatency;

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the data stream.
        /// </summary>
        [PropertyOrder(6)]
        [DisplayName("FirstMessageOriginatingTime")]
        [Description("The originating time of the first message in the stream.")]
        public string FirstMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the first message in the data stream.
        /// </summary>
        [PropertyOrder(7)]
        [DisplayName("FirstMessageTime")]
        [Description("The time of the first message in the stream.")]
        public string FirstMessageTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the data stream.
        /// </summary>
        [PropertyOrder(8)]
        [DisplayName("LastMessageOriginatingTime")]
        [Description("The originating time of the last message in the stream.")]
        public string LastMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the time of the last message in the data stream.
        /// </summary>
        [PropertyOrder(9)]
        [DisplayName("LastMessageTime")]
        [Description("The time of the last message in the stream.")]
        public string LastMessageTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageTime);

        /// <summary>
        /// Gets or sets the type of data of this stream tree node.
        /// </summary>
        [PropertyOrder(10)]
        [DisplayName("MessageType")]
        [Description("The type of messages in the stream.")]
        public string TypeName { get; protected set; }

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
        public DateTime? FirstMessageTime => this.StreamMetadata?.FirstMessageTime;

        /// <summary>
        /// Gets the originating time of the last message in the data stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageOriginatingTime => this.StreamMetadata?.LastMessageOriginatingTime;

        /// <summary>
        /// Gets the time of the last message in the data stream.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageTime => this.StreamMetadata?.LastMessageTime;

        /// <summary>
        /// Gets a value indicating whether the node represents a stream.
        /// </summary>
        [Browsable(false)]
        public bool IsStream => this.StreamMetadata != null;

        /// <summary>
        /// Gets the partition where this stream tree node can be found.
        /// </summary>
        [Browsable(false)]
        public PartitionViewModel Partition { get; private set; }

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
        public double UiElementOpacity => this.Partition.SessionViewModel.UiElementOpacity;

        /// <summary>
        /// Gets a value indicating whether this StreamTreeNode can currently be visualized.
        /// </summary>
        [Browsable(false)]
        public bool CanVisualize => this.IsStream && this.Partition.SessionViewModel.IsCurrentSession;

        /// <summary>
        /// Gets a value indicating whether this StreamTreeNode should display a context menu.
        /// </summary>
        [Browsable(false)]
        public bool CanShowContextMenu
        {
            get
            {
                // Show the context menu if:
                //  a) This node is a stream, and
                //  b) This node is within the session currently being visualized, and
                //  c) This node has some context menu items
                if (this.CanVisualize)
                {
                    var commands = VisualizationContext.Instance.GetDatasetStreamMenu(this);
                    if (commands != null && commands.Items.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the command that executes when the context menu for the stream tree node opens.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<StackPanel> ContextMenuOpeningCommand
        {
            get
            {
                if (this.contextMenuOpeningCommand == null)
                {
                    this.contextMenuOpeningCommand = new RelayCommand<StackPanel>(e => this.UpdateContextMenu(e));
                }

                return this.contextMenuOpeningCommand;
            }
        }

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
                    if (VisualizationContext.Instance.GetStreamType(this)?.Name == nameof(PipelineDiagnostics))
                    {
                        return this.Partition.IsLivePartition ? IconSourcePath.DiagnosticsLive : IconSourcePath.Diagnostics;
                    }
                    else if (this.InternalChildren.Count > 0)
                    {
                        return this.Partition.IsLivePartition ? IconSourcePath.StreamGroupLive : IconSourcePath.StreamGroup;
                    }
                    else if (VisualizationContext.Instance.GetStreamType(this) == typeof(AudioBuffer))
                    {
                        return this.Partition.IsLivePartition ? IconSourcePath.StreamAudioMutedLive : IconSourcePath.StreamAudioMuted;
                    }
                    else
                    {
                        return this.Partition.IsLivePartition ? IconSourcePath.StreamLive : IconSourcePath.Stream;
                    }
                }
                else
                {
                    return this.Partition.IsLivePartition ? IconSourcePath.GroupLive : IconSourcePath.Group;
                }
            }
        }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this session.
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
        /// <param name="streamName">The name of the stream to find.</param>
        /// <returns>A stream tree node, or null if the stream was not found.</returns>
        public StreamTreeNode SelectNode(string streamName)
        {
            if (this.StreamName == streamName)
            {
                this.IsTreeNodeSelected = true;
                return this;
            }

            foreach (StreamTreeNode streamTreeNode in this.Children)
            {
                StreamTreeNode foundStream = streamTreeNode.SelectNode(streamName);
                if (foundStream != null)
                {
                    this.IsTreeNodeExpanded = true;
                    return foundStream;
                }
            }

            return null;
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

        private StreamTreeNode AddPath(string[] path, IStreamMetadata streamMetadata, int depth)
        {
            var child = this.InternalChildren.FirstOrDefault(p => p.Name == path[depth - 1]) as StreamTreeNode;
            if (child == null)
            {
                child = new StreamTreeNode(this.Partition)
                {
                    Path = string.Join(".", path.Take(depth)),
                    Name = path[depth - 1],
                };
                this.InternalChildren.Add(child);
            }

            // if we are at the last segement of the path name then we are at the leaf node
            if (path.Length == depth)
            {
                Debug.Assert(child.StreamMetadata == null, "There should never be two leaf nodes");
                child.StreamMetadata = streamMetadata;
                child.TypeName = streamMetadata.TypeName;
                child.StreamName = streamMetadata.Name;
                return child;
            }

            // we are not at the last segment so recurse in
            return child.AddPath(path, streamMetadata, depth + 1);
        }

        private void UpdateContextMenu(StackPanel panel)
        {
            VisualizationPanel currentPanel = VisualizationContext.Instance.VisualizationContainer.CurrentPanel;
            List<VisualizationPanelType> compatiblePanelTypes = VisualizationContext.Instance.VisualizerMap.GetCompatiblePanelTypes(currentPanel);

            foreach (object item in panel.ContextMenu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    // Enable/disable all "visualize in current panel" menuitems based on the current visualization panel in the container
                    if (menuItem.Tag is VisualizerMetadata visualizerMetadata && visualizerMetadata.IsInNewPanel == false)
                    {
                        menuItem.Visibility = compatiblePanelTypes.Contains(visualizerMetadata.VisualizationPanelType) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        private void Partition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Partition.IsLivePartition))
            {
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        private void DatasetViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Partition.SessionViewModel.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
                this.RaisePropertyChanged(nameof(this.CanVisualize));
                this.RaisePropertyChanged(nameof(this.CanShowContextMenu));
            }
        }
    }
}
