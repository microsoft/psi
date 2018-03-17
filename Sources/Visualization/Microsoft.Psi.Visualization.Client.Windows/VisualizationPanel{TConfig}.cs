// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Visualization.Config;
    using Newtonsoft.Json;

    /// <summary>
    /// Class implements a generic client proxy for Microsoft.Psi.Visualization.VisualizationPanels.VisualizationPanel />.
    /// </summary>
    /// <typeparam name="TConfig">Type of configuration of visualization panel.</typeparam>
    public abstract class VisualizationPanel<TConfig> : VisualizationPanel, INotifyRemoteConfigurationChanged
        where TConfig : VisualizationPanelConfiguration, new()
    {
        private TConfig configuration;
        private uint cookie;
        private IRemoteVisualizationPanel panel;
        private bool remoteConfigurationChanging;

        /// <summary>
        /// Finalizes an instance of the <see cref="VisualizationPanel{TConfig}"/> class.
        /// </summary>
        ~VisualizationPanel()
        {
            if (this.cookie != 0)
            {
                try
                {
                    this.panel.Unadvise(this.cookie);
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
        /// Gets visualization panel configuration.
        /// </summary>
        public TConfig Configuration
        {
            get
            {
                if (this.configuration == null)
                {
                    this.UpdateConfiguration(this.panel.GetConfiguration());
                }

                return this.configuration;
            }
        }

        /// <inheritdoc />
        public override double Height { get => this.Configuration.Height; set => this.Configuration.Height = value; }

        /// <inheritdoc />
        public override string Name { get => this.Configuration.Name; set => this.Configuration.Name = value; }

        /// <inheritdoc />
        public override double Width { get => this.Configuration.Width; }

        /// <inheritdoc />
        internal override IRemoteVisualizationPanel RemoteVisualizationPanel
        {
            get => this.panel;
            set
            {
                if (this.cookie != 0)
                {
                    this.panel.Unadvise(this.cookie);
                }

                this.panel = value;
                this.cookie = this.panel.Advise(this);
            }
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

        private void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string jsonConfiguration = JsonConvert.SerializeObject(this.configuration);

            // Update the remote configuration and set a flag to ignore the resulting remote configuration changed notification
            this.remoteConfigurationChanging = true;
            this.panel.SetConfiguration(jsonConfiguration);
            this.remoteConfigurationChanging = false;
        }

        private void UpdateConfiguration(string jsonConfiguration)
        {
            if (this.configuration != null)
            {
                this.configuration.PropertyChanged -= this.OnConfigurationPropertyChanged;
            }

            this.RaisePropertyChanging(nameof(this.Configuration));
            this.configuration = JsonConvert.DeserializeObject<TConfig>(jsonConfiguration);
            this.configuration.PropertyChanged += this.OnConfigurationPropertyChanged;
            this.RaisePropertyChanged(nameof(this.Configuration));
        }
    }
}
