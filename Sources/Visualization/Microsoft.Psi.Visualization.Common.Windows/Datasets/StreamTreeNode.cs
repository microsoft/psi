// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Datasets
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Defines a base class for nodes in a tree that hold information about data streams.
    /// </summary>
    public class StreamTreeNode : IStreamTreeNode
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
        /// Gets the internal collection of children for the this stream tree node.
        /// </summary>
        [Browsable(false)]
        protected ObservableCollection<IStreamTreeNode> InternalChildren => this.internalChildren;

        /// <inheritdoc />
        public void AddPath(IStreamMetadata streamMetadata)
        {
            this.AddPath(streamMetadata.Name.Split('.'), streamMetadata, 1);
        }

        private void AddPath(string[] path, IStreamMetadata streamMetadata, int depth)
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
                return;
            }

            // we are not at the last segment so recurse in
            child.AddPath(path, streamMetadata, depth + 1);
        }
    }
}
