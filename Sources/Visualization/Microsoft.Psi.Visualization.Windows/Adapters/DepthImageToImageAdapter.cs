// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of depth image to an image.
    /// </summary>
    [StreamAdapter]
    public class DepthImageToImageAdapter : StreamAdapter<Shared<DepthImage>, Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageToImageAdapter"/> class.
        /// </summary>
        public DepthImageToImageAdapter()
            : base(Adapter)
        {
        }

        private static Shared<Image> Adapter(Shared<DepthImage> sharedDepthImage, Envelope envelope)
        {
            Shared<Image> sharedImage = null;

            if ((sharedDepthImage != null) && (sharedDepthImage.Resource != null))
            {
                sharedImage = ImagePool.GetOrCreate(sharedDepthImage.Resource.Width, sharedDepthImage.Resource.Height, PixelFormat.Gray_16bpp);
                sharedImage.Resource.CopyFrom(sharedDepthImage.Resource);
            }

            return sharedImage;
        }
    }
}
