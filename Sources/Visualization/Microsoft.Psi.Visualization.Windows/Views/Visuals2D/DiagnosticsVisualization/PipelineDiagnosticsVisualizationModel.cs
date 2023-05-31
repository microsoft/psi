// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DiagnosticsVisualizationObjectView.xaml.
    /// </summary>
    public partial class PipelineDiagnosticsVisualizationModel
    {
        private readonly Stack<int> navStack = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsVisualizationModel"/> class.
        /// </summary>
        public PipelineDiagnosticsVisualizationModel()
        {
            this.Reset();
        }

        /// <summary>
        /// Gets or sets visualization object.
        /// </summary>
        public PipelineDiagnosticsVisualizationObject VisualizationObject { get; set; }

        /// <summary>
        /// Gets or sets diagnostics graph.
        /// </summary>
        public PipelineDiagnostics Graph { get; set; }

        /// <summary>
        /// Gets navigation stack of graphs/subgraphs.
        /// </summary>
        public Stack<int> NavStack => this.navStack;

        /// <summary>
        /// Gets or sets the ID of the selected graph edge.
        /// </summary>
        public int SelectedEdgeId { get; set; }

        /// <summary>
        /// Gets or sets details of selected edge.
        /// </summary>
        public string SelectedEdgeDetails { get; set; }

        /// <summary>
        /// Reset model state.
        /// </summary>
        public void Reset()
        {
            this.Graph = null;
            this.navStack.Clear();
            this.SelectedEdgeId = -1;
            this.SelectedEdgeDetails = string.Empty;
        }
    }
}
