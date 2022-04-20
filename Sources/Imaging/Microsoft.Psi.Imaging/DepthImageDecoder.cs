// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that decodes a depth image using a specified <see cref="IDepthImageFromStreamDecoder"/>.
    /// </summary>
    public class DepthImageDecoder : ConsumerProducer<Shared<EncodedDepthImage>, Shared<DepthImage>>
    {
        private readonly IDepthImageFromStreamDecoder decoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageDecoder"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="decoder">The depth image decoder to use.</param>
        /// <param name="name">An optional name for the component.</param>
        public DepthImageDecoder(Pipeline pipeline, IDepthImageFromStreamDecoder decoder, string name = nameof(DepthImageDecoder))
            : base(pipeline, name)
        {
            this.decoder = decoder;
        }

        /// <inheritdoc/>
        protected override void Receive(Shared<EncodedDepthImage> sharedEncodedDepthImage, Envelope envelope)
        {
            using var sharedDepthImage = DepthImagePool.GetOrCreate(
                sharedEncodedDepthImage.Resource.Width,
                sharedEncodedDepthImage.Resource.Height,
                sharedEncodedDepthImage.Resource.DepthValueSemantics,
                sharedEncodedDepthImage.Resource.DepthValueToMetersScaleFactor);
            sharedDepthImage.Resource.DecodeFrom(sharedEncodedDepthImage.Resource, this.decoder);
            this.Out.Post(sharedDepthImage, envelope.OriginatingTime);
        }
    }
}