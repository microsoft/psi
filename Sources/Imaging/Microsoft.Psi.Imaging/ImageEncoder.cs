// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that encodes an image using a specified <see cref="IImageToStreamEncoder"/>.
    /// </summary>
    public class ImageEncoder : ConsumerProducer<Shared<Image>, Shared<EncodedImage>>
    {
        private readonly IImageToStreamEncoder encoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageEncoder"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="encoder">The image encoder to use.</param>
        /// <param name="name">An optional name for the component.</param>
        public ImageEncoder(Pipeline pipeline, IImageToStreamEncoder encoder, string name = null)
            : base(pipeline, name ?? $"{nameof(ImageEncoder)}({encoder.Description})")
        {
            this.encoder = encoder;
        }

        /// <inheritdoc/>
        protected override void Receive(Shared<Image> sharedImage, Envelope envelope)
        {
            if (sharedImage == null || sharedImage.Resource == null)
            {
                this.Out.Post(null, envelope.OriginatingTime);
                return;
            }

            using var sharedEncodedImage = EncodedImagePool.GetOrCreate(
                sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.PixelFormat);
            sharedEncodedImage.Resource.EncodeFrom(sharedImage.Resource, this.encoder);
            this.Out.Post(sharedEncodedImage, envelope.OriginatingTime);
        }
    }
}