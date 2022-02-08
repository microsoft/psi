// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a 3D depth image rectangle list visualization object.
    /// </summary>
    [VisualizationObject("3D Depth Image Rectangles")]
    public class DepthImageRectangle3DListVisualizationObject : ModelVisual3DListVisualizationObject<DepthImageRectangle3DVisualizationObject, DepthImageRectangle3D>
    {
    }
}