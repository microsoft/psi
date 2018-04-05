// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation

namespace Microsoft.Psi.Media
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Media capture component for Linux.
    /// </summary>
    public class MediaCapture : IProducer<Shared<Image>>, IStartable, IDisposable
    {
        private static readonly SharedPool<byte[]> RawPool = new SharedPool<byte[]>(0);

        private readonly Pipeline pipeline;
        private readonly MediaCaptureConfiguration configuration;

        private MediaCaptureInternal camera;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        /// <param name="configurationFilename">Name of file containing media capture device configuration</param>
        public MediaCapture(Pipeline pipeline, string configurationFilename)
        : this(pipeline)
        {
            var configurationHelper = new ConfigurationHelper<MediaCaptureConfiguration>(configurationFilename);
            this.configuration = configurationHelper.Configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        /// <param name="configuration">Describes how to configure the media capture device</param>
        public MediaCapture(Pipeline pipeline, MediaCaptureConfiguration configuration)
        : this(pipeline)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        /// <param name="width">Width of output image in pixels</param>
        /// <param name="height">Height of output image in pixels</param>
        /// <param name="deviceId">Device ID</param>
        /// <param name="pixelFormat">Pixel format</param>
        public MediaCapture(Pipeline pipeline, int width, int height, string deviceId = "/dev/video0", PixelFormatId pixelFormat = PixelFormatId.BGR24)
            : this(pipeline)
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

        private MediaCapture(Pipeline pipeline)
        {
            this.pipeline = pipeline;
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
        /// This is the unprocessed/unconverted frame data. (e.g. YUYV or MJPEG)
        /// </remarks>
        public Emitter<Shared<byte[]>> Raw { get; private set; }

        /// <summary>
        /// Called once all the subscriptions are established.
        /// </summary>
        /// <param name="onCompleted">Delegate to call when the execution completed</param>
        /// <param name="descriptor">If set, describes the playback constraints</param>
        unsafe void IStartable.Start(Action onCompleted, ReplayDescriptor descriptor)
        {
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
                    using (Shared<byte[]> shared = RawPool.GetOrCreate(() => new byte[len]))
                    {
                        var buffer = shared.Resource.Length >= len ? shared : new Shared<byte[]>(new byte[len], shared.Recycler);
                        Marshal.Copy(frame.Start, buffer.Resource, 0, len);
                        this.Raw.Post(buffer, originatingTime);
                    }
                }

                if (this.Out.HasSubscribers)
                {
                    using (var sharedImage = ImagePool.GetOrCreate(this.configuration.Width, this.configuration.Height, PixelFormat.BGR_24bpp))
                    {
                        if (this.configuration.PixelFormat == PixelFormatId.BGR24)
                        {
                            sharedImage.Resource.CopyFrom((IntPtr)frame.Start);
                            this.Out.Post(sharedImage, this.pipeline.GetCurrentTime());
                        }
                        else if (this.configuration.PixelFormat == PixelFormatId.YUYV)
                        {
                            // convert YUYV -> BGR24 (see https://msdn.microsoft.com/en-us/library/ms893078.aspx)
                            var len = (int)(frame.Length * 1.5);
                            using (Shared<byte[]> shared = RawPool.GetOrCreate(() => new byte[len]))
                            {
                                var buffer = shared.Resource.Length >= len ? shared : new Shared<byte[]>(new byte[len], shared.Recycler);
                                var bytes = buffer.Resource;
                                var pY = (byte*)frame.Start.ToPointer();
                                var pU = pY + 1;
                                var pV = pY + 2;
                                for (var i = 0; i < len;)
                                {
                                    var y = (*pY - 16) * 298;
                                    var u = *pU - 128;
                                    var v = *pV - 128;
                                    var b = (y + (516 * u) + 128) >> 8;
                                    var g = (y - (100 * u) - (208 * v) + 128) >> 8;
                                    var r = (y + (409 * v) + 128) >> 8;
                                    bytes[i++] = (byte)(b < 0 ? 0 : b > 255 ? 255 : b);
                                    bytes[i++] = (byte)(g < 0 ? 0 : g > 255 ? 255 : g);
                                    bytes[i++] = (byte)(r < 0 ? 0 : r > 255 ? 255 : r);
                                    pY += 2;
                                    pU += 4;
                                    pV += 4;
                                }

                                this.Raw.Post(buffer, originatingTime);
                            }
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

        /// <summary>
        /// Called by the pipeline when media capture should be stopped
        /// </summary>
        void IStartable.Stop()
        {
            if (this.camera != null)
            {
                this.camera.Close();
                this.camera.Dispose();
                this.camera = null;
            }
        }

        /// <summary>
        /// Dispose method
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
    }
}

#pragma warning restore SA1305 // Field names must not use Hungarian notation