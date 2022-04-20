// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that streams video from a Windows Media Visual.
    /// </summary>
    public class VisualCapture : Generator, IProducer<Shared<Image>>
    {
        private readonly Visual visual;
        private readonly int pixelWidth;
        private readonly int pixelHeight;
        private readonly TimeSpan interval;
        private readonly RenderTargetBitmap renderTargetBitmap;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval at which to render and emit frames of the rendered visual.</param>
        /// <param name="visual">Windows Media Visual to stream.</param>
        /// <param name="pixelWidth">Pixel width at which to render the visual.</param>
        /// <param name="pixelHeight">Pixel height at which to render the visual.</param>
        /// <param name="name">An optional name for the component.</param>
        public VisualCapture(Pipeline pipeline, TimeSpan interval, Visual visual, int pixelWidth, int pixelHeight, string name = nameof(VisualCapture))
            : base(pipeline, true, name)
        {
            this.interval = interval;
            this.visual = visual;
            this.pixelWidth = pixelWidth;
            this.pixelHeight = pixelHeight;
            this.renderTargetBitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
            this.Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Configuration values for the visual capture component.</param>
        /// <param name="name">An optional name for the component.</param>
        public VisualCapture(Pipeline pipeline, VisualCaptureConfiguration configuration, string name = nameof(VisualCapture))
            : this(pipeline, configuration.Interval, configuration.Visual, configuration.PixelWidth, configuration.PixelHeight, name)
        {
        }

        /// <summary>
        /// Gets the emitter that generates images from the visual.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <inheritdoc />
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            using (var sharedImage = ImagePool.GetOrCreate(this.pixelWidth, this.pixelHeight, Imaging.PixelFormat.BGRA_32bpp))
            {
                var resource = sharedImage.Resource;
                this.visual.Dispatcher.Invoke(() =>
                {
                    this.renderTargetBitmap.Render(this.visual);
                    this.renderTargetBitmap.CopyPixels(Int32Rect.Empty, resource.ImageData, resource.Size, resource.Stride);
                });

                this.Out.Post(sharedImage, currentTime);
            }

            return currentTime + this.interval;
        }
    }
}
