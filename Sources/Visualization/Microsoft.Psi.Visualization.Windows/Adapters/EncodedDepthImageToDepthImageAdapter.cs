// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of encoded depth image to depth image.
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
            return sharedEncodedDepthImage?.Decode();
        }
    }
}
