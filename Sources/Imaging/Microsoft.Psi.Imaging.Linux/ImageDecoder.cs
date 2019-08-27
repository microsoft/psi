// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.Runtime.InteropServices;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using SkiaSharp;

    /// <summary>
    /// Pipeline component for decoding an image.
    /// </summary>
    public class ImageDecoder : ConsumerProducer<Shared<EncodedImage>, Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDecoder"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        public ImageDecoder(Pipeline pipeline)
            : base(pipeline)
        {
        }

        /// <summary>
        /// Decodes an encoded image into the given image instance.
        /// </summary>
        /// <param name="encodedImage">Encoded image to decode.</param>
        /// <param name="image">Image into which to decode.</param>
        public static void DecodeTo(EncodedImage encodedImage, Image image)
        {
            var decoded = SKBitmap.Decode(encodedImage.GetStream());
            Marshal.Copy(decoded.Bytes, 0, image.ImageData, decoded.ByteCount);
        }

        /// <summary>
        /// Pipeline callback method for decoding a sample.
        /// </summary>
        /// <param name="encodedImage">Encoded image to decode.</param>
        /// <param name="e">Pipeline information about the sample.</param>
        protected override void Receive(Shared<EncodedImage> encodedImage, Envelope e)
        {
            using (var image = ImagePool.GetOrCreate(encodedImage.Resource.Width, encodedImage.Resource.Height, PixelFormat.BGR_24bpp))
            {
                DecodeTo(encodedImage.Resource, image.Resource);
                this.Out.Post(image, e.OriginatingTime);
            }
        }
    }
}