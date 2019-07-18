// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents an audio visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AudioVisualizationObject : PlotVisualizationObject<double, AudioVisualizationObjectConfiguration>
    {
        private RelayCommand enableAudioCommand;

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
                            if (this.Navigator.AudioPlaybackStream == this.Configuration.StreamBinding)
                            {
                                this.Navigator.AudioPlaybackStream = null;
                            }
                            else
                            {
                                this.Navigator.AudioPlaybackStream = this.Configuration.StreamBinding;
                            }
                        });
                }

                return this.enableAudioCommand;
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool IsAudioStream => true;

        /// <summary>
        /// Gets a value indicating whether this stream is currently enabled for audio playback.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsAudioPlaybackStream => this.Configuration.StreamBinding == this.Navigator.AudioPlaybackStream;

        /// <inheritdoc/>
        public override bool CanSnapToStream => false;

        /// <inheritdoc/>
        public override string IconSource
        {
            get
            {
                if (!this.Configuration.StreamBinding.IsBound)
                {
                    return IconSourcePath.StreamUnbound;
                }
                else if (this.IsAudioPlaybackStream)
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
        /// Gets the text for the enable/mute audio playback menu item.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public string EnableAudioCommandText => this.IsAudioPlaybackStream ? "Mute Audio" : "Enable Audio";

        /// <inheritdoc/>
        public override double GetDoubleValue(double data)
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

            // Listen for changes to which stream the navigator is using for audio playback
            this.Navigator.PropertyChanged += this.OnNavigatorPropertyChanged;

            // If there's currently no audio stream set as the playback audio stream, make this stream the one
            if (this.Navigator.AudioPlaybackStream == null)
            {
                this.Navigator.AudioPlaybackStream = this.Configuration.StreamBinding;
            }
        }

        /// <inheritdoc/>
        protected override void OnStreamUnbound()
        {
            this.Navigator.PropertyChanged -= this.OnNavigatorPropertyChanged;
            base.OnStreamUnbound();
        }

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnConfigurationPropertyChanged(sender, e);

            if (e.PropertyName == nameof(AudioVisualizationObjectConfiguration.Channel))
            {
                if (this.Panel != null)
                {
                    // NOTE: Only open a stream when this visualization object is connected to it's parent

                    // Create a new binding with a different channel argument and re-open the stream
                    this.Configuration.StreamBinding.SummarizerArgs = new object[] { this.Configuration.Channel };
                    this.OnStreamBound();
                }
            }
        }

        private void OnNavigatorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Navigator.AudioPlaybackStream))
            {
                this.RaisePropertyChanged(nameof(this.IsAudioPlaybackStream));
                this.RaisePropertyChanged(nameof(this.IconSource));
                this.RaisePropertyChanged(nameof(this.EnableAudioCommandText));
            }
        }
    }
}
