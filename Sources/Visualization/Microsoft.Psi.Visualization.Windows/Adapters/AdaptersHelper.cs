// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Provides helper and extension methods useful for various adapters.
    /// </summary>
    internal static class AdaptersHelper
    {
        /// <summary>
        /// Decode a shared encoded image to a shared image.
        /// </summary>
        /// <param name="sharedEncodedImage">The shared encoded image to decode.</param>
        /// <returns>The decoded shared image.</returns>
        internal static Shared<Image> Decode(this Shared<EncodedImage> sharedEncodedImage)
        {
            Shared<Image> sharedImage = null;

            if (sharedEncodedImage.Resource != null)
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

        /// <summary>
        /// Decode a shared encoded depth image to a shared depth image.
        /// </summary>
        /// <param name="sharedEncodedDepthImage">The shared encoded depth image to decode.</param>
        /// <returns>The decoded shared depth image.</returns>
        internal static Shared<DepthImage> Decode(this Shared<EncodedDepthImage> sharedEncodedDepthImage)
        {
            Shared<DepthImage> sharedDepthImage = null;

            if (sharedEncodedDepthImage.Resource != null)
            {
                sharedDepthImage = DepthImagePool.GetOrCreate(sharedEncodedDepthImage.Resource.Width, sharedEncodedDepthImage.Resource.Height);
                var decoder = new DepthImageFromStreamDecoder();
                decoder.DecodeFromStream(sharedEncodedDepthImage.Resource.ToStream(), sharedDepthImage.Resource);
            }

            return sharedDepthImage;
        }
    }
}
