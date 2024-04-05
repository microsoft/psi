// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.OpenXR.Visualization
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object that can display a stream of hands from an interval.
    /// </summary>
    [VisualizationObject("stream interval of Hands", typeof(HandSamplingSummarizer))]
    public class HandIntervalVisualizationObject : ModelVisual3DIntervalVisualizationObject<HandVisualizationObject, Hand>
    {
    }
}