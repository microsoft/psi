// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of encoded images to images.
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
            return sharedEncodedImage?.Decode();
        }
    }
}
