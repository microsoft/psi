// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    /// <summary>
    /// Class implements a generic client proxy for <see cref="VisualizationContainer" />.
    /// </summary>
    public sealed class VisualizationContainer
    {
        private IRemoteVisualizationContainer container;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainer"/> class.
        /// </summary>
        /// <param name="container">The remote visualization container.</param>
        internal VisualizationContainer(IRemoteVisualizationContainer container)
        {
            this.container = container;
        }

        /// <summary>
        /// Gets or sets the current remote visualization panel.
        /// </summary>
        public IRemoteVisualizationPanel CurrentRemoteVisualizationPanel
        {
            get => this.container.CurrentPanel;
            set => this.container.CurrentPanel = value;
        }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name
        {
            get => this.container.Name;
            set => this.container.Name = value;
        }

        /// <summary>
        /// Gets the remote navigator.
        /// </summary>
        public IRemoteNavigator RemoteNavigator => this.container.Navigator;

        /// <summary>
        /// Gets the remote visualization container.
        /// </summary>
        internal IRemoteVisualizationContainer RemoteVisualizationContainer => this.container;

        /// <summary>
        /// Adds a new visualization panel of the indicated type.
        /// </summary>
        /// <typeparam name="TPanel">The type of new visualization panel.</typeparam>
        /// <returns>An instance of the new visualization panel.</returns>
        public TPanel AddPanel<TPanel>()
            where TPanel : VisualizationPanel, new()
        {
            TPanel panel = new TPanel();
            panel.Container = this;
            panel.RemoteVisualizationPanel = this.container.AddPanel(panel.TypeName);
            return panel;
        }

        /// <summary>
        /// Clears the visualization container.
        /// </summary>
        public void Clear()
        {
            this.container.Clear();
        }

        /// <summary>
        /// Removes the indicated panel.
        /// </summary>
        /// <param name="panel">The visualization panel to remove.</param>
        public void RemovePanel(VisualizationPanel panel)
        {
            this.container.RemovePanel(panel.RemoteVisualizationPanel);
        }
    }
}
