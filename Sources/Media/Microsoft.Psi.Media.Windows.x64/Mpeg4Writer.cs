// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media_Interop;

    /// <summary>
    /// Component that writes video and audio streams into an MPEG-4 file.
    /// </summary>
    public class Mpeg4Writer : IConsumer<Shared<Image>>, IDisposable
    {
        private readonly string name;
        private readonly Mpeg4WriterConfiguration configuration;
        private readonly Queue<(DateTime Timestamp, AudioBuffer Audio)> audioBuffers = new ();
        private readonly Queue<(DateTime Timestamp, Shared<Image> Image)> images = new ();
        private IntPtr waveFmtPtr = default;
        private MP4Writer writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mpeg4Writer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">Name of output file to write to.</param>
        /// <param name="configurationFilename">Name of file containing media capture device configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        public Mpeg4Writer(Pipeline pipeline, string filename, string configurationFilename, string name = nameof(Mpeg4Writer))
            : this(pipeline, filename, name)
        {
            var configurationHelper = new ConfigurationHelper<Mpeg4WriterConfiguration>(configurationFilename);
            this.configuration = configurationHelper.Configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mpeg4Writer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">Name of output file to write to.</param>
        /// <param name="configuration">Describes how to configure the media capture device.</param>
        /// <param name="name">An optional name for the component.</param>
        public Mpeg4Writer(Pipeline pipeline, string filename, Mpeg4WriterConfiguration configuration, string name = nameof(Mpeg4Writer))
            : this(pipeline, filename, name)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mpeg4Writer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">Name of output file to write to.</param>
        /// <param name="width">Width of output image in pixels.</param>
        /// <param name="height">Height of output image in pixels.</param>
        /// <param name="pixelFormat">Format of input images.</param>
        /// <param name="name">An optional name for the component.</param>
        public Mpeg4Writer(Pipeline pipeline, string filename, uint width, uint height, PixelFormat pixelFormat, string name = nameof(Mpeg4Writer))
            : this(pipeline, filename, name)
        {
            this.configuration = Mpeg4WriterConfiguration.Default;
            this.configuration.ImageWidth = width;
            this.configuration.ImageHeight = height;
            this.configuration.PixelFormat = pixelFormat;
        }

        private Mpeg4Writer(Pipeline pipeline, string filename, string name)
        {
            pipeline.PipelineRun += (_, _) =>
            {
                MP4Writer.Startup();
                this.writer = new MP4Writer();
                this.writer.Open(filename, this.configuration.Config);
            };

            pipeline.PipelineCompleted += (_, _) =>
            {
                if (this.writer != null)
                {
                    // Write any remaining data
                    this.WriteData(DateTime.MaxValue);
                }
            };

            this.name = name;
            this.ImageIn = pipeline.CreateReceiver<Shared<Image>>(this, this.ReceiveImage, nameof(this.ImageIn));
            this.AudioIn = pipeline.CreateReceiver<AudioBuffer>(this, this.ReceiveAudio, nameof(this.AudioIn));
        }

        /// <summary>
        /// Gets or sets the input stream of images.
        /// </summary>
        public Receiver<Shared<Image>> ImageIn { get; set; }

        /// <summary>
        /// Gets or sets the input stream of images.
        /// </summary>
        public Receiver<AudioBuffer> AudioIn { get; set; }

        /// <summary>
        /// Gets the input stream of images.
        /// </summary>
        public Receiver<Shared<Image>> In => this.ImageIn;

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            if (this.waveFmtPtr != default)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(this.waveFmtPtr);
            }

            // check for null since it's possible that Start was never called
            if (this.writer != null)
            {
                this.writer.Close();
                ((IDisposable)this.writer).Dispose(); // Cast to IDisposable to suppress false CA2213 warning
                this.writer = null;
                MP4Writer.Shutdown();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void ReceiveImage(Shared<Image> image, Envelope envelope)
        {
            // Cache the incoming images
            this.images.Enqueue((envelope.OriginatingTime, image.AddRef()));

            // Write what we can so far
            if (this.writer != null)
            {
                this.WriteData(this.ComputeHighWaterMark());
            }
        }

        private void ReceiveAudio(AudioBuffer audioBuffer, Envelope envelope)
        {
            // Cache the incoming audio buffers
            this.audioBuffers.Enqueue((envelope.OriginatingTime, audioBuffer.DeepClone()));

            // Write what we can so far
            if (this.writer != null)
            {
                this.WriteData(this.ComputeHighWaterMark());
            }
        }

        private DateTime ComputeHighWaterMark()
        {
            if (this.configuration.ContainsAudio)
            {
                var lastAudioTime = this.AudioIn.LastEnvelope.OriginatingTime;
                var lastImageTime = this.ImageIn.LastEnvelope.OriginatingTime;
                return lastAudioTime < lastImageTime ? lastAudioTime : lastImageTime;
            }
            else
            {
                return DateTime.MaxValue;
            }
        }

        private void WriteData(DateTime highWaterMark)
        {
            // Write images and audio in a coordinated way, using a watermark approach, so one stream does not get too far ahead of the other
            var messageProcessed = true;
            while (messageProcessed)
            {
                messageProcessed = false;
                while (this.images.Count > 0 &&
                       this.images.Peek().Timestamp <= highWaterMark &&
                       (this.audioBuffers.Count == 0 || this.images.Peek().Timestamp <= this.audioBuffers.Peek().Timestamp))
                {
                    var image = this.images.Dequeue();
                    this.writer.WriteVideoFrame(image.Timestamp.Ticks, image.Image.Resource.ImageData, (uint)image.Image.Resource.Width, (uint)image.Image.Resource.Height, (int)image.Image.Resource.PixelFormat);
                    image.Image.Dispose();
                    messageProcessed = true;
                }

                while (this.audioBuffers.Count > 0 &&
                       this.audioBuffers.Peek().Timestamp <= highWaterMark &&
                       (this.images.Count == 0 || this.audioBuffers.Peek().Timestamp <= this.images.Peek().Timestamp))
                {
                    var audio = this.audioBuffers.Dequeue();
                    var audioBuffer = audio.Audio;
                    var audioData = System.Runtime.InteropServices.Marshal.AllocHGlobal(audioBuffer.Length);
                    System.Runtime.InteropServices.Marshal.Copy(audioBuffer.Data, 0, audioData, audioBuffer.Length);

                    if (this.waveFmtPtr == default)
                    {
                        this.waveFmtPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal((int)WaveFormat.MarshalSizeOf(audioBuffer.Format) + sizeof(int));
                        WaveFormat.MarshalToPtr(audioBuffer.Format, this.waveFmtPtr);
                    }

                    // The semantics of MP4Writer.WriteAudioSample(ticks, audio, ...) are that ticks
                    // represents the *start* of the audio buffer, while \psi semantics are that
                    // AudioBuffer messages have originating times representing the *end* of the buffer.
                    var originatingTicksOfStartOfAudioBuffer = audio.Timestamp.Ticks - audio.Audio.Duration.Ticks;
                    this.writer.WriteAudioSample(originatingTicksOfStartOfAudioBuffer, audioData, (uint)audioBuffer.Length, this.waveFmtPtr);
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(audioData);
                    messageProcessed = true;
                }
            }
        }
    }
}
