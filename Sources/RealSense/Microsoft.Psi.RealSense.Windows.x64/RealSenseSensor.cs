// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Microsoft.Psi.RealSense.Windows
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that captures and streams video and depth from an Intel RealSense camera.
    /// </summary>
    public class RealSenseSensor : ISourceComponent, IDisposable
    {
        private bool shutdown;
        private Pipeline pipeline;
        private RealSenseDevice device;
        private Thread thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealSenseSensor"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        public RealSenseSensor(Pipeline pipeline)
        {
            pipeline.RegisterPipelineStartHandler(this, this.OnPipelineStart);
            pipeline.RegisterPipelineStopHandler(this, this.OnPipelineStop);
            this.shutdown = false;
            this.ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, "ColorImage");
            this.DepthImage = pipeline.CreateEmitter<Shared<Image>>(this, "DepthImage");
            this.pipeline = pipeline;
        }

        /// <summary>
        /// Gets the emitter that generates RGB images from the RealSense depth camera
        /// </summary>
        public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the emitter that generates Depth images from the RealSense depth camera
        /// </summary>
        public Emitter<Shared<Image>> DepthImage { get; private set; }

        /// <summary>
        /// Called once all the subscriptions are established.
        /// </summary>
        private void OnPipelineStart()
        {
            this.device = new RealSenseDevice();
            this.thread = new Thread(new ThreadStart(ThreadProc));
            this.thread.Start();
        }

        private void ThreadProc()
        {
            Imaging.PixelFormat pixelFormat = Imaging.PixelFormat.BGR_24bpp;
            switch (device.GetColorBpp())
            {
                case 24:
                    pixelFormat = Imaging.PixelFormat.BGR_24bpp;
                    break;
                case 32:
                    pixelFormat = Imaging.PixelFormat.BGRX_32bpp;
                    break;
                default:
                    throw new NotSupportedException("Expected 24bpp or 32bpp image.");
            }
            var colorImage = ImagePool.GetOrCreate((int)device.GetColorWidth(), (int)device.GetColorHeight(), pixelFormat);
            uint colorImageSize = device.GetColorHeight() * device.GetColorStride();
            switch (device.GetDepthBpp())
            {
                case 16:
                    pixelFormat = Imaging.PixelFormat.Gray_16bpp;
                    break;
                case 8:
                    pixelFormat = Imaging.PixelFormat.Gray_8bpp;
                    break;
                default:
                    throw new NotSupportedException("Expected 8bpp or 16bpp image.");
            }
            var depthImage = ImagePool.GetOrCreate((int)device.GetDepthWidth(), (int)device.GetDepthHeight(), pixelFormat);
            uint depthImageSize = device.GetDepthHeight() * device.GetDepthStride();
            while (!this.shutdown)
            {
                device.ReadFrame(colorImage.Resource.ImageData, colorImageSize, depthImage.Resource.ImageData, depthImageSize);
                DateTime t = DateTime.Now;
                ColorImage.Post(colorImage, t);
                DepthImage.Post(depthImage, t);
            }
        }

        /// <summary>
        /// Called by the pipeline when media capture should be stopped
        /// </summary>
        private void OnPipelineStop()
        {
            if (this.thread != null)
            {
                this.shutdown = true;
                TimeSpan waitTime = new TimeSpan(0, 0, 1);
                if (this.thread.Join(waitTime) != true)
                {
                    this.thread.Abort();
                }
            }
            if (this.device != null)
            {
                this.device = null;
            }
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            if (this.device != null)
            {
                this.device.Dispose();
                this.device = null;
            }
        }
    }
}
