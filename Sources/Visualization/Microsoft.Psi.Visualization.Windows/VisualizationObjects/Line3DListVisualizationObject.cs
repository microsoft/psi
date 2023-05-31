// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements a visualization object that can display lists of 3D lines.
    /// </summary>
    [VisualizationObject("3D Lines")]
    public class Line3DListVisualizationObject : ModelVisual3DListVisualizationObject<Line3DVisualizationObject, Line3D?>
    {
    }
}
