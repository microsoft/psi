// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.Collections.Generic;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for <see cref="List{DepthImageRectangle3D}"/>.
    /// </summary>
    [VisualizationObject("Depth Images in 3D Rectangles")]
    public class DepthImageRectangle3DListVisualizationObject : ModelVisual3DListVisualizationObject<DepthImageRectangle3DVisualizationObject, DepthImageRectangle3D>
    {
    }
}