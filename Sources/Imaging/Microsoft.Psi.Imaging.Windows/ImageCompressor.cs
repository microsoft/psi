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
    public class ImageCompressor : Image.IImageCompressor
    {
        private Image.CustomSerializer.CompressionMethod compressionMethod = Image.CustomSerializer.CompressionMethod.PNG;

        /// <summary>
        /// This method sets the compression method which will be used by
        /// Serialize/Deserialize.
        /// </summary>
        /// <param name="method">Type of compression to use</param>
        public void Initialize(Image.CustomSerializer.CompressionMethod method)
        {
            this.compressionMethod = method;
        }

        /// <summary>
        /// Given an image and stream, this method will compress the image using
        /// the compression method set in Initialize()
        /// </summary>
        /// <param name="writer">Stream to write compressed image to</param>
        /// <param name="instance">Image to be compressed</param>
        /// <param name="context">Serialization context</param>
        public void Serialize(BufferWriter writer, Image instance, SerializationContext context)
        {
            BitmapEncoder encoder = null;
            switch (this.compressionMethod)
            {
                case Image.CustomSerializer.CompressionMethod.JPEG:
                    encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                    break;
                case Image.CustomSerializer.CompressionMethod.PNG:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            using (var sharedEncodedImage = EncodedImagePool.Get())
            {
                sharedEncodedImage.Resource.EncodeFrom(instance, encoder);
                Serializer.Serialize(writer, sharedEncodedImage, context);
            }
        }

        /// <summary>
        /// Given an serialization stream, this method will decompress
        /// an image from the stream and return the image via 'target'
        /// </summary>
        /// <param name="reader">Stream to read compressed image from</param>
        /// <param name="target">Returns the decompressed image</param>
        /// <param name="context">Serialization context</param>
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
