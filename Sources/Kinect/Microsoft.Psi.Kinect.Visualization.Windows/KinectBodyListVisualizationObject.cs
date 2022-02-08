// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect.Visualization
{
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a visualization object for Azure Kinect bodies.
    /// </summary>
    [VisualizationObject("Kinect Bodies")]
    public class KinectBodyListVisualizationObject : ModelVisual3DListVisualizationObject<KinectBodyVisualizationObject, KinectBody>
    {
    }
}