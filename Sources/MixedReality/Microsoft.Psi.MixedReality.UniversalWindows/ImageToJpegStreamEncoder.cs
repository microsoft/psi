// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Graphics.Imaging;

    /// <summary>
    /// Implements a JPEG image encoder.
    /// </summary>
    public class ImageToJpegStreamEncoder : IImageToStreamEncoder
    {
        private readonly BitmapPropertySet propertySet;
        private readonly double imageQuality;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageToJpegStreamEncoder"/> class.
        /// </summary>
        /// <param name="imageQuality">Optional image quality (0.0 - 1.0, default 1.0).</param>
        public ImageToJpegStreamEncoder(double imageQuality = 1.0)
        {
            this.imageQuality = imageQuality;
            this.propertySet = new ()
            {
                { "ImageQuality", new BitmapTypedValue(imageQuality, Windows.Foundation.PropertyType.Single) },
            };
        }

        /// <inheritdoc/>
        public string Description => $"Jpeg({this.imageQuality:0.00})";

        /// <inheritdoc/>
        public void EncodeToStream(Image image, Stream stream)
        {
            this.Encode(image, stream).Wait();
        }

        private async Task Encode(Image image, Stream stream)
        {
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream(), this.propertySet);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                (uint)image.Width,
                (uint)image.Height,
                96,
                96,
                image.ReadBytes(image.Size));
            await encoder.FlushAsync();
        }
    }
}
