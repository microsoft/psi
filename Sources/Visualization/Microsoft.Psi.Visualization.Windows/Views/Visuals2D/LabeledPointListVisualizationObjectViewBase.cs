// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements view for <see cref="LabeledPointListVisualizationObject"/>.
    /// </summary>
    /// <remarks>
    /// This class implements a base for the
    /// <see cref="LabeledPointListVisualizationObjectView"/> class because
    /// WPF XAML does not support creating views with classes that derive
    /// directly off of a templated class. The functionality for the view is
    /// implmented in this class, and the view class simply derives off of
    /// this to avoid templating.
    /// </remarks>
    public class LabeledPointListVisualizationObjectViewBase :
        XYValueEnumerableVisualizationObjectCanvasView<
            LabeledPointListVisualizationObject,
            Tuple<Point, string, string>,
            List<Tuple<Point, string, string>>,
            LabeledPointListVisualizationObjectCanvasItemView>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPointListVisualizationObjectViewBase"/> class.
        /// </summary>
        public LabeledPointListVisualizationObjectViewBase()
            : base()
        {
        }

        /// <summary>
        /// Gets the scatter plot visualization object.
        /// </summary>
        public LabeledPointListVisualizationObject LabeledPointListVisualizationObject =>
            this.VisualizationObject as LabeledPointListVisualizationObject;
    }
}
