// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides an abstract base class for a canvas-based view of a <see cref="StreamValueVisualizationObject{TData}"/>.
    /// </summary>
    /// <typeparam name="TStreamValueVisualizationObject">The type of the stream value visualization object.</typeparam>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    public abstract class StreamValueVisualizationObjectCanvasView<TStreamValueVisualizationObject, TData> :
        StreamVisualizationObjectCanvasView<TStreamValueVisualizationObject, TData>
        where TStreamValueVisualizationObject : StreamValueVisualizationObject<TData>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamValueVisualizationObjectCanvasView{TStreamValueVisualizationObject, TData}"/> class.
        /// </summary>
        public StreamValueVisualizationObjectCanvasView()
            : base()
        {
        }

        /// <summary>
        /// Gets the stream value visualization object.
        /// </summary>
        public virtual TStreamValueVisualizationObject StreamValueVisualizationObject =>
            this.VisualizationObject as TStreamValueVisualizationObject;

        /// <inheritdoc/>
        protected override void OnCurrentValueChanged()
        {
            base.OnCurrentValueChanged();
            this.UpdateView();
        }
    }
}
