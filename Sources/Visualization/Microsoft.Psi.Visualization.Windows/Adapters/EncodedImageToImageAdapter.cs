// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from shared encoded images to shared images.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageToImageAdapter : StreamAdapter<Shared<EncodedImage>, Shared<Image>>
    {
        /// <inheritdoc/>
        public override Shared<Image> GetAdaptedValue(Shared<EncodedImage> source, Envelope envelope)
        {
            Shared<Image> sharedImage = null;

            if (source != null && source.Resource != null)
            {
                // The code below maintains back-compatibility with encoded images which did not store the pixel format
                // on the instance, but only in the stream. If the pixel format is unknown, we call upon the decoder to
                // retrieve the pixel format. This might be less performant, but enables decoding in the right format
                // even from older versions of encoded images.
                var decoder = new ImageFromStreamDecoder();
                var pixelFormat = source.Resource.PixelFormat == PixelFormat.Undefined ?
                    decoder.GetPixelFormat(source.Resource.ToStream()) : source.Resource.PixelFormat;

                // If the decoder does not return a valid pixel format, we throw an exception.
                if (pixelFormat == PixelFormat.Undefined)
                {
                    throw new System.ArgumentException("The encoded image does not contain a supported pixel format.");
                }

                sharedImage = ImagePool.GetOrCreate(source.Resource.Width, source.Resource.Height, pixelFormat);
                decoder.DecodeFromStream(source.Resource.ToStream(), sharedImage.Resource);
            }

            return sharedImage;
        }

        /// <inheritdoc/>
        public override void Dispose(Shared<Image> destination) =>
            destination?.Dispose();
    }
}
