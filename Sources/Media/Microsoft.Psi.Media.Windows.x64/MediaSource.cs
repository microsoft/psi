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
        private bool disposed = false;
        private string filename;
        private short videoWidth;
        private short videoHeight;
        private SourceReader sourceReader;
        private DateTime start;
        private int imageStreamIndex = -1;
        private int audioStreamIndex = -1;
        private WaveFormat waveFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSource"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="filename">Name of media file to play.</param>
        public MediaSource(Pipeline pipeline, string filename)
            : base(pipeline)
        {
            FileInfo info = new FileInfo(filename);
            pipeline.ProposeReplayTime(new TimeInterval(info.CreationTime, DateTime.MaxValue));
            this.start = info.CreationTime;
            this.filename = filename;
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
                this.sourceReader.Dispose();
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
            DateTime originatingTime = default(DateTime);
            int streamIndex = 0;
            SourceReaderFlags flags = SourceReaderFlags.None;
            long timestamp = 0;
            Sample sample = this.sourceReader.ReadSample(SourceReaderIndex.AnyStream, 0, out streamIndex, out flags, out timestamp);
            if (sample != null)
            {
                originatingTime = this.start + TimeSpan.FromTicks(timestamp);
                MediaBuffer buffer = sample.ConvertToContiguousBuffer();
                int currentByteCount = 0;
                int maxByteCount = 0;
                IntPtr data = buffer.Lock(out maxByteCount, out currentByteCount);

                if (streamIndex == this.imageStreamIndex)
                {
                    using (var sharedImage = ImagePool.GetOrCreate(this.videoWidth, this.videoHeight, Imaging.PixelFormat.BGR_24bpp))
                    {
                        sharedImage.Resource.CopyFrom(data);
                        this.Image.Post(sharedImage, originatingTime);
                    }
                }
                else if (streamIndex == this.audioStreamIndex)
                {
                    AudioBuffer audioBuffer = new AudioBuffer(currentByteCount, this.waveFormat);
                    Marshal.Copy(data, audioBuffer.Data, 0, currentByteCount);
                    this.Audio.Post(audioBuffer, originatingTime);
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
            MediaAttributes sourceReaderAttributes = new MediaAttributes();
            sourceReaderAttributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);
            this.sourceReader = new SourceReader(this.filename, sourceReaderAttributes);
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
