// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Media;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Common;

    /// <summary>
    /// Defines a base class for nodes in a tree that hold information about data streams.
    /// </summary>
    public class StreamTreeNode : ObservableObject, IStreamTreeNode
    {
        private ObservableCollection<IStreamTreeNode> internalChildren;
        private ReadOnlyObservableCollection<IStreamTreeNode> children;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTreeNode"/> class.
        /// </summary>
        /// <param name="partition">The partition where this stream tree node can be found.</param>
        public StreamTreeNode(PartitionViewModel partition)
        {
            this.Partition = partition;
            this.Partition.SessionViewModel.DatasetViewModel.PropertyChanged += this.DatasetViewModel_PropertyChanged;
            this.internalChildren = new ObservableCollection<IStreamTreeNode>();
            this.children = new ReadOnlyObservableCollection<IStreamTreeNode>(this.internalChildren);
        }

        /// <inheritdoc />
        public int? AverageLatency => this.StreamMetadata?.AverageLatency;

        /// <inheritdoc />
        public int? AverageMessageSize => this.StreamMetadata?.AverageMessageSize;

        /// <inheritdoc />
        [Browsable(false)]
        public ReadOnlyObservableCollection<IStreamTreeNode> Children => this.children;

        /// <inheritdoc />
        public int? Id => this.StreamMetadata?.Id;

        /// <inheritdoc />
        public DateTime? FirstMessageOriginatingTime => this.StreamMetadata?.FirstMessageOriginatingTime;

        /// <inheritdoc />
        public DateTime? FirstMessageTime => this.StreamMetadata?.FirstMessageTime;

        /// <inheritdoc />
        public DateTime? LastMessageOriginatingTime => this.StreamMetadata?.LastMessageOriginatingTime;

        /// <inheritdoc />
        public DateTime? LastMessageTime => this.StreamMetadata?.LastMessageTime;

        /// <inheritdoc />
        public int? MessageCount => this.StreamMetadata?.MessageCount;

        /// <inheritdoc />
        public bool IsStream => this.StreamMetadata != null;

        /// <inheritdoc />
        public string Name { get; protected set; }

        /// <inheritdoc />
        [Browsable(false)]
        public PartitionViewModel Partition { get; private set; }

        /// <inheritdoc />
        public string Path { get; private set; }

        /// <inheritdoc />
        [Browsable(false)]
        public IStreamMetadata StreamMetadata { get; private set; }

        /// <inheritdoc />
        public string StreamName { get; protected set; }

        /// <inheritdoc />
        public string TypeName { get; protected set; }

        /// <summary>
        /// Gets the brush for drawing text
        /// </summary>
        public Brush TextBrush => this.Partition.SessionViewModel.DatasetViewModel.CurrentSessionViewModel == this.Partition.SessionViewModel ? ViewModelBrushes.SelectedBrush : ViewModelBrushes.StandardBrush;

        /// <inheritdoc/>
        public bool CanVisualize => this.IsStream && this.Partition.SessionViewModel.IsCurrentSession;

        /// <inheritdoc/>
        public bool CanShowContextMenu
        {
            get
            {
                // Show the context menu if:
                //  a) This node is a stream, and
                //  b) This node is withing the session currently being visualized, and
                //  c) This node has some context menu items
                if (this.CanVisualize)
                {
                    var commands = PsiStudioContext.Instance.GetVisualizeStreamCommands(this);
                    if (commands != null && commands.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the path to the stream's icon
        /// </summary>
        [Browsable(false)]
        public virtual string IconSource
        {
            get
            {
                if (this.IsStream)
                {
                    if (PsiStudioContext.Instance.GetStreamType(this) == typeof(AudioBuffer))
                    {
                        return IconSourcePath.AudioMuted;
                    }
                    else
                    {
                        return IconSourcePath.Stream;
                    }
                }
                else
                {
                    return IconSourcePath.BlankIcon;
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
        protected ObservableCollection<IStreamTreeNode> InternalChildren => this.internalChildren;

        /// <inheritdoc />
        public IStreamTreeNode AddPath(IStreamMetadata streamMetadata)
        {
            return this.AddPath(streamMetadata.Name.Split('.'), streamMetadata, 1);
        }

        private IStreamTreeNode AddPath(string[] path, IStreamMetadata streamMetadata, int depth)
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

        private void DatasetViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Partition.SessionViewModel.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.TextBrush));
                this.RaisePropertyChanged(nameof(this.CanVisualize));
                this.RaisePropertyChanged(nameof(this.CanShowContextMenu));
            }
        }
    }
}
