// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object that can display lists of 3D boxes.
    /// </summary>
    [VisualizationObject("3D boxes")]
    public class Box3DListVisualizationObject : ModelVisual3DListVisualizationObject<Box3DVisualizationObject, Box3D>
    {
    }
}
