// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that encodes an image using a specified encoder (e.g. JPEG, PNG).
    /// </summary>
    public class ImageEncoder : ConsumerProducer<Shared<Image>, Shared<EncodedImage>>
    {
        private readonly Func<BitmapEncoder> encoderFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageEncoder"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="encoderFn">Callback method for encoding a single image sample.</param>
        public ImageEncoder(Pipeline pipeline, Func<BitmapEncoder> encoderFn)
            : base(pipeline)
        {
            this.encoderFn = encoderFn;
        }

        /// <summary>
        /// Encodes an image in-place into the given encoded image instance using the specified encoder.
        /// </summary>
        /// <param name="encodedImage">Encoded image into which to encode in-place.</param>
        /// <param name="image">Image to be encoded.</param>
        /// <param name="encoder">Encoder to use.</param>
        public static void EncodeFrom(EncodedImage encodedImage, Image image, BitmapEncoder encoder)
        {
            System.Windows.Media.PixelFormat format;
            if (image.PixelFormat == PixelFormat.BGR_24bpp)
            {
                format = System.Windows.Media.PixelFormats.Bgr24;
            }
            else if (image.PixelFormat == PixelFormat.Gray_16bpp)
            {
                format = System.Windows.Media.PixelFormats.Gray16;
            }
            else if (image.PixelFormat == PixelFormat.Gray_8bpp)
            {
                format = System.Windows.Media.PixelFormats.Gray8;
            }
            else
            {
                format = System.Windows.Media.PixelFormats.Bgr32;
            }

            encodedImage.EncodeFrom(image, (_, stream) =>
            {
                BitmapSource bitmapSource = BitmapSource.Create(image.Width, image.Height, 96, 96, format, null, image.ImageData, image.Stride * image.Height, image.Stride);
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
            });
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
                EncodeFrom(sharedEncodedImage.Resource, sharedImage.Resource, encoder);
                this.Out.Post(sharedEncodedImage, e.OriginatingTime);
            }
        }
    }
}