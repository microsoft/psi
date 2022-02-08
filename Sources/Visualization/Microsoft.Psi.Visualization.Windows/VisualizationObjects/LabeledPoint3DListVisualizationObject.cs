// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements a visualization object that can display lists of labeled points as spheres.
    /// </summary>
    [VisualizationObject("Labeled 3D Spheres")]
    public class LabeledPoint3DListVisualizationObject : ModelVisual3DListVisualizationObject<LabeledPoint3DVisualizationObject, Tuple<string, Point3D>>
    {
    }
}
