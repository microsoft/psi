// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.Windows;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements an audio visualization object.
    /// </summary>
    [VisualizationObject("Audio", typeof(AudioSummarizer), IconSourcePath.StreamAudioMuted, IconSourcePath.StreamAudioMuted)]
    public class AudioVisualizationObject : PlotVisualizationObject<double>
    {
        private RelayCommand enableAudioCommand;
        private RelayCommand exportAudioCommand;
        private RelayCommand exportAudioSelectionCommand;
        private bool playDisplayChannelOnly;
        private WaveFormat audioFormat;

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
        public RelayCommand EnableAudioCommand =>
            this.enableAudioCommand ??= new RelayCommand(
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

        /// <summary>
        /// Gets the export audio command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExportAudioCommand =>
            this.exportAudioCommand ??= new RelayCommand(
                () => this.ExportAudio(null));

        /// <summary>
        /// Gets the export selected audio command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExportAudioSelectionCommand =>
            this.exportAudioSelectionCommand ??= new RelayCommand(
                () => this.ExportAudio(this.Navigator.SelectionRange.AsTimeInterval),
                () => this.Navigator.SelectionRange.StartTime != DateTime.MinValue && this.Navigator.SelectionRange.EndTime != DateTime.MaxValue);

        /// <summary>
        /// Gets or sets the audio channel to plot.
        /// </summary>
        [DataMember]
        [DisplayName("Display Channel")]
        [Description("The audio channel to display.")]
        [PropertyOrder(0)]
        public short Channel
        {
            get
            {
                return this.channel;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.Channel), "Channel must be greater than or equal to 0.");
                }

                if (value >= this.ChannelCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.Channel), $"Channel must be less than the number of channels ({this.ChannelCount}) in the audio stream.");
                }

                this.Set(nameof(this.Channel), ref this.channel, value);

                if (this.Panel != null)
                {
                    // NOTE: Only open a stream when this visualization object is connected to its parent

                    // Create a new binding with a different channel argument and update the stream
                    this.StreamBinding = new StreamBinding(
                        this.StreamBinding.SourceStreamName,
                        this.StreamBinding.PartitionName,
                        this.StreamBinding.StreamName,
                        this.StreamBinding.DerivedStreamAdapterType,
                        this.StreamBinding.DerivedStreamAdapterArguments,
                        typeof(AudioChannelAdapter),
                        new object[] { this.Channel },
                        this.StreamBinding.VisualizerSummarizerType,
                        null);

                    // Update the stream source to refresh the data
                    this.UpdateStreamSource(VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to play only the displayed channel.
        /// </summary>
        [DataMember]
        [DisplayName("Play Display Channel Only")]
        [Description("Play only the displayed channel.")]
        [PropertyOrder(1)]
        public bool PlayDisplayChannelOnly
        {
            get => this.playDisplayChannelOnly;
            set
            {
                this.Set(nameof(this.PlayDisplayChannelOnly), ref this.playDisplayChannelOnly, value);

                // Restart playback if this is an audio playback source
                if (this.Navigator != null && this.Navigator.IsAudioPlaybackVisualizationObject(this))
                {
                    this.Navigator.AddOrUpdateAudioPlaybackSource(this, this.StreamSource);
                }
            }
        }

        /// <summary>
        /// Gets the number of channels in the audio stream.
        /// </summary>
        [Browsable(true)]
        [DisplayName("Channel Count")]
        [Description("The number of audio channels.")]
        [IgnoreDataMember]
        [PropertyOrder(2)]
        public int? ChannelCount => this.audioFormat?.Channels;

        /// <summary>
        /// Gets the sampling rate of the audio stream.
        /// </summary>
        [Browsable(true)]
        [DisplayName("Sampling Rate")]
        [Description("The audio sampling rate.")]
        [IgnoreDataMember]
        [PropertyOrder(3)]
        public uint? SamplingRate => this.audioFormat?.SamplesPerSec;

        /// <summary>
        /// Gets the format of the audio stream.
        /// </summary>
        [Browsable(true)]
        [DisplayName("Format")]
        [Description("The audio format.")]
        [IgnoreDataMember]
        [PropertyOrder(4)]
        public WaveFormatTag? Format => this.audioFormat?.FormatTag;

        /// <summary>
        /// Gets the audio format for the visualized stream.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public WaveFormat AudioFormat => this.audioFormat;

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
                new (this.ContextMenuIconSource, this.EnableAudioCommandText, this.EnableAudioCommand),
                new (null, $"Export {this.Name} stream to wav file", this.ExportAudioCommand),
            };

            if (this.Navigator.SelectionRange.StartTime != DateTime.MinValue && this.Navigator.SelectionRange.EndTime != DateTime.MaxValue)
            {
                items.Add(new (null, $"Export {this.Name} stream selection to wav file", this.ExportAudioSelectionCommand));
            }

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

            if (this.audioFormat == null)
            {
                this.ReadAudioFormat();
            }

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

            this.audioFormat = null;

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

        private void ReadAudioFormat()
        {
            using var streamReader = StreamReader.Create(this.StreamSource.StoreName, this.StreamSource.StorePath, this.StreamSource.StreamReaderType);
            streamReader.OpenStream<AudioBuffer>(
                this.StreamSource.StreamName,
                (data, envelope) =>
                {
                    this.audioFormat = data.Format;
                });

            // Read the first message to get the audio format
            streamReader.Seek(TimeInterval.Infinite);
            streamReader.MoveNext(out _);
        }

        private void ExportAudio(TimeInterval interval)
        {
            var getFilenameDialog = new GetParameterWindow(Application.Current.MainWindow, "Export Audio to wav file", "Wav file name", string.Empty);

            if (getFilenameDialog.ShowDialog() == true)
            {
                using var p = Pipeline.Create("ExportAudio", DeliveryPolicy.Throttle);
                var store = PsiStore.Open(p, this.StreamSource.StoreName, this.StreamSource.StorePath);
                var audio = store.OpenStream<AudioBuffer>(this.StreamSource.StreamName);
                var wavFileWriter = new WaveFileWriter(p, getFilenameDialog.ParameterValue);
                audio.PipeTo(wavFileWriter);
                var replayDescriptor = interval == null ? ReplayDescriptor.ReplayAll : new ReplayDescriptor(interval);
                p.Run(replayDescriptor);
            }
        }
    }
}
