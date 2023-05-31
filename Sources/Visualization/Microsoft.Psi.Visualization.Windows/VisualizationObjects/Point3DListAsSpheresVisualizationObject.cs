// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object that can display lists of Windows 3D points.
    /// </summary>
    [VisualizationObject("3D Spheres")]
    public class Point3DListAsSpheresVisualizationObject : ModelVisual3DListVisualizationObject<Point3DAsSphereVisualizationObject, Point3D?>
    {
    }
}