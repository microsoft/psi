// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a visualization object that can be displayed in a 2D panel.
    /// </summary>
    /// <typeparam name="TData">The type of the instant visualization.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XY)]
    public abstract class Instant2DVisualizationObject<TData> : InstantVisualizationObject<TData>
    {
    }
}
