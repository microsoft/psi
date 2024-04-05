// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements view for <see cref="Line2DListVisualizationObject"/>.
    /// </summary>
    /// <remarks>
    /// This class implements a base for the
    /// <see cref="Line2DListVisualizationObject"/> class because
    /// WPF XAML does not support creating views with classes that derive
    /// directly off of a templated class. The functionality for the view is
    /// implmented in this class, and the view class simply derives off of
    /// this to avoid templating.
    /// </remarks>
    public class Line2DListVisualizationObjectViewBase :
        XYValueEnumerableVisualizationObjectCanvasView<
            Line2DListVisualizationObject,
            Line2D?,
            List<Line2D?>,
            Line2DVisualizationObjectCanvasItemView>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line2DListVisualizationObjectViewBase"/> class.
        /// </summary>
        public Line2DListVisualizationObjectViewBase()
            : base()
        {
        }

        /// <summary>
        /// Gets the visualization object.
        /// </summary>
        public Line2DListVisualizationObject Line2DListVisualizationObject => this.VisualizationObject as Line2DListVisualizationObject;
    }
}
