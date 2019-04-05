// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if FFMPEG
namespace Microsoft.Psi.Media
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
#if WINDOWS
    using Microsoft.Psi.Media.Native.Windows;
#else
    using Microsoft.Psi.Media.Native.Linux;
#endif

    /// <summary>
    /// Component that streams video and audio from an MPEG file.
    /// </summary>
    public class FFMPEGMediaSource : Generator, IDisposable
    {
        private bool disposed = false;
        private string filename;
        private DateTime start;
        private FFMPEGReader mpegReader;
        private WaveFormat waveFormat;
        private DateTime lastAudioTime = default(DateTime);
        private PixelFormat outputFormat = PixelFormat.BGRX_32bpp;
        private AudioBuffer audioBuffer;
        private int audioBufferSize;
        private IntPtr audioData;
        private int audioDataSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMPEGMediaSource"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        /// <param name="filename">Name of media file to play</param>
        /// <param name="format">Output format for images</param>
        public FFMPEGMediaSource(Pipeline pipeline, string filename, PixelFormat format = PixelFormat.BGRX_32bpp)
           : base(pipeline)
        {
            FileInfo info = new FileInfo(filename);
            pipeline.ProposeReplayTime(new TimeInterval(info.CreationTime, DateTime.MaxValue), new TimeInterval(info.CreationTime, DateTime.MaxValue));
            this.start = info.CreationTime;
            this.filename = filename;
            this.Image = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Image));
            this.Audio = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Audio));
            this.mpegReader = new FFMPEGReader((format == PixelFormat.BGRX_32bpp) ? 32 : 24);
            this.mpegReader.Open(filename, new FFMPEGReaderConfiguration());
            this.waveFormat = WaveFormat.CreatePcm(this.mpegReader.AudioSampleRate, this.mpegReader.AudioBitsPerSample, this.mpegReader.AudioNumChannels);
            this.outputFormat = format;
            this.audioBufferSize = 0;
            this.audioData = IntPtr.Zero;
            this.audioDataSize = 0;
        }

        /// <summary>
        /// Gets the emitter that generates images from the media
        /// </summary>
        public int Width
        {
            get
            {
                return this.mpegReader.Width;
            }
        }

        /// <summary>
        /// Gets the emitter that generates images from the media
        /// </summary>
        public int Height
        {
            get
            {
                return this.mpegReader.Height;
            }
        }

        /// <summary>
        /// Gets the emitter that generates images from the media
        /// </summary>
        public Emitter<Shared<Image>> Image { get; private set; }

        /// <summary>
        /// Gets the emitter that generates audio from the media
        /// </summary>
        public Emitter<AudioBuffer> Audio { get; private set; }

        /// <summary>
        /// Releases the media player
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
            }
        }

        /// <summary>
        /// GenerateNext is called by the Generator base class when the next sample should be read
        /// </summary>
        /// <param name="previous">Time of previous sample</param>
        /// <returns>Time for current sample</returns>
        protected override DateTime GenerateNext(DateTime previous)
        {
            DateTime originatingTime = default(DateTime);
            FFMPEGFrameInfo frameInfo = new FFMPEGFrameInfo();
            bool eos = false;
            bool frameRead = this.mpegReader.NextFrame(ref frameInfo, out eos);
            if (!frameRead)
            {
                return this.lastAudioTime;
            }

            if (eos)
            {
                return DateTime.MaxValue;
            }

            double timestamp = 0.0;
            int dataSize = 0;
            if (frameInfo.FrameType == FFMPEGFrameInfo.FrameTypeVideo)
            {
                using (var image = ImagePool.GetOrCreate(this.mpegReader.Width, this.mpegReader.Height, this.outputFormat))
                {
                    if (this.mpegReader.ReadFrameData(image.Resource.ImageData, ref dataSize, ref timestamp))
                    {
                        originatingTime = this.start + TimeSpan.FromMilliseconds(timestamp);
                        this.Image.Post(image, originatingTime);
                    }
                }
            }
            else if (frameInfo.FrameType == FFMPEGFrameInfo.FrameTypeAudio)
            {
                if (this.audioData == IntPtr.Zero || frameInfo.BufferSize != this.audioDataSize)
                {
                    if (this.audioData != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(this.audioData);
                    }

                    this.audioData = Marshal.AllocHGlobal(frameInfo.BufferSize);
                    this.audioDataSize = frameInfo.BufferSize;
                }

                if (this.mpegReader.ReadFrameData(this.audioData, ref dataSize, ref timestamp))
                {
                    if (dataSize > 0)
                    {
                        if (dataSize != this.audioBufferSize)
                        {
                            this.audioBuffer = new AudioBuffer(dataSize, this.waveFormat);
                            this.audioBufferSize = dataSize;
                        }

                        originatingTime = this.start + TimeSpan.FromMilliseconds(timestamp);
                        Marshal.Copy(this.audioData, this.audioBuffer.Data, 0, dataSize);
                        this.Audio.Post(this.audioBuffer, originatingTime);
                        this.lastAudioTime = originatingTime;
                    }
                }
            }

            return this.lastAudioTime;
        }
    }
}
#endif
