// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object that can display lists of 3D rectangles.
    /// </summary>
    [VisualizationObject("3D Rectangles")]
    public class Rect3DListVisualizationObject : ModelVisual3DListVisualizationObject<Rect3DVisualizationObject, Rect3D?>
    {
    }
}
