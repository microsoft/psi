// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that decodes an image using a specified <see cref="IImageFromStreamDecoder"/>.
    /// </summary>
    public class ImageDecoder : ConsumerProducer<Shared<EncodedImage>, Shared<Image>>
    {
        private readonly IImageFromStreamDecoder decoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDecoder"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="decoder">The image decoder to use.</param>
        /// <param name="name">An optional name for the component.</param>
        public ImageDecoder(Pipeline pipeline, IImageFromStreamDecoder decoder, string name = nameof(ImageDecoder))
            : base(pipeline, name)
        {
            this.decoder = decoder;
        }

        /// <inheritdoc/>
        protected override void Receive(Shared<EncodedImage> sharedEncodedImage, Envelope envelope)
        {
            if (sharedEncodedImage == null || sharedEncodedImage.Resource == null)
            {
                this.Out.Post(null, envelope.OriginatingTime);
                return;
            }

            // The code below maintains back-compatibility with encoded images which did not store the pixel format
            // on the instance, but only in the stream. If the pixel format is unknown, we call upon the decoder to
            // retrieve the pixel format. This might be less performant, but enables decoding in the right format
            // even from older versions of encoded images.
            var pixelFormat = sharedEncodedImage.Resource.PixelFormat == PixelFormat.Undefined ?
                this.decoder.GetPixelFormat(sharedEncodedImage.Resource.ToStream()) : sharedEncodedImage.Resource.PixelFormat;

            // If the decoder does not return a valid pixel format, we throw an exception.
            if (pixelFormat == PixelFormat.Undefined)
            {
                throw new ArgumentException("The encoded image does not contain a supported pixel format.");
            }

            using var sharedImage = ImagePool.GetOrCreate(
                sharedEncodedImage.Resource.Width, sharedEncodedImage.Resource.Height, pixelFormat);
            sharedImage.Resource.DecodeFrom(sharedEncodedImage.Resource, this.decoder);
            this.Out.Post(sharedImage, envelope.OriginatingTime);
        }
    }
}