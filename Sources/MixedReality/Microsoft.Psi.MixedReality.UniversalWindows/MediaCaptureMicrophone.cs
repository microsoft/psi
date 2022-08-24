// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
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
    /// Photo/video (PV) camera source component.
    /// </summary>
    public class MediaCaptureMicrophone : ISourceComponent, IDisposable
    {
        private readonly MediaCaptureMicrophoneConfiguration configuration;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly Task initMediaCaptureTask;

        private readonly WaveFormat audioFormat;

        private MediaCapture mediaCapture;
        private MediaFrameReader audioFrameReader;
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> audioFrameHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCaptureMicrophone"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCaptureMicrophone(Pipeline pipeline, MediaCaptureMicrophoneConfiguration configuration = null, string name = nameof(MediaCaptureMicrophone))
        {
            this.name = name;
            this.pipeline = pipeline;
            this.configuration = configuration ?? new MediaCaptureMicrophoneConfiguration();
            this.audioFormat = this.configuration.MicrophoneConfiguration.AudioFormat;

            this.AudioBuffer = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.AudioBuffer));

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
        public Emitter<AudioBuffer> AudioBuffer { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            // ensure mediaCapture isn't disposed during initialization task
            this.initMediaCaptureTask.Wait();

            if (this.mediaCapture != null)
            {
                this.mediaCapture.Dispose();
                this.mediaCapture = null;
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
                    this.AudioBuffer);

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
            MediaCaptureMicrophoneConfiguration audioSettings,
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

                        unsafe
                        {
                            using Windows.Media.AudioBuffer buffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read);
                            using IMemoryBufferReference reference = buffer.CreateReference();
                            ((UnsafeNative.IMemoryBufferByteAccess)reference).GetBuffer(out byte* audioDataIn, out uint capacity);

                            uint frameDurMs = (uint)mediaFrame.Duration.TotalMilliseconds;
                            uint sampleRate = audioEncodingProperties.SampleRate;
                            uint sampleCount = (frameDurMs * sampleRate) / 1000;

                            uint numAudioChannels = audioEncodingProperties.ChannelCount;
                            uint bytesPerSample = audioEncodingProperties.BitsPerSample / 8;

                            // Buffer size is (number of samples) * (size of each sample)
                            byte[] audioDataOut = new byte[sampleCount * bytesPerSample];

                            // Convert to bytes
                            if (numAudioChannels > 1)
                            {
                                // Data is interlaced, so we need to change the multi-channel input
                                // to the supported single-channel output for StereoKit to consume
                                uint inPos = 0;
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
