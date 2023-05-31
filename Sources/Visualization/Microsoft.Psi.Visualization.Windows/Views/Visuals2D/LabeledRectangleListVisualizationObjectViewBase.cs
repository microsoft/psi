// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements view for <see cref="LabeledRectangleListVisualizationObject"/>.
    /// </summary>
    /// <remarks>
    /// This class implements a base for the
    /// <see cref="LabeledRectangleListVisualizationObjectView"/> class because
    /// WPF XAML does not support creating views with classes that derive
    /// directly off of a templated class. The functionality for the view is
    /// implmented in this class, and the view class simply derives off of
    /// this to avoid templating.
    /// </remarks>
    public class LabeledRectangleListVisualizationObjectViewBase :
        XYValueEnumerableVisualizationObjectCanvasView<
            LabeledRectangleListVisualizationObject,
            Tuple<System.Drawing.Rectangle, string, string>,
            List<Tuple<System.Drawing.Rectangle, string, string>>,
            LabeledRectangleListVisualizationObjectCanvasItemView>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledRectangleListVisualizationObjectViewBase"/> class.
        /// </summary>
        public LabeledRectangleListVisualizationObjectViewBase()
            : base()
        {
        }

        /// <summary>
        /// Gets the labeled rectangle list visualization object.
        /// </summary>
        public LabeledRectangleListVisualizationObject LabeledRectangleListVisualizationObject =>
            this.VisualizationObject as LabeledRectangleListVisualizationObject;
    }
}
