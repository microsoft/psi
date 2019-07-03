// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using GalaSoft.MvvmLight.CommandWpf;
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
        // The minimum height of a Visualization Panel
        private const double MinHeight = 10;

        private TConfig configuration;
        private RelayCommand removePanelCommand;
        private RelayCommand clearPanelCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<DragDeltaEventArgs> resizePanelCommand;

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

        /// <summary>
        /// Gets the remove panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemovePanelCommand
        {
            get
            {
                if (this.removePanelCommand == null)
                {
                    this.removePanelCommand = new RelayCommand(() => this.Container.RemovePanel(this));
                }

                return this.removePanelCommand;
            }
        }

        /// <summary>
        /// Gets the clear panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearPanelCommand
        {
            get
            {
                if (this.clearPanelCommand == null)
                {
                    this.clearPanelCommand = new RelayCommand(() => this.Clear());
                }

                return this.clearPanelCommand;
            }
        }

        /// <summary>
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
        {
            get
            {
                if (this.mouseLeftButtonDownCommand == null)
                {
                    this.mouseLeftButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        e =>
                        {
                            // Set the current panel on click
                            if (!this.IsCurrentPanel)
                            {
                                this.Container.CurrentPanel = this;
                            }
                        });
                }

                return this.mouseLeftButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets the resize panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<DragDeltaEventArgs> ResizePanelCommand
        {
            get
            {
                if (this.resizePanelCommand == null)
                {
                    this.resizePanelCommand = new RelayCommand<DragDeltaEventArgs>(o => this.Configuration.Height = Math.Max(this.Configuration.Height + o.VerticalChange, MinHeight));
                }

                return this.resizePanelCommand;
            }
        }

        /// <inheritdoc />
        protected override void InitNew()
        {
            this.Configuration = new TConfig();
            base.InitNew();
        }

        /// <inheritdoc />
        protected override void OnConfigurationChanged()
        {
            // RaisePropertyChanging for both Width and Height as they take their values from the configuration
            this.RaisePropertyChanging(nameof(this.Width));
            this.RaisePropertyChanging(nameof(this.Height));

            base.OnConfigurationChanged();

            // RaisePropertyChanged for both Width and Height as they take their values from the configuration
            this.RaisePropertyChanged(nameof(this.Width));
            this.RaisePropertyChanged(nameof(this.Height));
        }

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.configuration.Width))
            {
                // RaisePropertyChanging since this.Width => this.configuration.Width
                this.RaisePropertyChanging(nameof(this.Width));
            }
            else if (e.PropertyName == nameof(this.configuration.Height))
            {
                // RaisePropertyChanging since this.Height => this.configuration.Height
                this.RaisePropertyChanging(nameof(this.Height));
            }

            base.OnConfigurationPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.configuration.Width))
            {
                // RaisePropertyChanged since this.Width => this.configuration.Width
                this.RaisePropertyChanged(nameof(this.Width));
            }
            else if (e.PropertyName == nameof(this.configuration.Height))
            {
                // RaisePropertyChanged since this.Height => this.configuration.Height
                this.RaisePropertyChanged(nameof(this.Height));
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            this.InitNew();
        }
    }
}