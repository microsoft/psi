// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents the base class for 2D depth image visualization object views.
    /// </summary>
    public abstract class DepthImageVisualizationObjectViewBase : XYValueVisualizationObjectCanvasView<DepthImageVisualizationObject, Shared<DepthImage>>
    {
    }
}
