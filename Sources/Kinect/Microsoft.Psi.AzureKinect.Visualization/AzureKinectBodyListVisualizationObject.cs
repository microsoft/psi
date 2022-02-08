// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect.Visualization
{
    using System.Collections.Generic;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for a list of Azure Kinect bodies.
    /// </summary>
    [VisualizationObject("Azure Kinect Bodies")]
    public class AzureKinectBodyListVisualizationObject : ModelVisual3DListVisualizationObject<AzureKinectBodyVisualizationObject, AzureKinectBody>
    {
    }
}