// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.MediaCapture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Windows.Foundation;
    using Windows.Media;
    using Windows.Media.Capture;
    using Windows.Media.Capture.Frames;
    using Windows.Media.MediaProperties;
    using AudioBuffer = Microsoft.Psi.Audio.AudioBuffer;

    /// <summary>
    /// Source component for a <see cref="MediaCapture"/> microphone.
    /// </summary>
    public class Microphone : ISourceComponent, IProducer<AudioBuffer>, IDisposable
    {
        private readonly MicrophoneConfiguration configuration;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly Task initMediaCaptureTask;

        /// <summary>
        /// Gets the AudioEncodingProperties subtype as a WaveFormatTag.
        /// Please see the link below for the most recently updated list of subtypes:
        /// https://docs.microsoft.com/en-us/uwp/api/windows.media.mediaproperties.audioencodingproperties.subtype?view=winrt-22621.
        /// </summary>
        private readonly Dictionary<string, WaveFormatTag> findSubtypeAsWaveFormatTag = new ()
        {
            // Advanced Audio Coding (AAC). The stream can contain either raw AAC data or AAC data in an Audio Data Transport Stream (ADTS) stream.
            { "AAC", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Dolby Digital audio (AC-3).
            { "AC3", WaveFormatTag.WAVE_FORMAT_DOLBY_AC3_SPDIF },

            // Advanced Audio Coding (AAC) audio in Audio Data Transport Stream (ADTS) format.
            { "AACADTS", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // AAC in High-Bandwidth Digital Content Protection (HDCP) format.
            { "AACHDCP", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Dolby AC-3 audio over Sony/Philips Digital Interface (S/PDIF).
            { "AC3SPDIF", WaveFormatTag.WAVE_FORMAT_DOLBY_AC3_SPDIF },

            // Dolby AC-3 in High-Bandwidth Digital Content Protection (HDCP) format.
            { "AC3HDCP", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Audio Data Transport Stream
            { "ADTS", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Apple Lossless Audio Codec
            { "ALAC", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Adaptive Multi-Rate audio codec (AMR-NB)
            { "AMRNB", WaveFormatTag.WAVE_FORMAT_NOKIA_ADAPTIVE_MULTIRATE },

            // Adaptive Multi-Rate Wideband audio codec (AMR-WB)
            { "AWRWB", WaveFormatTag.WAVE_FORMAT_GSM_ADAPTIVE_MULTIRATE_WB },

            // Digital Theater Systems (DTS)
            { "DTS", WaveFormatTag.WAVE_FORMAT_DTS },

            // Dolby Digital Plus audio (E-AC-3).
            { "EAC3", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Free Lossless Audio Codec
            { "FLAC", WaveFormatTag.WAVE_FORMAT_FLAC },

            // Uncompressed 32-bit float PCM audio.
            { "Float", WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT },

            // MPEG Audio Layer-3 (MP3).
            { "MP3", WaveFormatTag.WAVE_FORMAT_MPEGLAYER3 },

            // MPEG-1 audio payload.
            { "MPEG", WaveFormatTag.WAVE_FORMAT_MPEG },

            // Opus
            { "OPUS", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Uncompressed 16-bit PCM audio.
            { "PCM", WaveFormatTag.WAVE_FORMAT_PCM },

            // Windows Media Audio 8 codec, Windows Media Audio 9 codec, or Windows Media Audio 9.1 codec.
            { "WMA8", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Windows Media Audio 9 Professional codec or Windows Media Audio 9.1 Professional codec.
            { "WMA9", WaveFormatTag.WAVE_FORMAT_UNKNOWN },

            // Vorbis codec
            { "Vorbis", WaveFormatTag.WAVE_FORMAT_UNKNOWN },
        };

        private MediaCapture mediaCapture;
        private MediaFrameReader audioFrameReader;
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> audioFrameHandler;

        private WaveFormat audioFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Microphone"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="name">An optional name for the component.</param>
        public Microphone(Pipeline pipeline, MicrophoneConfiguration configuration = null, string name = nameof(Microphone))
        {
            this.name = name;
            this.pipeline = pipeline;
            this.configuration = configuration ?? new MicrophoneConfiguration();

            this.Out = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Out));

            // Call this here (rather than in the Start() method, which is executed on the thread pool) to
            // ensure that MediaCapture.InitializeAsync() is called from an STA thread (this constructor must
            // itself be called from an STA thread in order for this to be true). Calls from an MTA thread may
            // result in undefined behavior, per the following documentation:
            // https://docs.microsoft.com/en-us/uwp/api/windows.media.capture.mediacapture.initializeasync
            this.initMediaCaptureTask = this.InitializeMediaCaptureAsync();
        }

        /// <summary>
        /// Gets the microphone's audio buffer stream.
        /// </summary>
        public Emitter<AudioBuffer> Out { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            // ensure mediaCapture isn't disposed during initialization task
            this.initMediaCaptureTask.Wait();

            if (this.mediaCapture != null)
            {
                this.mediaCapture.Dispose();
                this.mediaCapture = null;

                // Reset the audio properties for the current media capture.
                this.audioFormat = null;
            }
        }

        /// <inheritdoc/>
        public async void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Ensure that media capture initialization has finished
            await this.initMediaCaptureTask;

            // Start the media frame reader for the audio stream, if configured
            if (this.audioFrameReader != null)
            {
                var status = await this.audioFrameReader.StartAsync();
                if (status != MediaFrameReaderStartStatus.Success)
                {
                    throw new InvalidOperationException($"Audio stream media frame reader failed to start: {status}");
                }

                this.audioFrameHandler = this.CreateMediaFrameHandler(
                    this.configuration,
                    this.Out);

                this.audioFrameReader.FrameArrived += this.audioFrameHandler;
            }
        }

        /// <inheritdoc/>
        public async void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.audioFrameReader != null)
            {
                this.audioFrameReader.FrameArrived -= this.audioFrameHandler;

                await this.audioFrameReader.StopAsync();
                this.audioFrameReader.Dispose();
                this.audioFrameReader = null;
            }

            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Initializes the MediaCapture object and creates the MediaFrameReader for the default audio stream.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeMediaCaptureAsync()
        {
            // Specify the media capture settings for the requested capture configuration
            var settings = new MediaCaptureInitializationSettings
            {
                AudioProcessing = AudioProcessing.Default,
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            };

            // Initialize the MediaCapture object
            this.mediaCapture = new MediaCapture();
            await this.mediaCapture.InitializeAsync(settings);

            this.audioFrameReader = await this.CreateMediaFrameReaderAsync(MediaStreamType.Audio);

            if (this.audioFrameReader == null)
            {
                throw new InvalidOperationException("Could not create a frame reader for the requested audio source.");
            }
        }

        /// <summary>
        /// Creates a MediaFrameReader from the first frame source for the given target stream type.
        /// </summary>
        /// <param name="targetStreamType">The requested capture stream type.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<MediaFrameReader> CreateMediaFrameReaderAsync(MediaStreamType targetStreamType)
        {
            foreach (var sourceInfo in this.mediaCapture.FrameSources
                .Where(si => si.Value.Info.MediaStreamType == targetStreamType))
            {
                var frameSource = this.mediaCapture.FrameSources[sourceInfo.Value.Info.Id];
                return await this.mediaCapture.CreateFrameReaderAsync(frameSource);
            }

            // No frame source was found for the requested stream type.
            return null;
        }

        /// <summary>
        /// Creates an event handler that handles the FrameArrived event of the MediaFrameReader.
        /// </summary>
        /// <param name="audioSettings">The stream settings.</param>
        /// <param name="audioStream">The stream on which to post the audio buffer.</param>
        /// <returns>The event handler.</returns>
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> CreateMediaFrameHandler(
            MicrophoneConfiguration audioSettings,
            Emitter<AudioBuffer> audioStream)
        {
            return (sender, args) =>
            {
                using var frame = sender.TryAcquireLatestFrame();
                if (frame != null)
                {
                    // Convert frame QPC time to pipeline time
                    var frameTimestamp = frame.SystemRelativeTime.Value.Ticks;
                    var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(frameTimestamp);

                    // Post the audio buffer stream if requested
                    if (audioSettings.OutputAudio)
                    {
                        using MediaFrameReference mediaFrame = frame.AudioMediaFrame.FrameReference;
                        using AudioFrame audioFrame = frame.AudioMediaFrame.GetAudioFrame();

                        AudioEncodingProperties audioEncodingProperties = mediaFrame.AudioMediaFrame.AudioEncodingProperties;

                        if (this.audioFormat == null)
                        {
                            ushort channelCount = (ushort)audioEncodingProperties.ChannelCount;

                            if (this.configuration.AudioChannelNumber > channelCount)
                            {
                                throw new Exception(
                                    $"The audio channel requested, Channel {this.configuration.AudioChannelNumber}, exceeds " +
                                    $"the {audioEncodingProperties.ChannelCount} channel(s) available for this audio source.");
                            }

                            // The audio buffer output should be the contents of one channel.
                            if (this.configuration.SingleChannel)
                            {
                                channelCount = 1;
                            }

                            int samplingRate = (int)audioEncodingProperties.SampleRate;
                            ushort bitsPerSample = (ushort)audioEncodingProperties.BitsPerSample;
                            ushort blockAlign = (ushort)(channelCount * (bitsPerSample / 8));
                            int avgBytesPerSec = samplingRate * blockAlign;

                            string subtype = audioEncodingProperties.Subtype ?? string.Empty;

                            bool waveFormatTagExists = this.findSubtypeAsWaveFormatTag.TryGetValue(subtype, out WaveFormatTag formatTag);
                            if (!waveFormatTagExists)
                            {
                                formatTag = WaveFormatTag.WAVE_FORMAT_UNKNOWN;
                            }

                            this.audioFormat = WaveFormat.Create(
                                formatTag,
                                samplingRate,
                                bitsPerSample,
                                channelCount,
                                blockAlign,
                                avgBytesPerSec);
                        }

                        unsafe
                        {
                            using Windows.Media.AudioBuffer buffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read);
                            using IMemoryBufferReference reference = buffer.CreateReference();
                            ((UnsafeNative.IMemoryBufferByteAccess)reference).GetBuffer(out byte* audioDataIn, out uint capacity);

                            uint frameDurMs = (uint)mediaFrame.Duration.TotalMilliseconds;
                            uint sampleRate = audioEncodingProperties.SampleRate;
                            uint sampleCount = (frameDurMs * sampleRate) / 1000;

                            uint bytesPerSample = audioEncodingProperties.BitsPerSample / 8;
                            uint numAudioChannels = audioEncodingProperties.ChannelCount;

                            // Buffer size is (number of samples) * (size of each sample * number of channels)
                            byte[] audioDataOut = new byte[sampleCount * this.audioFormat.BlockAlign];

                            // If the single-channel buffer output is requested and the incoming
                            // audio data has multiple channels, we downsample the interlaced data.
                            if (this.configuration.SingleChannel && numAudioChannels > 1)
                            {
                                // Start the index position for the requested audio channel's buffer.
                                uint inPos = bytesPerSample * (this.configuration.AudioChannelNumber - 1);
                                uint outPos = 0;

                                while (outPos < audioDataOut.Length)
                                {
                                    byte* src = &audioDataIn[inPos];
                                    fixed (byte* dst = &audioDataOut[outPos])
                                    {
                                        Buffer.MemoryCopy(src, dst, bytesPerSample, bytesPerSample);
                                    }

                                    inPos += bytesPerSample * numAudioChannels;
                                    outPos += bytesPerSample;
                                }
                            }
                            else
                            {
                                // Copy the incoming raw audio data to the buffer.
                                byte* src = audioDataIn;
                                fixed (byte* dst = audioDataOut)
                                {
                                    Buffer.MemoryCopy(src, dst, audioDataOut.Length, audioDataOut.Length);
                                }
                            }

                            audioStream.Post(new AudioBuffer(audioDataOut, this.audioFormat), originatingTime);
                        }
                    }
                }
            };
        }
    }
}
