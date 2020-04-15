// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a visualization object that can display lists of 3D rectangles.
    /// </summary>
    [VisualizationObject("Visualize")]
    public class Rect3DListVisualizationObject : ModelVisual3DVisualizationObjectEnumerable<Rect3DVisualizationObject, Rect3D, List<Rect3D>>
    {
    }
}
