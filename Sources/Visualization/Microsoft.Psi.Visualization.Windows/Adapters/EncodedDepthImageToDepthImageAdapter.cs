// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents an adapter that converts an encoded depth image to a depth image.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageToDepthImageAdapter : StreamAdapter<Shared<EncodedDepthImage>, Shared<DepthImage>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImageToDepthImageAdapter"/> class.
        /// </summary>
        public EncodedDepthImageToDepthImageAdapter()
            : base(Adapter)
        {
        }

        private static Shared<DepthImage> Adapter(Shared<EncodedDepthImage> sharedEncodedDepthImage, Envelope envelope)
        {
            Shared<DepthImage> sharedDepthImage = null;

            if ((sharedEncodedDepthImage != null) && (sharedEncodedDepthImage.Resource != null))
            {
                sharedDepthImage = DepthImagePool.GetOrCreate(sharedEncodedDepthImage.Resource.Width, sharedEncodedDepthImage.Resource.Height);
                var decoder = new DepthImageFromStreamDecoder();
                decoder.DecodeFromStream(sharedEncodedDepthImage.Resource.ToStream(), sharedDepthImage.Resource);
            }

            return sharedDepthImage;
        }
    }
}
