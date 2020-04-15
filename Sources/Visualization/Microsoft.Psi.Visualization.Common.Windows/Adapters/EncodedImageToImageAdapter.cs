// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
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

        private static Shared<Image> Adapter(Shared<EncodedImage> encodedImage, Envelope env)
        {
            Shared<Image> sharedImage = null;

            if ((encodedImage != null) && (encodedImage.Resource != null))
            {
                sharedImage = ImagePool.GetOrCreate(encodedImage.Resource.Width, encodedImage.Resource.Height, ImageDecoder.GetPixelFormat(encodedImage.Resource));
                ImageDecoder.DecodeTo(encodedImage.Resource, sharedImage.Resource);
            }

            return sharedImage;
        }
    }
}
