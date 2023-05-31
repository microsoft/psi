// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object that can display lists of linear velocity objects.
    /// </summary>
    [VisualizationObject("Linear 3D-velocities")]
    public class LinearVelocity3DListVisualizationObject : ModelVisual3DListVisualizationObject<LinearVelocity3DVisualizationObject, LinearVelocity3D>
    {
    }
}