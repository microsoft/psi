// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.Collections.Generic;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for <see cref="List{ImageRectangle3D}"/>.
    /// </summary>
    [VisualizationObject("Images in 3D Rectangles")]
    public class ImageRectangle3DListVisualizationObject : ModelVisual3DListVisualizationObject<ImageRectangle3DVisualizationObject, ImageRectangle3D>
    {
    }
}