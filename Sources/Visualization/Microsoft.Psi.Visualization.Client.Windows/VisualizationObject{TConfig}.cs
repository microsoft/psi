// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Config;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Class implements a generic client proxy for Microsoft.Psi.Visualization.VisualizationObjects.VisualizationObject{TConfig}.
    /// </summary>
    /// <typeparam name="TConfig">The type of the visualization configuration.</typeparam>
    public abstract class VisualizationObject<TConfig> : ComObservableObject, INotifyRemoteConfigurationChanged
        where TConfig : VisualizationObjectConfiguration, new()
    {
        private TConfig configuration;
        private uint cookie;
        private VisualizationPanel panel;
        private IRemoteVisualizationObject visualizationOjbect;
        private bool remoteConfigurationChanging;

        /// <summary>
        /// Finalizes an instance of the <see cref="VisualizationObject{TConfig}"/> class.
        /// </summary>
        ~VisualizationObject()
        {
            if (this.cookie != 0)
            {
                try
                {
                    this.visualizationOjbect.Unadvise(this.cookie);
                }
                catch (COMException)
                {
                }
                catch (InvalidCastException)
                {
                }
            }
        }

        /// <summary>
        /// Gets visualization object configuration.
        /// </summary>
        public TConfig Configuration
        {
            get
            {
                if (this.configuration == null)
                {
                    this.UpdateConfiguration(this.visualizationOjbect.GetConfiguration());
                }

                return this.configuration;
            }
        }

        /// <summary>
        /// Gets visualization object parent container.
        /// </summary>
        public VisualizationContainer Container => this.panel.Container;

        /// <summary>
        /// Gets visualization object parent panel.
        /// </summary>
        public VisualizationPanel Panel
        {
            get => this.panel;
            internal set => this.panel = value;
        }

        /// <summary>
        /// Gets the remote visualization object type name.
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// Gets or sets remote visualization object.
        /// </summary>
        internal IRemoteVisualizationObject IVisualizationObject
        {
            get => this.visualizationOjbect;
            set
            {
                if (this.cookie != 0)
                {
                    this.visualizationOjbect.Unadvise(this.cookie);
                }

                this.visualizationOjbect = value;
                this.cookie = this.visualizationOjbect.Advise(this);
            }
        }

        /// <summary>
        /// Gets JSON contract resolver. Defaults to null - DefaultContractResolver.
        /// </summary>
        protected virtual IContractResolver ContractResolver => null;

        /// <summary>
        /// Brings this remote visualization object to the top of z-order within its containing panel.
        /// </summary>
        public void BringToFront()
        {
            this.visualizationOjbect.BringToFront();
        }

        /// <summary>
        /// Closes and removes this remote visualization object.
        /// </summary>
        public void Close()
        {
            this.visualizationOjbect.Close();
        }

        /// <inheritdoc />
        public void OnRemoteConfigurationChanged(string jsonConfiguration)
        {
            // Update our local configuration only if we did not initiate the change
            if (!this.remoteConfigurationChanging)
            {
                this.UpdateConfiguration(jsonConfiguration);
            }
        }

        /// <summary>
        /// Sends this remote visualization object to the back of z-order within its containing panel.
        /// </summary>
        public void SendToBack()
        {
            this.visualizationOjbect.SendToBack();
        }

        private void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = this.ContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            string jsonConfiguration = JsonConvert.SerializeObject(this.configuration, jsonSerializerSettings);

            // Update the remote configuration and set a flag to ignore the resulting remote configuration changed notification
            this.remoteConfigurationChanging = true;
            this.visualizationOjbect.SetConfiguration(jsonConfiguration);
            this.remoteConfigurationChanging = false;
        }

        private void UpdateConfiguration(string jsonConfiguration)
        {
            if (this.configuration != null)
            {
                this.configuration.PropertyChanged -= this.OnConfigurationPropertyChanged;
            }

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = this.ContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            this.RaisePropertyChanging(nameof(this.Configuration));
            this.configuration = JsonConvert.DeserializeObject<TConfig>(jsonConfiguration, jsonSerializerSettings);
            this.configuration.PropertyChanged += this.OnConfigurationPropertyChanged;
            this.RaisePropertyChanged(nameof(this.Configuration));
        }
    }
}
