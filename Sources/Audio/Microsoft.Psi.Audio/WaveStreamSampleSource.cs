// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that produces on-demand an audio sample specified by a <see cref="System.IO.Stream"/>.
    /// </summary>
    /// <remarks>
    /// This is meant for relatively short sound effects cached in memory.
    /// We consume the stream given at construction time; breaking it into
    /// audio buffers which are "played" upon receiving a true input signal.
    /// </remarks>
    public class WaveStreamSampleSource : IConsumerProducer<bool, AudioBuffer>
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly AudioBuffer[] audioData;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveStreamSampleSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="stream">Audio stream in WAVE format (48KHz, 1-channel, IEEE Float).</param>
        /// <param name="name">An optional name for this component.</param>
        public WaveStreamSampleSource(Pipeline pipeline, Stream stream, string name = nameof(WaveStreamSampleSource))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.In = pipeline.CreateReceiver<bool>(this, this.Play, nameof(this.In));
            this.Out = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Out));

            var reader = new BinaryReader(stream);
            var inputFormat = WaveFileHelper.ReadWaveFileHeader(reader);

            // we don't do resampling or conversion (must be 1-channel, 48kHz, float32).
            // convert offline if needed: ffmpeg -i foo.wav -f wav -acodec pcm_f32le -ar 48000 -ac 1 bar.wav
            if (inputFormat.Channels != 1 ||
                inputFormat.SamplesPerSec != 48000 ||
                (inputFormat.FormatTag != WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT &&
                 inputFormat.FormatTag != WaveFormatTag.WAVE_FORMAT_EXTENSIBLE) ||
                inputFormat.BitsPerSample != 32)
            {
                throw new ArgumentException("Expected 1-channel, 48kHz, float32 audio format.");
            }

            // break into 1 second audio buffers
            var outputFormat = WaveFormat.CreateIeeeFloat(48000, 1);
            var dataLength = WaveFileHelper.ReadWaveDataLength(reader);

            // stepping over this line computing frames (e.g. F10) in the debugger will throw - still trying to understand why
            var frames = (int)Math.Ceiling((double)dataLength / (double)outputFormat.AvgBytesPerSec);
            this.audioData = new AudioBuffer[frames];
            for (var i = 0; dataLength > 0; i++)
            {
                var count = (int)Math.Min(dataLength, outputFormat.AvgBytesPerSec);
                var bytes = reader.ReadBytes(count);
                this.audioData[i] = new AudioBuffer(bytes, outputFormat);
                dataLength -= count;
            }
        }

        /// <summary>
        /// Gets the receiver of a signal indicating whether to play a sound.
        /// </summary>
        public Receiver<bool> In { get; private set; }

        /// <summary>
        /// Gets the stream of sound output.
        /// </summary>
        public Emitter<AudioBuffer> Out { get; private set; }

        private void Play(bool play)
        {
            if (play)
            {
                var now = this.pipeline.GetCurrentTime();
                if (now < this.Out.LastEnvelope.OriginatingTime)
                {
                    // overlapping with last time played (play after)
                    now = this.Out.LastEnvelope.OriginatingTime.AddTicks(1);
                }

                for (var i = 0; i < this.audioData.Length; i++)
                {
                    this.Out.Post(this.audioData[i], now + TimeSpan.FromSeconds(i));
                }
            }
        }
    }
}
