// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Pipeline component for encoding an image.
    /// </summary>
    public class ImageEncoder : ConsumerProducer<Shared<Image>, Shared<EncodedImage>>
    {
        private readonly Func<IBitmapEncoder> encoderFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageEncoder"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="encoderFn">Callback method for encoding a single image sample.</param>
        public ImageEncoder(Pipeline pipeline, Func<IBitmapEncoder> encoderFn)
            : base(pipeline)
        {
            this.encoderFn = encoderFn;
        }

        /// <summary>
        /// Pipeline callback function for encoding an image sample.
        /// </summary>
        /// <param name="sharedImage">Image to be encoded.</param>
        /// <param name="e">Pipeline information about the sample.</param>
        protected override void Receive(Shared<Image> sharedImage, Envelope e)
        {
            // the encoder has thread affinity, so we need to re-create it (we can't dispatch the call since we sdon't know if the thread that created us is pumping messages)
            var encoder = this.encoderFn();

            using (var sharedEncodedImage = EncodedImagePool.GetOrCreate())
            {
                sharedEncodedImage.Resource.EncodeFrom(sharedImage.Resource, encoder.Encode);
                this.Out.Post(sharedEncodedImage, e.OriginatingTime);
            }
        }
    }
}