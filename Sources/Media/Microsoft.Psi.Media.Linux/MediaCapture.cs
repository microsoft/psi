// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation

namespace Microsoft.Psi.Media
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using SkiaSharp;

    /// <summary>
    /// Component that captures and streams video from a camera.
    /// </summary>
    public class MediaCapture : IProducer<Shared<Image>>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly MediaCaptureConfiguration configuration;

        private MediaCaptureInternal camera;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">Name of file containing media capture device configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCapture(Pipeline pipeline, string configurationFilename, string name = nameof(MediaCapture))
            : this(pipeline, name)
        {
            var configurationHelper = new ConfigurationHelper<MediaCaptureConfiguration>(configurationFilename);
            this.configuration = configurationHelper.Configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Describes how to configure the media capture device.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCapture(Pipeline pipeline, MediaCaptureConfiguration configuration, string name = nameof(MediaCapture))
            : this(pipeline, name)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="width">Width of output image in pixels.</param>
        /// <param name="height">Height of output image in pixels.</param>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="pixelFormat">Pixel format.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCapture(Pipeline pipeline, int width, int height, string deviceId = "/dev/video0", PixelFormatId pixelFormat = PixelFormatId.BGR24, string name = nameof(MediaCapture))
            : this(pipeline, name)
        {
            if (pixelFormat != PixelFormatId.BGR24 && pixelFormat != PixelFormatId.YUYV && pixelFormat != PixelFormatId.MJPEG)
            {
                throw new ArgumentException("Only YUYV and MJPEG are currently supported");
            }

            this.configuration = new MediaCaptureConfiguration()
            {
                Width = width,
                Height = height,
                DeviceId = deviceId,
                PixelFormat = pixelFormat,
            };
        }

        private MediaCapture(Pipeline pipeline, string name)
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Out));
            this.Raw = pipeline.CreateEmitter<Shared<byte[]>>(this, nameof(this.Raw));
        }

        /// <summary>
        /// Gets the output stream of images.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <summary>
        /// Gets the raw output stream of image frames.
        /// </summary>
        /// <remarks>
        /// This is the unprocessed/unconverted frame data. (e.g. YUYV or MJPEG).
        /// </remarks>
        public Emitter<Shared<byte[]>> Raw { get; private set; }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // check for null since it's possible that Start was never called
            if (this.camera != null)
            {
                this.camera.Close();
                this.camera.Dispose();
                this.camera = null;
            }
        }

        /// <inheritdoc/>
        public unsafe void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.camera = new MediaCaptureInternal(this.configuration.DeviceId);
            this.camera.Open();
            var isFormatSupported = false;
            foreach (var format in this.camera.SupportedPixelFormats())
            {
                if (format.Pixels == this.configuration.PixelFormat)
                {
                    this.camera.SetVideoFormat(this.configuration.Width, this.configuration.Height, format);
                    isFormatSupported = true;
                }
            }

            if (!isFormatSupported)
            {
                throw new ArgumentException($"Pixel format {this.configuration.PixelFormat} is not supported by the camera");
            }

            var current = this.camera.GetVideoFormat();
            if (current.Width != this.configuration.Width || current.Height != this.configuration.Height)
            {
                throw new ArgumentException($"Width/height {this.configuration.Width}x{this.configuration.Height} is not supported by the camera");
            }

            this.camera.OnFrame += (_, frame) =>
            {
                var originatingTime = this.pipeline.GetCurrentTime();

                if (this.Raw.HasSubscribers)
                {
                    var len = frame.Length;
                    using Shared<byte[]> shared = SharedArrayPool<byte>.GetOrCreate(len);
                    Marshal.Copy(frame.Start, shared.Resource, 0, len);
                    this.Raw.Post(shared, originatingTime);
                }

                if (this.Out.HasSubscribers)
                {
                    if (this.configuration.PixelFormat == PixelFormatId.BGR24)
                    {
                        using var sharedImage = ImagePool.GetOrCreate(this.configuration.Width, this.configuration.Height, PixelFormat.BGR_24bpp);
                        sharedImage.Resource.CopyFrom((IntPtr)frame.Start);
                        this.Out.Post(sharedImage, this.pipeline.GetCurrentTime());
                    }
                    else if (this.configuration.PixelFormat == PixelFormatId.YUYV)
                    {
                        // convert YUYV -> BGR24 (see https://msdn.microsoft.com/en-us/library/ms893078.aspx)
                        using var sharedImage = ImagePool.GetOrCreate(this.configuration.Width, this.configuration.Height, PixelFormat.BGR_24bpp);
                        var len = (int)(frame.Length * 1.5);
                        using Shared<byte[]> shared = SharedArrayPool<byte>.GetOrCreate(len);
                        var bytes = shared.Resource;
                        var pY = (byte*)frame.Start.ToPointer();
                        var pU = pY + 1;
                        var pV = pY + 3;
                        for (var i = 0; i < len;)
                        {
                            int y = (*pY - 16) * 298;
                            int u = *pU - 128;
                            int v = *pV - 128;
                            int r = (y + (409 * v) + 128) >> 8;
                            int g = (y - (100 * u) - (208 * v) + 128) >> 8;
                            int b = (y + (516 * u) + 128) >> 8;

                            bytes[i++] = (byte)((r < 0) ? 0 : ((r > 255) ? 255 : r));
                            bytes[i++] = (byte)((g < 0) ? 0 : ((g > 255) ? 255 : g));
                            bytes[i++] = (byte)((b < 0) ? 0 : ((b > 255) ? 255 : b));

                            pY += 2;

                            y = (*pY - 16) * 298;
                            r = (y + (409 * v) + 128) >> 8;
                            g = (y - (100 * u) - (208 * v) + 128) >> 8;
                            b = (y + (516 * u) + 128) >> 8;
                            bytes[i++] = (byte)((r < 0) ? 0 : ((r > 255) ? 255 : r));
                            bytes[i++] = (byte)((g < 0) ? 0 : ((g > 255) ? 255 : g));
                            bytes[i++] = (byte)((b < 0) ? 0 : ((b > 255) ? 255 : b));

                            pY += 2;
                            pU += 4;
                            pV += 4;
                        }

                        sharedImage.Resource.CopyFrom(bytes);
                        this.Out.Post(sharedImage, originatingTime);
                    }
                    else if (this.configuration.PixelFormat == PixelFormatId.MJPEG)
                    {
                        var decoded = SKBitmap.Decode(new UnmanagedMemoryStream((byte*)frame.Start, frame.Length));
                        if (decoded != null)
                        {
                            using var sharedImage = ImagePool.GetOrCreate(this.configuration.Width, this.configuration.Height, PixelFormat.BGRA_32bpp);
                            sharedImage.Resource.CopyFrom(decoded.Bytes);
                            this.Out.Post(sharedImage, originatingTime);
                        }
                    }
                }

#if TEST_DROPPED_FRAMES
                System.Threading.Thread.Sleep(1000); // for testing dropped frames
#endif // TEST_DROPPED_FRAMES

                frame.Dispose(); // release back to driver!
            };

            this.camera.StreamBuffers();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.camera != null)
            {
                this.camera.Close();
                this.camera.Dispose();
                this.camera = null;
            }

            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;
    }
}

#pragma warning restore SA1305 // Field names must not use Hungarian notation
