// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Defines a node in a tree that holds information about data streams.
    /// </summary>
    public interface IStreamTreeNode
    {
        /// <summary>
        /// Gets the average latency of the data stream.
        /// </summary>
        int? AverageLatency { get; }

        /// <summary>
        /// Gets the average message size of the data stream.
        /// </summary>
        int? AverageMessageSize { get; }

        /// <summary>
        /// Gets the collection of children for the this stream tree node.
        /// </summary>
        ReadOnlyObservableCollection<IStreamTreeNode> Children { get; }

        /// <summary>
        /// Gets the id of the data stream.
        /// </summary>
        int? Id { get; }

        /// <summary>
        /// Gets the originating time of the first message in the data stream.
        /// </summary>
        DateTime? FirstMessageOriginatingTime { get; }

        /// <summary>
        /// Gets the time of the first message in the data stream.
        /// </summary>
        DateTime? FirstMessageTime { get; }

        /// <summary>
        /// Gets the originating time of the last message in the data stream.
        /// </summary>
        DateTime? LastMessageOriginatingTime { get; }

        /// <summary>
        /// Gets the time of the last message in the data stream.
        /// </summary>
        DateTime? LastMessageTime { get; }

        /// <summary>
        /// Gets the originating time interval.
        /// </summary>
        TimeInterval OriginatingTimeInterval { get; }

        /// <summary>
        /// Gets the number of messages in the data stream.
        /// </summary>
        int? MessageCount { get; }

        /// <summary>
        /// Gets a value indicating whether the node represents a stream.
        /// </summary>
        bool IsStream { get; }

        /// <summary>
        /// Gets the name of this stream tree node.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the partition where this stream tree node can be found.
        /// </summary>
        PartitionViewModel Partition { get; }

        /// <summary>
        /// Gets the path of the data stream.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the stream metadata of the data stream.
        /// </summary>
        IStreamMetadata StreamMetadata { get; }

        /// <summary>
        /// Gets the stream name of this stream tree node.
        /// </summary>
        string StreamName { get; }

        /// <summary>
        /// Gets the type of data of this stream tree node.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets a value indicating whether this IStreamTreeNode can currently be visualized.
        /// </summary>
        bool CanVisualize { get; }

        /// <summary>
        /// Gets a value indicating whether this IStreamTreeNode should display a context menu.
        /// </summary>
        bool CanShowContextMenu { get; }

        /// <summary>
        /// Adds a new store stream tree node based on the specified stream as child of this node.
        /// </summary>
        /// <param name="streamMetadata">The stream to add to the tree.</param>
        /// <returns>A reference to the new stream tree node.</returns>
        IStreamTreeNode AddPath(IStreamMetadata streamMetadata);
    }
}