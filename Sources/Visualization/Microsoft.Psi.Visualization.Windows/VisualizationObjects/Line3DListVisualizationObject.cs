// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a visualization object that can display lists of 3D lines.
    /// </summary>
    [VisualizationObject("3D Lines")]
    public class Line3DListVisualizationObject : ModelVisual3DVisualizationObjectEnumerable<Line3DVisualizationObject, Line3D, List<Line3D>>
    {
    }
}
