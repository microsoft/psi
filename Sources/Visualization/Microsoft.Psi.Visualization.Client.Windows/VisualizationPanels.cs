// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name must match first type name.
#pragma warning disable SA1402 // File may only contain a single class.

namespace Microsoft.Psi.Visualization.Client
{
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationPanels.TimelineVisualizationPanel />.
    /// </summary>
    public sealed class TimelineVisualizationPanel : VisualizationPanel<TimelineVisualizationPanelConfiguration>
    {
        /// <inheritdoc />
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationPanels.TimelineVisualizationPanel";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationPanels.XYVisualizationPanel />.
    /// </summary>
    public sealed class XYVisualizationPanel : VisualizationPanel<XYVisualizationPanelConfiguration>
    {
        /// <inheritdoc />
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationPanels.XYVisualizationPanel";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationPanels.XYZVisualizationPanel />.
    /// </summary>
    public sealed class XYZVisualizationPanel : VisualizationPanel<XYZVisualizationPanelConfiguration>
    {
        /// <inheritdoc />
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationPanels.XYZVisualizationPanel";
    }
}
