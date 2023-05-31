// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using SharpDX.MediaFoundation;

    /// <summary>
    /// Component that streams video and audio from a media file.
    /// </summary>
    public class MediaSource : Generator, IDisposable
    {
        private readonly Stream input;
        private readonly DateTime start;
        private readonly bool dropOutOfOrderPackets = false;

        private bool disposed = false;
        private short videoWidth;
        private short videoHeight;
        private SourceReader sourceReader;
        private int imageStreamIndex = -1;
        private int audioStreamIndex = -1;
        private WaveFormat waveFormat;

        /// <summary>
        /// Keep track of the timestamp of the last image frame (computed from the value reported to us by media foundation).
        /// </summary>
        private DateTime lastPostedImageTime = DateTime.MinValue;

        /// <summary>
        /// Keep track of the timestamp of the last audio buffer (computed from the value reported to us by media foundation).
        /// </summary>
        private DateTime lastPostedAudioTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">Name of media file to play.</param>
        /// <param name="dropOutOfOrderPackets">Optional flag specifying whether to drop out of order packets (defaults to <c>false</c>).</param>
        /// <param name="name">An optional name for the stream operator.</param>
        public MediaSource(Pipeline pipeline, string filename, bool dropOutOfOrderPackets = false, string name = nameof(MediaSource))
            : this(pipeline, File.OpenRead(filename), new FileInfo(filename).CreationTime, dropOutOfOrderPackets, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="input">Source stream of the media to consume.</param>
        /// <param name="startTime">Optional date/time that the media started.</param>
        /// <param name="dropOutOfOrderPackets">Optional flag specifying whether to drop out of order packets (defaults to <c>false</c>).</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaSource(Pipeline pipeline, Stream input, DateTime? startTime = null, bool dropOutOfOrderPackets = false, string name = nameof(MediaSource))
            : base(pipeline, name: name)
        {
            var proposedReplayTime = startTime ?? DateTime.UtcNow;
            pipeline.ProposeReplayTime(new TimeInterval(proposedReplayTime, DateTime.MaxValue));
            this.start = proposedReplayTime;
            this.input = input;
            this.dropOutOfOrderPackets = dropOutOfOrderPackets;
            this.Image = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Image));
            this.Audio = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Audio));
            this.InitializeMediaPipeline();
        }

        /// <summary>
        /// Gets the emitter that generates images from the media.
        /// </summary>
        public Emitter<Shared<Image>> Image { get; private set; }

        /// <summary>
        /// Gets the emitter that generates audio from the media.
        /// </summary>
        public Emitter<AudioBuffer> Audio { get; private set; }

        /// <summary>
        /// Releases the media player.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.sourceReader?.Dispose();
                this.input?.Dispose();
                MediaManager.Shutdown();
                this.disposed = true;
            }
        }

        /// <summary>
        /// GenerateNext is called by the Generator base class when the next sample should be read.
        /// </summary>
        /// <param name="currentTime">The originating time that triggered the current call.</param>
        /// <returns>The originating time at which to capture the next sample.</returns>
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            DateTime originatingTime = default;
            var sample = this.sourceReader.ReadSample(SourceReaderIndex.AnyStream, 0, out int streamIndex, out SourceReaderFlags flags, out long timestamp);
            if (sample != null)
            {
                originatingTime = this.start + TimeSpan.FromTicks(timestamp);
                var buffer = sample.ConvertToContiguousBuffer();
                var data = buffer.Lock(out _, out int currentByteCount);

                if (streamIndex == this.imageStreamIndex)
                {
                    // Detect out of order originating times
                    if (originatingTime > this.lastPostedImageTime)
                    {
                        using var sharedImage = ImagePool.GetOrCreate(this.videoWidth, this.videoHeight, Imaging.PixelFormat.BGR_24bpp);
                        sharedImage.Resource.CopyFrom(data);
                        this.Image.Post(sharedImage, originatingTime);
                        this.lastPostedImageTime = originatingTime;
                    }
                    else if (!this.dropOutOfOrderPackets)
                    {
                        throw new InvalidOperationException(
                            $"The most recently captured image frame has a timestamp ({originatingTime.TimeOfDay}) which is before " +
                            $"that of the last posted image frame ({this.lastPostedImageTime.TimeOfDay}), as reported by the video stream. This could " +
                            $"be due to a timing glitch in the video stream. Set the 'dropOutOfOrderPackets' " +
                            $"parameter to true to handle this condition by dropping " +
                            $"packets with out of order timestamps.");
                    }
                }
                else if (streamIndex == this.audioStreamIndex)
                {
                    // Detect out of order originating times
                    if (originatingTime > this.lastPostedAudioTime)
                    {
                        var audioBuffer = new AudioBuffer(currentByteCount, this.waveFormat);
                        Marshal.Copy(data, audioBuffer.Data, 0, currentByteCount);
                        this.Audio.Post(audioBuffer, originatingTime);
                        this.lastPostedAudioTime = originatingTime;
                    }
                    else if (!this.dropOutOfOrderPackets)
                    {
                        throw new InvalidOperationException(
                            $"The most recently captured audio buffer has a timestamp ({originatingTime.TimeOfDay}) which is before " +
                            $"that of the last posted audio buffer ({this.lastPostedAudioTime.TimeOfDay}), as reported by the audio stream. This could " +
                            $"be due to a timing glitch in the audio stream. Set the 'dropOutOfOrderPackets' " +
                            $"parameter to true to handle this condition by dropping " +
                            $"packets with out of order timestamps.");
                    }
                }

                buffer.Unlock();
                buffer.Dispose();
                sample.Dispose();
            }

            if (flags == SourceReaderFlags.Endofstream)
            {
                return DateTime.MaxValue; // Used to indicated there is no more data
            }

            return originatingTime;
        }

        private static void DumpMediaType(MediaType mediaType)
        {
            var subType = mediaType.Get(MediaTypeAttributeKeys.Subtype);
            Debug.WriteLine($"Found stream MajorType: {mediaType.MajorType} SubType: {subType}");
        }

        /// <summary>
        /// Called by the ctor to configure the media playback component.
        /// </summary>
        private void InitializeMediaPipeline()
        {
            MediaManager.Startup(false);
            var sourceReaderAttributes = new MediaAttributes();
            sourceReaderAttributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);
            this.sourceReader = new SourceReader(this.input, sourceReaderAttributes);
            this.sourceReader.SetStreamSelection(SourceReaderIndex.AllStreams, false);

            int streamIndex = 0;
            bool doneEnumerating = false;
            while (!doneEnumerating)
            {
                try
                {
                    MediaType mediaType = this.sourceReader.GetCurrentMediaType(streamIndex);
                    var subType = mediaType.Get(MediaTypeAttributeKeys.Subtype);
                    DumpMediaType(mediaType);

                    if (mediaType.MajorType == MediaTypeGuids.Video && this.imageStreamIndex == -1)
                    {
                        this.imageStreamIndex = streamIndex;

                        // get the image size
                        long frameSize = mediaType.Get(MediaTypeAttributeKeys.FrameSize);
                        this.videoHeight = (short)frameSize;
                        this.videoWidth = (short)(frameSize >> 32);

                        // enable the stream and set the current media type
                        this.sourceReader.SetStreamSelection(this.imageStreamIndex, true);
                        mediaType = new MediaType();
                        mediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                        mediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb24);
                        mediaType.Set(MediaTypeAttributeKeys.FrameSize, frameSize);
                        this.sourceReader.SetCurrentMediaType(this.imageStreamIndex, mediaType);
                    }
                    else if (mediaType.MajorType == MediaTypeGuids.Audio && this.audioStreamIndex == -1)
                    {
                        this.audioStreamIndex = streamIndex;

                        // enable the stream and set the current media type to PCM
                        this.sourceReader.SetStreamSelection(this.audioStreamIndex, true);
                        mediaType = new MediaType();
                        mediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Audio);
                        mediaType.Set(MediaTypeAttributeKeys.Subtype, AudioFormatGuids.Pcm);
                        this.sourceReader.SetCurrentMediaType(this.audioStreamIndex, mediaType);

                        // get back all the media type details
                        mediaType = this.sourceReader.GetCurrentMediaType(this.audioStreamIndex);
                        int numberOfChannels = mediaType.Get(MediaTypeAttributeKeys.AudioNumChannels);
                        int sampleRate = mediaType.Get(MediaTypeAttributeKeys.AudioSamplesPerSecond);
                        int bitsPerSample = mediaType.Get(MediaTypeAttributeKeys.AudioBitsPerSample);

                        // post our output audio format
                        this.waveFormat = WaveFormat.CreatePcm(sampleRate, bitsPerSample, numberOfChannels);
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.GetType());

                    // expected thrown exception
                    // unfortunately no way to tell how many streams other than trying
                    doneEnumerating = true;
                }

                streamIndex += 1;
            }
        }
    }
}
