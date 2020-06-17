// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents an adapter that converts an encoded depth image to an image.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageToImageAdapter : StreamAdapter<Shared<EncodedDepthImage>, Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImageToImageAdapter"/> class.
        /// </summary>
        public EncodedDepthImageToImageAdapter()
            : base(Adapter)
        {
        }

        private static Shared<Image> Adapter(Shared<EncodedDepthImage> sharedEncodedDepthImage, Envelope envelope)
        {
            Shared<Image> sharedImage = null;

            if ((sharedEncodedDepthImage != null) && (sharedEncodedDepthImage.Resource != null))
            {
                var sharedDepthImage = DepthImagePool.GetOrCreate(sharedEncodedDepthImage.Resource.Width, sharedEncodedDepthImage.Resource.Height);
                sharedImage = ImagePool.GetOrCreate(sharedEncodedDepthImage.Resource.Width, sharedEncodedDepthImage.Resource.Height, PixelFormat.Gray_16bpp);
                var decoder = new DepthImageFromStreamDecoder();
                decoder.DecodeFromStream(sharedEncodedDepthImage.Resource.ToStream(), sharedDepthImage.Resource);
                sharedDepthImage.Resource.CopyTo(sharedImage.Resource);
            }

            return sharedImage;
        }
    }
}
