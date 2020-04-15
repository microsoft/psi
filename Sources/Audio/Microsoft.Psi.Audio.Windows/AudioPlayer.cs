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
    /// configuration parameter. The <see cref="GetAvailableDevices"/> static method may be used to
    /// enumerate the names of audio output devices currently available on the system.
    /// <br/>
    /// **Please note**: This component uses Audio APIs that are available on Windows only.
    /// </remarks>
    public sealed class AudioPlayer : SimpleConsumer<AudioBuffer>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly AudioPlayerConfiguration configuration;
        private WaveFormat currentInputFormat;

        /// <summary>
        /// The audio render device.
        /// </summary>
        private WasapiRender wasapiRender;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public AudioPlayer(Pipeline pipeline, AudioPlayerConfiguration configuration)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.configuration = configuration;
            this.currentInputFormat = configuration.InputFormat;
            this.AudioLevelInput = pipeline.CreateReceiver<double>(this, this.SetAudioLevel, nameof(this.AudioLevelInput));
            this.AudioLevel = pipeline.CreateEmitter<double>(this, nameof(this.AudioLevel));

            this.wasapiRender = new WasapiRender();
            this.wasapiRender.Initialize(configuration.DeviceName);

            if (configuration.AudioLevel >= 0)
            {
                this.wasapiRender.AudioLevel = configuration.AudioLevel;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public AudioPlayer(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new AudioPlayerConfiguration() : new ConfigurationHelper<AudioPlayerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the receiver for the audio level stream which controls the volume of the output.
        /// </summary>
        public Receiver<double> AudioLevelInput { get; }

        /// <summary>
        /// Gets the stream containing the output audio level.
        /// </summary>
        public Emitter<double> AudioLevel { get; }

        /// <summary>
        /// Gets a list of available audio render devices.
        /// </summary>
        /// <returns>
        /// An array of available render device names.
        /// </returns>
        public static string[] GetAvailableDevices()
        {
            return WasapiRender.GetAvailableRenderDevices();
        }

        /// <summary>
        /// Sets the audio output level.
        /// </summary>
        /// <param name="level">The audio level.</param>
        public void SetAudioLevel(Message<double> level)
        {
            this.wasapiRender.AudioLevel = (float)level.Data;
        }

        /// <summary>
        /// Receiver for the audio data.
        /// </summary>
        /// <param name="audioData">A buffer containing the next chunk of audio data.</param>
        public override void Receive(Message<AudioBuffer> audioData)
        {
            // take action only if format is different
            if (audioData.Data.HasValidData)
            {
                if (!WaveFormat.Equals(audioData.Data.Format, this.currentInputFormat))
                {
                    // Make a copy of the new input format (don't just use a direct reference,
                    // as the object graph of the Message.Data will be reclaimed by the runtime).
                    audioData.Data.Format.DeepClone(ref this.currentInputFormat);
                    this.configuration.InputFormat = this.currentInputFormat;

                    // stop and restart the renderer to switch formats
                    this.wasapiRender.StopRendering();
                    this.wasapiRender.StartRendering(
                        this.configuration.BufferLengthSeconds,
                        this.configuration.TargetLatencyInMs,
                        this.configuration.Gain,
                        this.configuration.InputFormat);
                }

                // Append the audio buffer to the audio renderer
                this.wasapiRender.AppendAudio(audioData.Data.Data, false);
            }
        }

        /// <summary>
        /// Disposes the <see cref="AudioPlayer"/> object.
        /// </summary>
        public void Dispose()
        {
            this.StopRendering();
            this.wasapiRender.Dispose();
            this.wasapiRender = null;
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // Notify that this is an infinite source component. It is a source because it
            // posts changes to the volume level of the output audio device to the AudioLevel
            // stream for as long as the component is active.
            notifyCompletionTime(DateTime.MaxValue);

            // publish initial volume level at startup
            this.AudioLevel.Post(this.wasapiRender.AudioLevel, this.pipeline.GetCurrentTime());

            // register the volume changed notification event handler
            this.wasapiRender.AudioVolumeNotification += this.HandleVolumeChangedNotification;

            // start the audio renderer
            this.wasapiRender.StartRendering(
                this.configuration.BufferLengthSeconds,
                this.configuration.TargetLatencyInMs,
                this.configuration.Gain,
                this.configuration.InputFormat);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.StopRendering();
            notifyCompleted();
        }

        private void StopRendering()
        {
            this.wasapiRender.StopRendering();

            // Unsubscribe from volume change notifications. This will cause the component to
            // stop posting changes to the volume level on the AudioLevel stream.
            this.wasapiRender.AudioVolumeNotification -= this.HandleVolumeChangedNotification;
        }

        /// <summary>
        /// Handles volume changed notification events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="AudioVolumeEventArgs"/> that contains the event data.</param>
        private void HandleVolumeChangedNotification(object sender, AudioVolumeEventArgs e)
        {
            this.AudioLevel.Post(e.MasterVolume, this.pipeline.GetCurrentTime());
        }
    }
}
