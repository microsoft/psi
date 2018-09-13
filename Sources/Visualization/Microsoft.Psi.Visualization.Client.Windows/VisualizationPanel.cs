// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Class implements a generic client proxy for Microsoft.Psi.Visualization.VisualizationPanels.VisualizationPanel />.
    /// </summary>
    public abstract class VisualizationPanel : ComObservableObject
    {
        private VisualizationContainer container;

        /// <summary>
        /// Gets visualization object parent container.
        /// </summary>
        public VisualizationContainer Container
        {
            get => this.container;
            internal set => this.container = value;
        }

        /// <summary>
        /// Getst the current remote visualization object.
        /// </summary>
        public IRemoteVisualizationObject CurrentRemoteVisualizationObject => this.RemoteVisualizationPanel.CurrentVisualizationObject;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public abstract double Height { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is the current panel.
        /// </summary>
        public bool IsCurrentPanel => this.RemoteVisualizationPanel.IsCurrentPanel;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public abstract string Name { get; set;  }

        /// <summary>
        /// Gets the navigator.
        /// </summary>
        public IRemoteNavigator RemoteNavigator => this.RemoteVisualizationPanel.Navigator;

        /// <summary>
        /// Gets the remote visualization panel type name.
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public abstract double Width { get; }

        /// <summary>
        /// Gets or sets the remote visualization panel.
        /// </summary>
        internal abstract IRemoteVisualizationPanel RemoteVisualizationPanel { get; set; }

        /// <summary>
        /// Adds a new visualization object of the indicated type.
        /// </summary>
        /// <typeparam name="TObject">Type of new visualization object to add.</typeparam>
        /// <typeparam name="TConfig">Type of the configuration of the new visualization object to add.</typeparam>
        /// <returns>An instance of the new visualization object.</returns>
        public TObject AddVisualizationObject<TObject, TConfig>()
            where TObject : VisualizationObject<TConfig>, new()
            where TConfig : VisualizationObjectConfiguration, new()
        {
            TObject visualizationObject = new TObject();
            visualizationObject.Panel = this;

            //// Due to security concerns around running unknown code, we've temporarily
            //// disabled the ability to load 3rd party remote Visualizers in Psi Studio.

            /*var executingAssembly = Assembly.GetExecutingAssembly();
            if (executingAssembly.GetType(visualizationObject.TypeName) != null)
            {
                visualizationObject.IVisualizationObject = this.RemoteVisualizationPanel.AddVisualizationObject(executingAssembly.Location, visualizationObject.TypeName);
            }
            else
            {
                // find the assembly containing the visualization object type on the client side
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetType(visualizationObject.TypeName) != null);

                if (assembly != null)
                {
                    visualizationObject.IVisualizationObject = this.RemoteVisualizationPanel.AddVisualizationObject(assembly.Location, visualizationObject.TypeName);
                }
                else
                {
                    visualizationObject.IVisualizationObject = this.RemoteVisualizationPanel.AddVisualizationObject(visualizationObject.TypeName);
                }
            }*/

            visualizationObject.IVisualizationObject = this.RemoteVisualizationPanel.AddVisualizationObject(visualizationObject.TypeName);

            return visualizationObject;
        }

        /// <summary>
        /// Brings the indictated remote visualization object to the front of z-order within this panel.
        /// </summary>
        /// <typeparam name="TObject">Type of visualization object to move.</typeparam>
        /// <typeparam name="TConfig">Type of the configuration of the visualization object to move.</typeparam>
        /// <param name="visualizationObject">The visualization object to move.</param>
        public void BringToFront<TObject, TConfig>(TObject visualizationObject)
            where TObject : VisualizationObject<TConfig>, new()
            where TConfig : VisualizationObjectConfiguration, new()
        {
            this.RemoteVisualizationPanel.BringToFront(visualizationObject.IVisualizationObject);
        }

        /// <summary>
        /// Clears the visualization panel.
        /// </summary>
        public void Clear()
        {
            this.RemoteVisualizationPanel.Clear();
        }

        /// <summary>
        /// Removes the indicated visualization object.
        /// </summary>
        /// <typeparam name="TObject">Type of visualization object to remove.</typeparam>
        /// <typeparam name="TConfig">Type of the configuration of the visualization object to remove.</typeparam>
        /// <param name="visualizationObject">The visualization object to remove.</param>
        public void RemoveVisualizationObject<TObject, TConfig>(TObject visualizationObject)
            where TObject : VisualizationObject<TConfig>, new()
            where TConfig : VisualizationObjectConfiguration, new()
        {
            this.RemoteVisualizationPanel.RemoveVisualizationObject(visualizationObject.IVisualizationObject);
        }

        /// <summary>
        /// Sends the indictated remote visualization object to the back of z-order within this panel.
        /// </summary>
        /// <typeparam name="TObject">Type of new visualization object to move.</typeparam>
        /// <typeparam name="TConfig">Type of the configuration of the new visualization object to move.</typeparam>
        /// <param name="visualizationObject">The visualization object to move.</param>
        public void SendToBack<TObject, TConfig>(TObject visualizationObject)
            where TObject : VisualizationObject<TConfig>, new()
            where TConfig : VisualizationObjectConfiguration, new()
        {
            this.RemoteVisualizationPanel.SendToBack(visualizationObject.IVisualizationObject);
        }
    }
}
