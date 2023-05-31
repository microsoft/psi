// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object that can display lists of 3D rectangles.
    /// </summary>
    [VisualizationObject("3D Rectangles")]
    public class Rectangle3DListVisualizationObject : ModelVisual3DListVisualizationObject<Rectangle3DVisualizationObject, Rectangle3D?>
    {
    }
}
