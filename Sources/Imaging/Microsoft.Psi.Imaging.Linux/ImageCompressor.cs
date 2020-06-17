// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Implements a compressor used by the serialization layer for compressing streams
    /// of images in a generic fashion. This object should not be called directly, but
    /// instead is used by the <see cref="Image.CustomSerializer"/> class.
    /// </summary>
    public class ImageCompressor : IImageCompressor
    {
        private readonly IImageToStreamEncoder encoder = null;
        private readonly IImageFromStreamDecoder decoder = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCompressor"/> class.
        /// </summary>
        /// <param name="compressionMethod">The image compression method to be used.</param>
        public ImageCompressor(CompressionMethod compressionMethod)
        {
            this.CompressionMethod = compressionMethod;

            switch (this.CompressionMethod)
            {
                case CompressionMethod.Jpeg:
                    this.encoder = new ImageToJpegStreamEncoder { QualityLevel = 90 };
                    break;
                case CompressionMethod.Png:
                    this.encoder = new ImageToPngStreamEncoder();
                    break;
                case CompressionMethod.None:
                    break;
            }

            this.decoder = new ImageFromStreamDecoder();
        }

        /// <inheritdoc/>
        public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.Png;

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, Image image, SerializationContext context)
        {
            if (this.encoder != null)
            {
                using var sharedEncodedImage = EncodedImagePool.GetOrCreate(image.Width, image.Height, image.PixelFormat);
                sharedEncodedImage.Resource.EncodeFrom(image, this.encoder);
                Serializer.Serialize(writer, sharedEncodedImage, context);
            }
            else
            {
                Serializer.Serialize(writer, image, context);
            }
        }

        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, ref Image image, SerializationContext context)
        {
            Shared<EncodedImage> sharedEncodedImage = null;
            Serializer.Deserialize(reader, ref sharedEncodedImage, context);

            using var sharedImage = ImagePool.GetOrCreate(
                sharedEncodedImage.Resource.Width,
                sharedEncodedImage.Resource.Height,
                sharedEncodedImage.Resource.PixelFormat);
            sharedImage.Resource.DecodeFrom(sharedEncodedImage.Resource, this.decoder);
            image = sharedImage.Resource.DeepClone();
            sharedEncodedImage.Dispose();
        }
    }
}
