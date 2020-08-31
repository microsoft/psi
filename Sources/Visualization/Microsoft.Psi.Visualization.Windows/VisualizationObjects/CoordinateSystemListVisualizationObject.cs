// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a visualization object that can display lists of coordinate systems.
    /// </summary>
    [VisualizationObject("Coordinate Systems")]
    public class CoordinateSystemListVisualizationObject : ModelVisual3DVisualizationObjectEnumerable<CoordinateSystemVisualizationObject, CoordinateSystem, List<CoordinateSystem>>
    {
    }
}
