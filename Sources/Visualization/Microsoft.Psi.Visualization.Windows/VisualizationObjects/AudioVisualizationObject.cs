// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements an audio visualization object.
    /// </summary>
    [VisualizationObject("Audio", typeof(AudioSummarizer), IconSourcePath.StreamAudioMuted, IconSourcePath.StreamAudioMuted)]
    public class AudioVisualizationObject : PlotVisualizationObject<double>
    {
        private RelayCommand enableAudioCommand;

        /// <summary>
        /// The audio channel to plot.
        /// </summary>
        private short channel;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(AudioVisualizationObjectView));

        /// <summary>
        /// Gets the enable audio command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand EnableAudioCommand
        {
            get
            {
                if (this.enableAudioCommand == null)
                {
                    this.enableAudioCommand = new RelayCommand(
                        () =>
                        {
                            this.RaisePropertyChanging(nameof(this.IconSource));
                            this.RaisePropertyChanging(nameof(this.EnableAudioCommandText));

                            if (this.Navigator.IsAudioPlaybackVisualizationObject(this))
                            {
                                this.Navigator.RemoveAudioPlaybackSource(this);
                            }
                            else
                            {
                                this.Navigator.AddOrUpdateAudioPlaybackSource(this, this.StreamSource);
                            }

                            this.RaisePropertyChanged(nameof(this.IconSource));
                            this.RaisePropertyChanged(nameof(this.EnableAudioCommandText));
                        });
                }

                return this.enableAudioCommand;
            }
        }

        /// <summary>
        /// Gets or sets the audio channel to plot.
        /// </summary>
        [DataMember]
        public short Channel
        {
            get
            {
                return this.channel;
            }

            set
            {
                this.Set(nameof(this.Channel), ref this.channel, value);

                if (this.Panel != null)
                {
                    // NOTE: Only open a stream when this visualization object is connected to it's parent

                    // Create a new binding with a different channel argument and re-open the stream
                    this.StreamBinding = new StreamBinding(
                        this.StreamBinding.SourceStreamName,
                        this.StreamBinding.PartitionName,
                        this.StreamBinding.StreamName,
                        this.StreamBinding.DerivedStreamAdapterType,
                        this.StreamBinding.DerivedStreamAdapterArguments,
                        this.StreamBinding.VisualizerStreamAdapterType,
                        this.StreamBinding.VisualizerStreamAdapterArguments,
                        this.StreamBinding.VisualizerSummarizerType,
                        new object[] { this.Channel });

                    this.OnStreamBound();
                }
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool IsAudioStream => true;

        /// <inheritdoc/>
        public override bool CanSnapToStream => false;

        /// <inheritdoc/>
        public override string IconSource
        {
            get
            {
                if (!this.IsBound)
                {
                    return IconSourcePath.StreamUnbound;
                }
                else if (this.Navigator.IsAudioPlaybackVisualizationObject(this))
                {
                    return this.IsLive ? IconSourcePath.StreamAudioLive : IconSourcePath.StreamAudio;
                }
                else
                {
                    return this.IsLive ? IconSourcePath.StreamAudioMutedLive : IconSourcePath.StreamAudioMuted;
                }
            }
        }

        /// <summary>
        /// Gets the icon to display in the context menu.  If audio is enabled, then we return the audio muted icon etc.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public string ContextMenuIconSource => this.Navigator.IsAudioPlaybackVisualizationObject(this) ? IconSourcePath.StreamAudioMuted : IconSourcePath.StreamAudio;

        /// <summary>
        /// Gets the text for the enable/mute audio playback menu item.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public string EnableAudioCommandText => this.Navigator.IsAudioPlaybackVisualizationObject(this) ? $"Mute {this.Name}" : $"Enable {this.Name}";

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = new List<ContextMenuItemInfo>()
            {
                new ContextMenuItemInfo(this.ContextMenuIconSource, this.EnableAudioCommandText, this.EnableAudioCommand),
            };

            items.AddRange(base.ContextMenuItemsInfo());
            return items;
        }

        /// <inheritdoc/>
        public override double GetNumericValue(double data)
        {
            return data;
        }

        /// <inheritdoc/>
        public override string GetStringValue(double data)
        {
            return data.ToString();
        }

        /// <inheritdoc/>
        protected override void OnStreamBound()
        {
            base.OnStreamBound();

            // If this audio visualization object is an audio playback source, notify the navigator of the new stream source.
            if (this.Navigator.IsAudioPlaybackVisualizationObject(this))
            {
                this.Navigator.AddOrUpdateAudioPlaybackSource(this, this.StreamSource);
            }
        }

        /// <inheritdoc/>
        protected override void OnStreamUnbound()
        {
            // If this audio visualization object is an audio playback source, notify the navigator that we're no longer bound.
            if (this.Navigator.IsAudioPlaybackVisualizationObject(this))
            {
                this.Navigator.AddOrUpdateAudioPlaybackSource(this, null);
            }

            base.OnStreamUnbound();
        }

        /// <inheritdoc/>
        protected override void OnAddToPanel()
        {
            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.EnableAudioCommandText));

            this.Navigator.AddOrUpdateAudioPlaybackSource(this, this.StreamSource);

            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.EnableAudioCommandText));

            base.OnAddToPanel();
        }

        /// <inheritdoc/>
        protected override void OnRemoveFromPanel()
        {
            if (this.Navigator.IsAudioPlaybackVisualizationObject(this))
            {
                this.RaisePropertyChanging(nameof(this.IconSource));
                this.RaisePropertyChanging(nameof(this.EnableAudioCommandText));

                this.Navigator.RemoveAudioPlaybackSource(this);

                this.RaisePropertyChanged(nameof(this.IconSource));
                this.RaisePropertyChanged(nameof(this.EnableAudioCommandText));
            }

            base.OnRemoveFromPanel();
        }
    }
}
