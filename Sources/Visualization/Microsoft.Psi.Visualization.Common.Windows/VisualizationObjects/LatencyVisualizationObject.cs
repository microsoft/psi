// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Summarizers;

    /// <summary>
    /// Represents a time interval visualization object.
    /// </summary>
    [VisualizationObject("Visualize Latency", typeof(TimeIntervalSummarizer), IconSourcePath.Latency, IconSourcePath.LatencyInPanel, "%StreamName% (Latency)", true)]
    public class LatencyVisualizationObject : TimeIntervalVisualizationObject
    {
    }
}
