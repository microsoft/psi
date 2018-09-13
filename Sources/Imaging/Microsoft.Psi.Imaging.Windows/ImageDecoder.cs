// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that decodes an image using a specified decoder (e.g. JPEG, PNG)
    /// </summary>
    public class ImageDecoder : ConsumerProducer<Shared<EncodedImage>, Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDecoder"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        public ImageDecoder(Pipeline pipeline)
            : base(pipeline)
        {
        }

        /// <summary>
        /// Pipeline callback method for decoding a sample
        /// </summary>
        /// <param name="encodedImage">Encoded image to decode</param>
        /// <param name="e">Pipeline information about the sample</param>
        protected override void Receive(Shared<EncodedImage> encodedImage, Envelope e)
        {
            using (var image = ImagePool.GetOrCreate(encodedImage.Resource.Width, encodedImage.Resource.Height, Imaging.PixelFormat.BGR_24bpp))
            {
                encodedImage.Resource.DecodeTo(image.Resource);
                this.Out.Post(image, e.OriginatingTime);
            }
        }
    }
}