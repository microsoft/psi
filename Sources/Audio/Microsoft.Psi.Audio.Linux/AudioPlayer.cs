// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that plays back an audio stream to an output device such as the speakers.
    /// </summary>
    /// <remarks>
    /// This output component renders an audio input stream of type <see cref="AudioBuffer"/> to the
    /// default or other specified audio output device for playback. The audio device on which to
    /// playback the output may be specified by name via the <see cref="AudioPlayerConfiguration.DeviceName"/>
    /// configuration parameter.
    /// </remarks>
    public sealed class AudioPlayer : SimpleConsumer<AudioBuffer>, IDisposable
    {
        private readonly Pipeline pipeline;

        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly AudioPlayerConfiguration configuration;

        /// <summary>
        /// Number of bytes per audio frame.
        /// </summary>
        private readonly int frameSize;

        /// <summary>
        /// The audio capture device.
        /// </summary>
        private LinuxAudioInterop.AudioDevice audioDevice;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        /// <param name="name">An optional name for this component.</param>
        public AudioPlayer(Pipeline pipeline, AudioPlayerConfiguration configuration, string name = nameof(AudioPlayer))
            : base(pipeline, name)
        {
            pipeline.PipelineRun += (s, e) => this.OnPipelineRun();
            this.In.Unsubscribed += _ => this.OnUnsubscribed();
            this.pipeline = pipeline;
            this.configuration = configuration;
            this.frameSize = this.configuration.Format.Channels * configuration.Format.BitsPerSample / 8;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public AudioPlayer(Pipeline pipeline, string configurationFilename = null)
            : this(pipeline, (configurationFilename == null) ? new AudioPlayerConfiguration() : new ConfigurationHelper<AudioPlayerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Receiver for the audio data.
        /// </summary>
        /// <param name="audioData">A buffer containing the next chunk of audio data.</param>
        public override void Receive(Message<AudioBuffer> audioData)
        {
            // take action only if format is different
            if (this.audioDevice != null && audioData.Data.HasValidData)
            {
                var data = audioData.Data;
                LinuxAudioInterop.Write(this.audioDevice, data.Data, data.Length / this.frameSize);
            }
        }

        /// <summary>
        /// Disposes the <see cref="AudioPlayer"/> object.
        /// </summary>
        public void Dispose()
        {
            this.OnUnsubscribed();
        }

        /// <summary>
        /// Starts playing back audio.
        /// </summary>
        private void OnPipelineRun()
        {
            this.audioDevice = LinuxAudioInterop.Open(
                this.configuration.DeviceName,
                LinuxAudioInterop.Mode.Playback,
                (int)this.configuration.Format.SamplesPerSec,
                this.configuration.Format.Channels,
                LinuxAudioInterop.ConvertFormat(this.configuration.Format));
        }

        /// <summary>
        /// Stops playing back audio.
        /// </summary>
        private void OnUnsubscribed()
        {
            if (this.audioDevice != null)
            {
                LinuxAudioInterop.Close(this.audioDevice);
                this.audioDevice = null;
            }
        }
    }
}
