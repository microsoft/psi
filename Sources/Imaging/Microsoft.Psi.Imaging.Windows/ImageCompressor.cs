// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// ImageCompressor defines an object used by the serialization layer
    /// for compressing streams of images in a generic fashion. This object
    /// should not be called directly but instead if used by Microsoft.Psi.Imaging.
    /// </summary>
    public class ImageCompressor : IImageCompressor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCompressor"/> class.
        /// </summary>
        public ImageCompressor()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCompressor"/> class.
        /// </summary>
        /// <param name="compressionMethod">Compression method to be used by compressor.</param>
        public ImageCompressor(CompressionMethod compressionMethod)
        {
            this.CompressionMethod = compressionMethod;
        }

        /// <summary>
        /// Gets or sets the compression method being used by the compressor.
        /// </summary>
        public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.PNG;

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, Image instance, SerializationContext context)
        {
            BitmapEncoder encoder = null;
            switch (this.CompressionMethod)
            {
                case CompressionMethod.JPEG:
                    encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                    break;
                case CompressionMethod.PNG:
                    encoder = new PngBitmapEncoder();
                    break;
                case CompressionMethod.None:
                    break;
            }

            if (encoder != null)
            {
                using (var sharedEncodedImage = EncodedImagePool.GetOrCreate())
                {
                    sharedEncodedImage.Resource.EncodeFrom(instance, encoder);
                    Serializer.Serialize(writer, sharedEncodedImage, context);
                }
            }
            else
            {
                Serializer.Serialize(writer, instance, context);
            }
        }

        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, ref Image target, SerializationContext context)
        {
            Shared<EncodedImage> encodedImage = null;
            Serializer.Deserialize(reader, ref encodedImage, context);
            using (var image = ImagePool.GetOrCreate(encodedImage.Resource.Width, encodedImage.Resource.Height, Imaging.PixelFormat.BGR_24bpp))
            {
                encodedImage.Resource.DecodeTo(image.Resource);
                target = image.Resource.DeepClone();
            }

            if (encodedImage != null)
            {
                encodedImage.Dispose();
            }
        }
    }
}
