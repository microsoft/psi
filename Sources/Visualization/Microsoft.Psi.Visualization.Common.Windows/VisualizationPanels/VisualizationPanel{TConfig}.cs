// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Config;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a base class for visualization panels.
    /// </summary>
    /// <typeparam name="TConfig">The type of the visualization panel configuration.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class VisualizationPanel<TConfig> : VisualizationPanel
        where TConfig : VisualizationPanelConfiguration, new()
    {
        private TConfig configuration;

        /// <summary>
        /// Gets or sets the visualization panel configuration.
        /// </summary>
        [DataMember]
        public TConfig Configuration
        {
            get => this.configuration;
            set
            {
                if (this.configuration != null)
                {
                    this.configuration.PropertyChanged -= this.OnConfigurationPropertyChanged;
                }

                this.Set(nameof(this.Configuration), ref this.configuration, value);

                this.OnConfigurationChanged();
                if (this.configuration != null)
                {
                    this.configuration.PropertyChanged += this.OnConfigurationPropertyChanged;
                }
            }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override double Height => this.Configuration.Height;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override double Width => this.Configuration.Width;

        /// <inheritdoc />
        public override string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this.Configuration);
        }

        /// <inheritdoc />
        public override void SetConfiguration(string jsonConfiguration)
        {
            this.Configuration = JsonConvert.DeserializeObject<TConfig>(jsonConfiguration);
        }

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration = new TConfig();
        }

        /// <inheritdoc />
        protected override void OnConfigurationChanged()
        {
            base.OnConfigurationChanged();

            // RaisePropertyChanged for both Width and Height as they take their values from the configuration
            this.RaisePropertyChanged(nameof(this.Width));
            this.RaisePropertyChanged(nameof(this.Height));
        }

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(string propertyName)
        {
            base.OnConfigurationPropertyChanged(propertyName);
            if (propertyName == nameof(this.configuration.Width))
            {
                // RaisePropertyChanged since this.Width => this.configuration.Width
                this.RaisePropertyChanged(nameof(this.Width));
            }
            else if (propertyName == nameof(this.configuration.Height))
            {
                // RaisePropertyChanged since this.Height => this.configuration.Height
                this.RaisePropertyChanged(nameof(this.Height));
            }
        }

        private void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnConfigurationPropertyChanged(e.PropertyName);
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            this.InitNew();
        }
    }
}