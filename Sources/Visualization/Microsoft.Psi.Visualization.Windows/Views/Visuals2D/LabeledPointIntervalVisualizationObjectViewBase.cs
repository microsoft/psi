// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements view for <see cref="VisualizationObjects.LabeledPointIntervalVisualizationObject"/>.
    /// </summary>
    /// <remarks>
    /// This class implements a base for the
    /// <see cref="LabeledPointIntervalVisualizationObjectView"/> class because
    /// WPF XAML does not support creating views with classes that derive
    /// directly off of a templated class. The functionality for the view is
    /// implmented in this class, and the view class simply derives off of
    /// this to avoid templating.
    /// </remarks>
    public class LabeledPointIntervalVisualizationObjectViewBase :
        XYIntervalVisualizationObjectCanvasView<
            LabeledPointIntervalVisualizationObject,
            Tuple<Point, string, string>,
            LabeledPointVisualizationObjectCanvasItemView>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPointIntervalVisualizationObjectViewBase"/> class.
        /// </summary>
        public LabeledPointIntervalVisualizationObjectViewBase()
            : base()
        {
        }

        /// <summary>
        /// Gets the scatter plot visualization object.
        /// </summary>
        public LabeledPointIntervalVisualizationObject LabeledPointIntervalVisualizationObject
            => this.VisualizationObject as LabeledPointIntervalVisualizationObject;
    }
}