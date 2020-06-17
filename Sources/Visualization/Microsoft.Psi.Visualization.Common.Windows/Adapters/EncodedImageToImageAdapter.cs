// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents an adapter that converts encoded images to images.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageToImageAdapter : StreamAdapter<Shared<EncodedImage>, Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageToImageAdapter"/> class.
        /// </summary>
        public EncodedImageToImageAdapter()
            : base(Adapter)
        {
        }

        private static Shared<Image> Adapter(Shared<EncodedImage> sharedEncodedImage, Envelope envelope)
        {
            Shared<Image> sharedImage = null;

            if ((sharedEncodedImage != null) && (sharedEncodedImage.Resource != null))
            {
                // The code below maintains back-compatibility with encoded images which did not store the pixel format
                // on the instance, but only in the stream. If the pixel format is unknown, we call upon the decoder to
                // retrieve the pixel format. This might be less performant, but enables decoding in the right format
                // even from older versions of encoded images.
                var decoder = new ImageFromStreamDecoder();
                var pixelFormat = sharedEncodedImage.Resource.PixelFormat == PixelFormat.Undefined ?
                    decoder.GetPixelFormat(sharedEncodedImage.Resource.ToStream()) : sharedEncodedImage.Resource.PixelFormat;

                // If the decoder does not return a valid pixel format, we throw an exception.
                if (pixelFormat == PixelFormat.Undefined)
                {
                    throw new ArgumentException("The encoded image does not contain a supported pixel format.");
                }

                sharedImage = ImagePool.GetOrCreate(sharedEncodedImage.Resource.Width, sharedEncodedImage.Resource.Height, pixelFormat);
                decoder.DecodeFromStream(sharedEncodedImage.Resource.ToStream(), sharedImage.Resource);
            }

            return sharedImage;
        }
    }
}
