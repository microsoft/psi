// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements a visualization object that can display lists of coordinate systems.
    /// </summary>
    [VisualizationObject("Coordinate Systems")]
    public class CoordinateSystemListVisualizationObject : ModelVisual3DListVisualizationObject<CoordinateSystemVisualizationObject, CoordinateSystem>
    {
    }
}
