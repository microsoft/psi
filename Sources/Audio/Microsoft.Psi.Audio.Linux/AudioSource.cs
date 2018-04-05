// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that implements an audio source which captures live audio from an input device such as a microphone.
    /// </summary>
    /// <remarks>
    /// This sensor component produces an audio output stream of type <see cref="AudioBuffer"/> which may be piped to
    /// downstream components for further processing and optionally saved to a data store. The audio input device from
    /// which to capture may be specified via the <see cref="AudioConfiguration.DeviceName"/> configuration
    /// parameter (e.g. "plughw:0,0").
    /// </remarks>
    public sealed class AudioSource : IProducer<AudioBuffer>, IStartable, IDisposable
    {
        private readonly Pipeline pipeline;

        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly AudioConfiguration configuration;

        /// <summary>
        /// The output stream of audio buffers.
        /// </summary>
        private readonly Emitter<AudioBuffer> audioBuffers;

        /// <summary>
        /// The audio capture device
        /// </summary>
        private LinuxAudioInterop.AudioDevice audioDevice;

        /// <summary>
        /// The current audio capture buffer.
        /// </summary>
        private AudioBuffer buffer;

        /// <summary>
        /// Keep track of the timestamp of the last audio buffer (computed from the value reported to us by the capture driver).
        /// </summary>
        private DateTime lastPostedAudioTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public AudioSource(Pipeline pipeline, AudioConfiguration configuration)
        {
            this.pipeline = pipeline;
            this.configuration = configuration;
            this.audioBuffers = pipeline.CreateEmitter<AudioBuffer>(this, "AudioBuffers");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public AudioSource(Pipeline pipeline, string configurationFilename = null)
            : this(pipeline, (configurationFilename == null) ? new AudioConfiguration() : new ConfigurationHelper<AudioConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the output stream of audio buffers.
        /// </summary>
        public Emitter<AudioBuffer> Out
        {
            get { return this.audioBuffers; }
        }

        /// <summary>
        /// Gets the name of the audio device.
        /// </summary>
        public string AudioDeviceName
        {
            get { return this.configuration.DeviceName; }
        }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private AudioConfiguration Configuration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Called to start capturing audio from the microphone.
        /// </summary>
        /// <param name="onCompleted">Delegate to call when the execution completed</param>
        /// <param name="descriptor">If set, describes the playback constraints</param>
        public void Start(Action onCompleted, ReplayDescriptor descriptor)
        {
            this.audioDevice = LinuxAudioInterop.Open(
                this.configuration.DeviceName,
                LinuxAudioInterop.Mode.Capture,
                (int)this.configuration.Format.SamplesPerSec,
                this.configuration.Format.Channels,
                LinuxAudioInterop.ConfigurationFormat(this.configuration));

            new Thread(new ThreadStart(() =>
            {
                const int blockSize = 256;
                var format = this.configuration.Format;
                var length = blockSize * format.BitsPerSample / 8;
                var buf = new byte[length];

                while (this.audioDevice != null)
                {
                    try
                    {
                        LinuxAudioInterop.Read(this.audioDevice, buf, blockSize);
                    }
                    catch (Exception ex)
                    {
                        if (this.audioDevice != null)
                        {
                            throw ex;
                        }
                    }

                    // Only create a new buffer if necessary
                    if ((this.buffer.Data == null) || (this.buffer.Length != length))
                    {
                        this.buffer = new AudioBuffer(length, format);
                    }

                    // Copy the data
                    Array.Copy(buf, this.buffer.Data, length);

                    // use the end of the last sample in the packet as the originating time
                    DateTime originatingTime = this.pipeline.GetCurrentTime().AddSeconds(length / format.AvgBytesPerSec);

                    // post the data to the output stream
                    this.audioBuffers.Post(this.buffer, originatingTime);
                }
            })) { IsBackground = true }.Start();
        }

        /// <summary>
        /// Called when the pipeline is shutting down.
        /// </summary>
        public void Stop()
        {
            var audioDevice = Interlocked.Exchange(ref this.audioDevice, null);
            if (audioDevice != null)
            {
                LinuxAudioInterop.Close(audioDevice);
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            this.Stop();
        }
    }
}
