// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that decodes an image using a specified decoder (e.g. JPEG, PNG).
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
            var decoder = BitmapDecoder.Create(encodedImage.GetStream(), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            bitmapSource.CopyPixels(Int32Rect.Empty, image.ImageData, image.Stride * image.Height, image.Stride);
        }

        /// <summary>
        /// Returns the pixel format of the image.
        /// </summary>
        /// <param name="encodedImage">Encoded image from which to get pixel format.</param>
        /// <returns>Returns the image's pixel format.</returns>
        public static PixelFormat GetPixelFormat(EncodedImage encodedImage)
        {
            var decoder = BitmapDecoder.Create(encodedImage.GetStream(), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            if (bitmapSource.Format == System.Windows.Media.PixelFormats.Bgr24)
            {
                return PixelFormat.BGR_24bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Gray16)
            {
                return PixelFormat.Gray_16bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Gray8)
            {
                return PixelFormat.Gray_8bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Bgr32)
            {
                return PixelFormat.BGRX_32bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Bgra32)
            {
                return PixelFormat.BGRA_32bpp;
            }
            else if (bitmapSource.Format == System.Windows.Media.PixelFormats.Rgba64)
            {
                return PixelFormat.RGBA_64bpp;
            }
            else
            {
                throw new NotImplementedException("Format not supported.");
            }
        }

        /// <summary>
        /// Pipeline callback method for decoding a sample.
        /// </summary>
        /// <param name="encodedImage">Encoded image to decode.</param>
        /// <param name="e">Pipeline information about the sample.</param>
        protected override void Receive(Shared<EncodedImage> encodedImage, Envelope e)
        {
            using (var image = ImagePool.GetOrCreate(encodedImage.Resource.Width, encodedImage.Resource.Height, Imaging.PixelFormat.BGR_24bpp))
            {
                DecodeTo(encodedImage.Resource, image.Resource);
                this.Out.Post(image, e.OriginatingTime);
            }
        }
    }
}
