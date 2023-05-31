// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.DataTypes
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a graph of objects.
    /// </summary>
    /// <typeparam name="TKey">The type of the key for each element in the graph.</typeparam>
    /// <typeparam name="TNode">The type of the graph nodes.</typeparam>
    /// <typeparam name="TEdge">The type of the graph edges.</typeparam>
    public class Graph<TKey, TNode, TEdge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Graph{TKey, TNode, TEdge}"/> class.
        /// </summary>
        /// <param name="nodes">The nodes of the graph.</param>
        /// <param name="edges">The edges of the graph.</param>
        public Graph(Dictionary<TKey, TNode> nodes, Dictionary<(TKey, TKey), TEdge> edges)
        {
            this.Nodes = nodes;
            this.Edges = edges;
        }

        /// <summary>
        /// Gets the set of nodes in the graph.
        /// </summary>
        public Dictionary<TKey, TNode> Nodes { get; }

        /// <summary>
        /// Gets the set of edges in the graph.
        /// </summary>
        public Dictionary<(TKey Start, TKey End), TEdge> Edges { get; }
    }
}
