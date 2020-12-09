// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a visualization object that can display lists of Windows 3D points.
    /// </summary>
    [VisualizationObject("3D Points")]
    public class WindowsPoint3DListVisualizationObject : ModelVisual3DVisualizationObjectEnumerable<WindowsPoint3DVisualizationObject, Point3D?, List<Point3D?>>
    {
    }
}