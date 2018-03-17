// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Newtonsoft.Json;

    /// <summary>
    /// Represent the client API singleton for remote access to PsiStudio.
    /// </summary>
    public class VisualizationClient
    {
        private IRemoteVisualizationService visualizationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationClient"/> class.
        /// </summary>
        public VisualizationClient()
        {
            this.visualizationService = (IRemoteVisualizationService)new VisualizationService();
            this.CurrentContainer = new VisualizationContainer(this.visualizationService.CurrentContainer);
        }

        /// <summary>
        /// Gets the current <see cref="VisualizationContainer"/>.
        /// </summary>
        public VisualizationContainer CurrentContainer { get; private set; }

        /// <summary>
        /// Gets the current <see cref="VisualizationPanel"/>.
        /// </summary>
        public VisualizationPanel CurrentPanel { get; private set; }

        /// <summary>
        /// Adds a new <see cref="VisualizationPanel"/> of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of visualization panel to be added.</typeparam>
        /// <returns>Instance of the new visualization panel.</returns>
        public T AddPanel<T>()
            where T : VisualizationPanel, new()
        {
            this.CurrentPanel = this.CurrentContainer.AddPanel<T>();
            return this.CurrentPanel as T;
        }

        /// <summary>
        /// Ensure the specified stream is currently open. If stream is not open, it will be upon retun.
        /// </summary>
        /// <param name="streamBinding">Stream binding inidicting which stream to open.</param>
        public void EnsureBinding(StreamBinding streamBinding)
        {
            string jsonStreamBinding = JsonConvert.SerializeObject(streamBinding);
            this.visualizationService.EnsureBinding(jsonStreamBinding);
        }

        /// <summary>
        /// Shows the specified stream in an appropriate visualization panel, creating a new one if needed.
        /// </summary>
        /// <typeparam name="TObject">The type of client stream visualization object (proxy) to create.</typeparam>
        /// <typeparam name="TData">The type of underlying data of the stream visualization object.</typeparam>
        /// <typeparam name="TConfig">The type of configuration of the stream visualization object</typeparam>
        /// <typeparam name="TPanel">The type of visauzalition panel required to show the stream.</typeparam>
        /// <param name="streamBinding">Stream binding inidicting which stream to open.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public TObject Show<TObject, TData, TConfig, TPanel>(StreamBinding streamBinding)
            where TObject : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
            where TPanel : VisualizationPanel, new()
        {
            this.EnsureCurrentPanel<TPanel>();
            this.EnsureBinding(streamBinding);

            TObject visObj = this.CurrentPanel.AddVisualizationObject<TObject, TConfig>();
            visObj.Configuration.Name = streamBinding.StreamName;
            visObj.OpenStream(streamBinding);

            return visObj;
        }

        /// <summary>
        /// Ensures current panel is properly setup.
        /// </summary>
        /// <typeparam name="T">The type of panel that is required. If the current panel is not of this type a new one will be created.</typeparam>
        private void EnsureCurrentPanel<T>()
            where T : VisualizationPanel, new()
        {
            if (this.CurrentPanel == null || (this.CurrentPanel as T) == null)
            {
                this.CurrentPanel = this.CurrentContainer.AddPanel<T>();
            }
        }
    }
}