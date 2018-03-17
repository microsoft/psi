// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Defines the delegate used to perform an image transformation
    /// </summary>
    /// <param name="src">Source image to be transformed</param>
    /// <param name="dest">Destination for transformed image</param>
    public delegate void TransformDelegate(Image src, Image dest);

    /// <summary>
    /// Pipeline component for transforming an image
    /// </summary>
    public class TransformImageComponent : ConsumerProducer<Shared<Image>, Shared<Image>>
    {
        private TransformDelegate transformer;
        private PixelFormat pixelFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformImageComponent"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        /// <param name="transformer">Function for transforming the source image</param>
        /// <param name="pixelFormat">Pixel format for destination image</param>
        public TransformImageComponent(Pipeline pipeline, TransformDelegate transformer, PixelFormat pixelFormat)
            : base(pipeline)
        {
            this.transformer = transformer;
            this.pixelFormat = pixelFormat;
        }

        /// <summary>
        /// Pipeline callback for processing this component
        /// </summary>
        /// <param name="sharedImage">Image to transform</param>
        /// <param name="e">Pipeline sample information</param>
        protected override void Receive(Shared<Image> sharedImage, Envelope e)
        {
            using (var psiImageDest = ImagePool.GetOrCreate(sharedImage.Resource.Width, sharedImage.Resource.Height, this.pixelFormat))
            {
                this.transformer(sharedImage.Resource, psiImageDest.Resource);
                this.Out.Post(psiImageDest, e.OriginatingTime);
            }
        }
    }
}