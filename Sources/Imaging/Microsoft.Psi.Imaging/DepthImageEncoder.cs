// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that encodes an image using a specified <see cref="IDepthImageToStreamEncoder"/>.
    /// </summary>
    public class DepthImageEncoder : ConsumerProducer<Shared<DepthImage>, Shared<EncodedDepthImage>>
    {
        private readonly IDepthImageToStreamEncoder encoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageEncoder"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="encoder">The depth image encoder to use.</param>
        public DepthImageEncoder(Pipeline pipeline, IDepthImageToStreamEncoder encoder)
            : base(pipeline)
        {
            this.encoder = encoder;
        }

        /// <inheritdoc/>
        protected override void Receive(Shared<DepthImage> sharedDepthImage, Envelope e)
        {
            using var sharedEncodedDepthImage = EncodedDepthImagePool.GetOrCreate(
                sharedDepthImage.Resource.Width, sharedDepthImage.Resource.Height);
            sharedEncodedDepthImage.Resource.EncodeFrom(sharedDepthImage.Resource, this.encoder);
            this.Out.Post(sharedEncodedDepthImage, e.OriginatingTime);
        }
    }
}