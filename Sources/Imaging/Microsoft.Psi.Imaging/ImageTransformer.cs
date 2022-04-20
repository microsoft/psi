// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Defines the delegate used to perform an image transformation.
    /// </summary>
    /// <param name="source">Source image to be transformed.</param>
    /// <param name="destination">Destination for transformed image.</param>
    public delegate void TransformDelegate(Image source, Image destination);

    /// <summary>
    /// Component that transforms an image given a specified transformer.
    /// </summary>
    public class ImageTransformer : ConsumerProducer<Shared<Image>, Shared<Image>>
    {
        private readonly TransformDelegate transformer;
        private readonly PixelFormat pixelFormat;
        private readonly Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageTransformer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="transformer">Function for transforming the source image.</param>
        /// <param name="pixelFormat">Pixel format for destination image.</param>
        /// <param name="sharedImageAllocator ">Optional image allocator for creating new shared image.</param>
        /// <param name="name">An optional name for the component.</param>
        public ImageTransformer(
            Pipeline pipeline,
            TransformDelegate transformer,
            PixelFormat pixelFormat,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null,
            string name = nameof(ImageTransformer))
            : base(pipeline, name)
        {
            this.transformer = transformer;
            this.pixelFormat = pixelFormat;
            this.sharedImageAllocator = sharedImageAllocator ?? ((width, height, pixelFormat) => ImagePool.GetOrCreate(width, height, pixelFormat));
        }

        /// <summary>
        /// Pipeline callback for processing this component.
        /// </summary>
        /// <param name="sharedImage">Image to transform.</param>
        /// <param name="e">Pipeline sample information.</param>
        protected override void Receive(Shared<Image> sharedImage, Envelope e)
        {
            using var sharedResultImage = this.sharedImageAllocator (sharedImage.Resource.Width, sharedImage.Resource.Height, this.pixelFormat);
            this.transformer(sharedImage.Resource, sharedResultImage.Resource);
            this.Out.Post(sharedResultImage, e.OriginatingTime);
        }
    }
}