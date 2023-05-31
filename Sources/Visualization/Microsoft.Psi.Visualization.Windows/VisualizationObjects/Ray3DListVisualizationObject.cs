// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements a visualization object that can display lists of 3D rays.
    /// </summary>
    [VisualizationObject("3D Rays")]
    public class Ray3DListVisualizationObject : ModelVisual3DListVisualizationObject<Ray3DVisualizationObject, Ray3D?>
    {
    }
}
