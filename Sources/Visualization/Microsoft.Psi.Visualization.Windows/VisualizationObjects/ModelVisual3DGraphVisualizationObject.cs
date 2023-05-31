// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.DataTypes;

    /// <summary>
    /// Implements a visualization object for Point3D graphs.
    /// </summary>
    /// <typeparam name="TNodeVisual3D">The type of the node visual.</typeparam>
    /// <typeparam name="TEdgeVisual3D">The type of the edge visual.</typeparam>
    /// <typeparam name="TNodeKey">The type of the graph node key.</typeparam>
    /// <typeparam name="TNode">The type of the graph nodes.</typeparam>
    /// <typeparam name="TEdge">The type of the graph edges.</typeparam>
    public abstract class ModelVisual3DGraphVisualizationObject<TNodeVisual3D, TEdgeVisual3D, TNodeKey, TNode, TEdge> :
        ModelVisual3DVisualizationObject<Graph<TNodeKey, TNode, TEdge>>
        where TNodeVisual3D : Visual3D, new()
        where TEdgeVisual3D : Visual3D, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DGraphVisualizationObject{TNodeVisualizationObject, TEdgeVisualizationObject, TNodeKey, TNode, TEdge}"/> class.
        /// </summary>
        /// <param name="nodeVisibilityFunc">An optional function that computes node visibility.</param>
        /// <param name="edgeVisibilityFunc">An optional function that computes edge visibility.</param>
        public ModelVisual3DGraphVisualizationObject(
            Func<TNodeKey, bool> nodeVisibilityFunc = null,
            Func<(TNodeKey, TNodeKey), bool> edgeVisibilityFunc = null)
        {
            this.NodeVisibilityFunc = nodeVisibilityFunc ?? (_ => true);
            this.EdgeVisibilityFunc = edgeVisibilityFunc ?? (_ => true);

            this.NodesVisuals = new UpdatableVisual3DDictionary<TNodeKey, TNodeVisual3D>(null);
            this.EdgesVisuals = new UpdatableVisual3DDictionary<(TNodeKey Start, TNodeKey End), TEdgeVisual3D>(null);

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets the visual nodes.
        /// </summary>
        protected UpdatableVisual3DDictionary<TNodeKey, TNodeVisual3D> NodesVisuals { get; }

        /// <summary>
        /// Gets the visual edges.
        /// </summary>
        protected UpdatableVisual3DDictionary<(TNodeKey Start, TNodeKey End), TEdgeVisual3D> EdgesVisuals { get; }

        /// <summary>
        /// Gets the node visibility function.
        /// </summary>
        protected Func<TNodeKey, bool> NodeVisibilityFunc { get; }

        /// <summary>
        /// Gets the edge visibility function.
        /// </summary>
        protected Func<(TNodeKey, TNodeKey), bool> EdgeVisibilityFunc { get; }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                this.UpdateVisuals();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <summary>
        /// Provides an abstract method for updating node visualization.
        /// </summary>
        /// <param name="nodeVisual3D">The node visual to update.</param>
        /// <param name="nodeKey">The node key.</param>
        protected abstract void UpdateNodeVisuals(TNodeVisual3D nodeVisual3D, TNodeKey nodeKey);

        /// <summary>
        /// Provides an abstract method for updating edge visualization.
        /// </summary>
        /// <param name="edgeVisual3D">The edge visual to update.</param>
        /// <param name="edge">The edge information.</param>
        protected abstract void UpdateEdgeVisuals(TEdgeVisual3D edgeVisual3D, (TNodeKey Start, TNodeKey End) edge);

        /// <summary>
        /// Updates the visuals.
        /// </summary>
        protected void UpdateVisuals()
        {
            // update the nodes
            this.NodesVisuals.BeginUpdate();

            if (this.CurrentData != null)
            {
                foreach (var nodeKey in this.CurrentData.Nodes.Keys)
                {
                    var nodeVisualizationObject = this.NodesVisuals[nodeKey];
                    this.UpdateNodeVisuals(nodeVisualizationObject, nodeKey);
                }
            }

            this.NodesVisuals.EndUpdate();

            // update the edges
            this.EdgesVisuals.BeginUpdate();

            if (this.CurrentData != null)
            {
                foreach (var edge in this.CurrentData.Edges.Keys)
                {
                    var edgeVisualizationObject = this.EdgesVisuals[edge];
                    this.UpdateEdgeVisuals(edgeVisualizationObject, edge);
                }
            }

            this.EdgesVisuals.EndUpdate();
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.NodesVisuals, this.Visible && this.CurrentData != default);
            this.UpdateChildVisibility(this.EdgesVisuals, this.Visible && this.CurrentData != default);
        }
    }
}