// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements view for <see cref="VisualizationObjects.Line2DIntervalVisualizationObject"/>.
    /// </summary>
    /// <remarks>
    /// This class implements a base for the
    /// <see cref="Line2DIntervalVisualizationObjectView"/> class because
    /// WPF XAML does not support creating views with classes that derive
    /// directly off of a templated class. The functionality for the view is
    /// implmented in this class, and the view class simply derives off of
    /// this to avoid templating.
    /// </remarks>
    public class Line2DIntervalVisualizationObjectViewBase :
        XYIntervalVisualizationObjectCanvasView<
            Line2DIntervalVisualizationObject,
            Line2D?,
            Line2DVisualizationObjectCanvasItemView>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line2DIntervalVisualizationObjectViewBase"/> class.
        /// </summary>
        public Line2DIntervalVisualizationObjectViewBase()
            : base()
        {
        }

        /// <summary>
        /// Gets the scatter plot visualization object.
        /// </summary>
        public Line2DIntervalVisualizationObject Line2DIntervalVisualizationObject => this.VisualizationObject as Line2DIntervalVisualizationObject;
    }
}