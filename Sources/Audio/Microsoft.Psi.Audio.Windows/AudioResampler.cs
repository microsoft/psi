// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that resamples an audio stream into a different format.
    /// </summary>
    /// <remarks>
    /// This component performs resampling on an audio stream of type <see cref="AudioBuffer"/> to convert it to an
    /// alternative format that may be required for consumption by downstream components. The audio format to convert
    /// to may be specified via the <see cref="AudioResamplerConfiguration.OutputFormat"/> configuration parameter in
    /// the form of a <see cref="WaveFormat"/> or <see cref="WaveFormatEx"/> value.
    /// <br/>
    /// **Please note**: This component uses Media APIs that are available on Windows only.
    /// </remarks>
    public sealed class AudioResampler : ConsumerProducer<AudioBuffer, AudioBuffer>, IDisposable
    {
        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly AudioResamplerConfiguration configuration;

        /// <summary>
        /// The audio resampler.
        /// </summary>
        private MFResampler resampler;

        /// <summary>
        /// The current input format.
        /// </summary>
        private WaveFormat currentInputFormat;

        /// <summary>
        /// The current resampled audio buffer.
        /// </summary>
        private byte[] buffer = null;

        /// <summary>
        /// The originating time of the last posted message.
        /// </summary>
        private DateTime lastOutputPostTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioResampler"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public AudioResampler(Pipeline pipeline, AudioResamplerConfiguration configuration)
            : base(pipeline)
        {
            this.configuration = configuration;
            this.currentInputFormat = configuration.InputFormat;

            // create the audio resampler
            this.resampler = new MFResampler();

            // initialize the resampler
            this.resampler.Initialize(
                this.Configuration.TargetLatencyInMs,
                this.Configuration.InputFormat,
                this.Configuration.OutputFormat,
                this.AudioDataAvailableCallback);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioResampler"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public AudioResampler(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new AudioResamplerConfiguration() : new ConfigurationHelper<AudioResamplerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private AudioResamplerConfiguration Configuration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Disposes the <see cref="AudioResampler"/> object.
        /// </summary>
        public void Dispose()
        {
            if (this.resampler != null)
            {
                this.resampler.Dispose();
                this.resampler = null;
            }
        }

        /// <summary>
        /// Receiver for audio data.
        /// </summary>
        /// <param name="audioBuffer">A buffer containing the next chunk of audio data.</param>
        /// <param name="e">The message envelope for the audio data.</param>
        protected override void Receive(AudioBuffer audioBuffer, Envelope e)
        {
            // take action only if format is different
            if (audioBuffer.HasValidData)
            {
                if (!WaveFormat.Equals(this.currentInputFormat, audioBuffer.Format))
                {
                    this.SetInputFormat(audioBuffer.Format);
                }

                unsafe
                {
                    // pass pointer to audio buffer data directly to MFResampler
                    fixed (void* dataPtr = audioBuffer.Data)
                    {
                        // compute the timestamp at the start of the chunk (originating time is at the end)
                        // Note that timestamp sent to resampler is expressed in ticks and will need to be
                        // converted back to a DateTime when posting the resampled audio.
                        this.resampler.Resample(
                            new IntPtr(dataPtr),
                            audioBuffer.Data.Length,
                            e.OriginatingTime.Ticks - (10000000L * audioBuffer.Length / this.Configuration.InputFormat.AvgBytesPerSec));
                    }
                }
            }
        }

        /// <summary>
        /// Resets the resampler and changes the input audio format.
        /// </summary>
        /// <param name="format">The audio format.</param>
        private void SetInputFormat(WaveFormat format)
        {
            // clone the data as we will be holding onto it beyond this callback
            format.DeepClone(ref this.currentInputFormat);
            this.Configuration.InputFormat = this.currentInputFormat;

            // dispose and re-create the resampler to switch formats
            this.resampler.Dispose();
            this.resampler = new MFResampler();
            this.resampler.Initialize(
                this.Configuration.TargetLatencyInMs,
                this.Configuration.InputFormat,
                this.Configuration.OutputFormat,
                this.AudioDataAvailableCallback);
        }

        /// <summary>
        /// Callback function that is passed to resampler to call whenever it has
        /// new resampled audio data ready and waiting to be read.
        /// </summary>
        /// <param name="data">
        /// Pointer to the native buffer containing the new audio data.
        /// </param>
        /// <param name="length">
        /// The number of bytes of audio data available to be read.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp in 100-ns ticks of the first sample in data.
        /// </param>
        private void AudioDataAvailableCallback(IntPtr data, int length, long timestamp)
        {
            if (length > 0)
            {
                // Only create a new buffer if necessary.
                if ((this.buffer == null) || (this.buffer.Length != length))
                {
                    this.buffer = new byte[length];
                }

                // Copy the data.
                Marshal.Copy(data, this.buffer, 0, length);

                // use the end of the last sample in the packet as the originating time
                // The QPC ticks from the resampler are converted back to a DateTime.
                DateTime originatingTime = new DateTime(
                    timestamp + (10000000L * length / this.Configuration.OutputFormat.AvgBytesPerSec),
                    DateTimeKind.Utc);

                if (originatingTime < this.lastOutputPostTime)
                {
                    // If the input audio packet is larger than the output packet (as determined by the
                    // target latency), then the packet will be split into multiple packets for resampling.
                    // Originating times of the multiple output packets are computed based on the input
                    // packet's originating time and the sample offset of each resampled sub-packet. There
                    // exists a possibility that the last resampled sub-packet on an input packet and the
                    // first resampled sub-packet of the next input packet do not perfectly line up in time.
                    // This could happen if the two consecutive input packets overlap in time, for example
                    // if an automatic system time adjustment occurred between the capture of the two packets.
                    // These adjustments occur from time to time to account for system clock drift w.r.t.
                    // UTC time. In order to ensure that this does not lead to resampled output sub-packets
                    // regressing in time, we manually enforce the output originating time to be no less than
                    // that of the previous packet.
                    originatingTime = this.lastOutputPostTime;
                }

                // post the data to the output stream
                this.Out.Post(new AudioBuffer(this.buffer, this.Configuration.OutputFormat), originatingTime);

                // track the last output originating time
                this.lastOutputPostTime = originatingTime;
            }
        }
    }
}
