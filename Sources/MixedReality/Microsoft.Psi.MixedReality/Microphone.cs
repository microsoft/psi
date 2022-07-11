// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using SKMicrophone = StereoKit.Microphone;

    /// <summary>
    /// Component that captures audio from the microphone.
    /// </summary>
    /// <remarks>Currently only supports 1-channel WAVE_FORMAT_IEEE_FLOAT at 48kHz.</remarks>
    public class Microphone : StereoKitComponent, IProducer<AudioBuffer>, ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly MicrophoneConfiguration configuration;
        private readonly WaveFormat audioFormat;
        private float[] buffer;
        private bool active;

        /// <summary>
        /// Initializes a new instance of the <see cref="Microphone"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="name">An optional name for the component.</param>
        public Microphone(Pipeline pipeline, MicrophoneConfiguration configuration = null, string name = nameof(Microphone))
            : base(pipeline, name)
        {
            this.pipeline = pipeline;
            this.configuration = configuration ?? new MicrophoneConfiguration();
            this.audioFormat = this.configuration.AudioFormat;
            this.buffer = new float[(uint)(this.audioFormat.SamplesPerSec * this.configuration.SamplingInterval.TotalSeconds)];
            this.Out = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Out));
        }

        /// <inheritdoc/>
        public Emitter<AudioBuffer> Out { get; }

        /// <inheritdoc/>
        public override bool Initialize()
        {
            if (!SKMicrophone.Start())
            {
                throw new Exception("Failed to access the system's default microphone.");
            }

            return true;
        }

        /// <inheritdoc/>
        public override void Shutdown() => SKMicrophone.Stop();

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.active = true;
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.active = false;
            notifyCompleted();
        }

        /// <inheritdoc/>
        public unsafe override void Step()
        {
            int unreadSamples = SKMicrophone.Sound.UnreadSamples;
            var originatingTime = this.pipeline.GetCurrentTime();

            // Check if there are samples to capture.
            // Note that we wait for more than 1 unread sample to be available, otherwise the
            // ReadSamples() method below does not work as expected.
            if (this.active && SKMicrophone.IsRecording && unreadSamples > 1)
            {
                // Ensure that the sample buffer is large enough
                if (unreadSamples > this.buffer.Length)
                {
                    this.buffer = new float[unreadSamples];
                }

                // Read the audio samples
                int samples = SKMicrophone.Sound.ReadSamples(ref this.buffer);

                if (samples < unreadSamples && samples < this.buffer.Length)
                {
                    throw new Exception(
                        "Error reading audio samples from the microphone.\n" +
                        $"Expected at least {Math.Min(unreadSamples, this.buffer.Length)} samples but obtained {samples}.");
                }

                // Convert to bytes and post the AudioBuffer
                byte[] audio = new byte[samples * this.audioFormat.BitsPerSample / 8];
                fixed (void* src = this.buffer, dst = audio)
                {
                    Buffer.MemoryCopy(src, dst, audio.Length, audio.Length);
                }

                this.Out.Post(new AudioBuffer(audio, this.audioFormat), originatingTime);
            }
        }
    }
}