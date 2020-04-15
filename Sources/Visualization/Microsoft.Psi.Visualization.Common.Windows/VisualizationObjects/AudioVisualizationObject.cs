// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents an audio visualization object.
    /// </summary>
    [VisualizationObject("Visualize Audio", typeof(AudioSummarizer), IconSourcePath.StreamAudioMuted, IconSourcePath.StreamAudioMuted)]
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
                            if (this.Navigator.AudioPlaybackStreams.Contains(this.StreamBinding))
                            {
                                this.Navigator.RemoveAudioPlaybackStream(this.StreamBinding);
                            }
                            else
                            {
                                this.Navigator.AddAudioPlaybackStream(this.StreamBinding);
                            }
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
                    this.StreamBinding.SummarizerArgs = new object[] { this.Channel };
                    this.OnStreamBound();
                }
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
        public bool IsAudioPlaybackStream => this.Navigator.AudioPlaybackStreams.Contains(this.StreamBinding);

        /// <inheritdoc/>
        public override bool CanSnapToStream => false;

        /// <inheritdoc/>
        public override string IconSource
        {
            get
            {
                if (!this.StreamBinding.IsBound)
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

            // Add this stream as an audio playback stream, if it hasn't already
            if (!this.Navigator.AudioPlaybackStreams.Contains(this.StreamBinding))
            {
                this.Navigator.AddAudioPlaybackStream(this.StreamBinding);
            }
        }

        /// <inheritdoc/>
        protected override void OnStreamUnbound()
        {
            if (this.Navigator.AudioPlaybackStreams.Contains(this.StreamBinding))
            {
                this.Navigator.AudioPlaybackStreams.Remove(this.StreamBinding);
            }

            this.Navigator.PropertyChanged -= this.OnNavigatorPropertyChanged;
            base.OnStreamUnbound();
        }

        private void OnNavigatorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Navigator.AudioPlaybackStreams))
            {
                this.RaisePropertyChanged(nameof(this.IsAudioPlaybackStream));
                this.RaisePropertyChanged(nameof(this.IconSource));
                this.RaisePropertyChanged(nameof(this.EnableAudioCommandText));
            }
        }
    }
}
