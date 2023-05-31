// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Summarizers;

    /// <summary>
    /// Implements a time interval visualization object.
    /// </summary>
    [VisualizationObject("Latency", typeof(TimeIntervalSummarizer), IconSourcePath.Latency, IconSourcePath.LatencyInPanel, "%StreamName% (Latency)", true)]
    public class LatencyVisualizationObject : TimeIntervalVisualizationObject
    {
    }
}
